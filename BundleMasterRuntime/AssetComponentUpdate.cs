using System.IO;
using System.Collections.Generic;
using ET;
using UnityEngine.Networking;

namespace BM
{
    public static partial class AssetComponent
    {
        /// <summary>
        /// 检查分包是否需要更新
        /// </summary>
        /// <param name="bundlePackageNames">所有分包的名称</param>
        public static async ETTask<UpdateBundleDataInfo> CheckAllBundlePackageUpdate(List<string> bundlePackageNames)
        {
            UpdateBundleDataInfo updateBundleDataInfo = new UpdateBundleDataInfo();
            if (AssetComponentConfig.AssetLoadMode != AssetLoadMode.Build)
            {
                updateBundleDataInfo.NeedUpdate = false;
                if (AssetComponentConfig.AssetLoadMode == AssetLoadMode.Local)
                {
                    AssetComponentConfig.HotfixPath = AssetComponentConfig.LocalBundlePath;
                }
                else
                {
#if !UNITY_EDITOR
                    AssetLogHelper.LogError("AssetLoadMode = AssetLoadMode.Develop 只能在编辑器下运行");
#endif
                }
                return updateBundleDataInfo;
            }
            
            for (int i = 0; i < bundlePackageNames.Count; i++)
            {
                string bundlePackageName = bundlePackageNames[i];
                string remoteVersionLog = await GetRemoteBundlePackageVersionLog(bundlePackageName);
                if (remoteVersionLog == null)
                {
                    continue;
                }
                //创建记录需要更新的Bundle的信息
                List<string> allRemoteVersionFiles = new List<string>();
                updateBundleDataInfo.PackageAllRemoteVersionFile.Add(bundlePackageName, allRemoteVersionFiles);
                string localVersionLog;
                //获取本地的VersionLog
                string localVersionLogExistPath = BundleFileExistPath(bundlePackageName, "VersionLogs.txt");
                ETTask logTcs = ETTask.Create();
                using (UnityWebRequest webRequest = UnityWebRequest.Get(localVersionLogExistPath))
                {
                    UnityWebRequestAsyncOperation weq = webRequest.SendWebRequest();
                    weq.completed += (o) =>
                    {
                        logTcs.SetResult();
                    };
                    await logTcs;
#if UNITY_2020_1_OR_NEWER
                    if (webRequest.result != UnityWebRequest.Result.Success)
#else
                    if (!string.IsNullOrEmpty(webRequest.error))
#endif
                    {
                        localVersionLog = "INIT|0";
                    }
                    else
                    {
                        localVersionLog = webRequest.downloadHandler.text;
                    }
                }
                CalcNeedUpdateBundle(updateBundleDataInfo, bundlePackageName, remoteVersionLog, localVersionLog, allRemoteVersionFiles);
            }
            if (updateBundleDataInfo.NeedUpdateSize > 0)
            {
                updateBundleDataInfo.NeedUpdate = true;
            }
            return updateBundleDataInfo;
        }
        
        /// <summary>
        /// 获取所哟需要更新的Bundle的文件
        /// </summary>
        private static void CalcNeedUpdateBundle(UpdateBundleDataInfo updateBundleDataInfo, string bundlePackageName, string remoteVersionLog, string localVersionLog, List<string> allRemoteVersionFiles)
        {
            string[] remoteVersionData = remoteVersionLog.Split('\n');
            string[] localVersionData = localVersionLog.Split('\n');
            int remoteVersion = int.Parse(remoteVersionData[0].Split('|')[1]);
            int localVersion = int.Parse(localVersionData[0].Split('|')[1]);
            updateBundleDataInfo.PackageToVersion.Add(bundlePackageName, new int[2]{localVersion, remoteVersion});
            if (localVersion > remoteVersion)
            {
                AssetLogHelper.LogError("本地版本号优先与远程版本号 " + localVersion + ">" + remoteVersion + "\n"
                + "localBundleTime: " + localVersionData[0].Split('|')[0] + "\n"
                + "remoteBundleTime: " + remoteVersionData[0].Split('|')[0] + "\n"
                + "Note: 发送了版本回退或者忘了累进版本号");
            }
            HashSet<string> allLocalBundleName = new HashSet<string>();
            for (int i = 1; i < localVersionData.Length; i++)
            {
                string lineStr = localVersionData[i];
                if (string.IsNullOrWhiteSpace(lineStr))
                {
                    continue;
                }
                string[] info = lineStr.Split('|');
                allLocalBundleName.Add(info[0]);
            }
            //创建最后需要返回的数据
            Dictionary<string, long> needUpdateBundles = new Dictionary<string, long>();
            for (int i = 1; i < remoteVersionData.Length; i++)
            {
                string lineStr = remoteVersionData[i];
                if (string.IsNullOrWhiteSpace(lineStr))
                {
                    continue;
                }
                string[] info = lineStr.Split('|');
                allRemoteVersionFiles.Add(info[0]);
                if (allLocalBundleName.Contains(info[0]))
                {
                    string filePath = BundleFileExistPath(bundlePackageName, info[0]);
                    if (filePath == null)
                    {
                        needUpdateBundles.Add(info[0], long.Parse(info[1]));
                        continue;
                    }
                    uint fileCRC32 = VerifyHelper.GetFileCRC32(filePath);
                    if (uint.Parse(info[2]) != fileCRC32)
                    {
                        needUpdateBundles.Add(info[0], long.Parse(info[1]));
                    }
                }
                else
                {
                    needUpdateBundles.Add(info[0], long.Parse(info[1]));
                }
            }
            updateBundleDataInfo.PackageNeedUpdateBundlesInfos.Add(bundlePackageName, needUpdateBundles);
            foreach (long needUpdateBundleSize in needUpdateBundles.Values)
            {
                updateBundleDataInfo.NeedUpdateSize += needUpdateBundleSize;
                updateBundleDataInfo.NeedDownLoadBundleCount++;
            }
        }
        
        /// <summary>
        /// 下载更新
        /// </summary>
        public static async ETTask DownLoadUpdate(UpdateBundleDataInfo updateBundleDataInfo)
        {
            if (AssetComponentConfig.AssetLoadMode != AssetLoadMode.Build)
            {
                AssetLogHelper.LogError("AssetLoadMode != AssetLoadMode.Build 不需要更新");
                return;
            }
            Dictionary<string, Queue<DownLoadTask>> packageDownLoadTask = new Dictionary<string, Queue<DownLoadTask>>();
            ETTask downLoading = ETTask.Create();
            //准备需要下载的内容的初始化信息
            foreach (var packageNeedUpdateBundlesInfo in updateBundleDataInfo.PackageNeedUpdateBundlesInfos)
            {
                Queue<DownLoadTask> downLoadTaskQueue = new Queue<DownLoadTask>();
                string packageName = packageNeedUpdateBundlesInfo.Key;
                packageDownLoadTask.Add(packageName, downLoadTaskQueue);
                string downLoadPackagePath = Path.Combine(AssetComponentConfig.HotfixPath, packageName);
                if (!Directory.Exists(downLoadPackagePath))
                {
                    Directory.CreateDirectory(downLoadPackagePath);
                }
                foreach (var updateBundlesInfo in packageNeedUpdateBundlesInfo.Value)
                {
                    DownLoadTask downLoadTask = new DownLoadTask();
                    downLoadTask.UpdateBundleDataInfo = updateBundleDataInfo;
                    downLoadTask.DownLoadingKey = downLoading;
                    downLoadTask.PackageDownLoadTask = packageDownLoadTask;
                    downLoadTask.PackegName = packageName;
                    downLoadTask.DownLoadPackagePath = downLoadPackagePath;
                    downLoadTask.FileName = updateBundlesInfo.Key;
                    downLoadTask.FileSize = updateBundlesInfo.Value;
                    downLoadTaskQueue.Enqueue(downLoadTask);
                }
            }
            //开启下载
            for (int i = 0; i < AssetComponentConfig.MaxDownLoadCount; i++)
            {
                foreach (Queue<DownLoadTask> downLoadTaskQueue in packageDownLoadTask.Values)
                {
                    if (downLoadTaskQueue.Count > 0)
                    {
                        downLoadTaskQueue.Dequeue().DownLoad().Coroutine();
                        break;
                    }
                }
            }
            await downLoading;
            //所有分包都下载完成了就处理分包的Log文件
            foreach (string packageName in updateBundleDataInfo.PackageNeedUpdateBundlesInfos.Keys)
            {
                byte[] fileLogsData = await DownloadBundleHelper.DownloadDataByUrl(Path.Combine(AssetComponentConfig.BundleServerUrl, packageName, "FileLogs.txt"));
                byte[] dependLogsData = await DownloadBundleHelper.DownloadDataByUrl(Path.Combine(AssetComponentConfig.BundleServerUrl, packageName, "DependLogs.txt"));
                byte[] versionLogsData = await DownloadBundleHelper.DownloadDataByUrl(Path.Combine(AssetComponentConfig.BundleServerUrl, packageName, "VersionLogs.txt"));
                if (fileLogsData == null || dependLogsData == null || versionLogsData == null)
                {
                    AssetLogHelper.LogError("获取Log表失败, PackageName: " + packageName);
                    continue;
                }
                CreateUpdateLogFile(Path.Combine(AssetComponentConfig.HotfixPath, packageName, "FileLogs.txt"),
                    System.Text.Encoding.UTF8.GetString(fileLogsData));
                CreateUpdateLogFile(Path.Combine(AssetComponentConfig.HotfixPath, packageName, "DependLogs.txt"),
                    System.Text.Encoding.UTF8.GetString(dependLogsData));
                CreateUpdateLogFile(Path.Combine(AssetComponentConfig.HotfixPath, packageName, "VersionLogs.txt"),
                    System.Text.Encoding.UTF8.GetString(versionLogsData));
            }
            
            AssetLogHelper.LogError("下载完成");
        }
        
    }


    public class DownLoadTask
    {
        public UpdateBundleDataInfo UpdateBundleDataInfo;

        public ETTask DownLoadingKey;

        public Dictionary<string, Queue<DownLoadTask>> PackageDownLoadTask;
        
        /// <summary>
        /// 下载的资源分包名称
        /// </summary>
        public string PackegName;

        /// <summary>
        /// 分包所在路径
        /// </summary>
        public string DownLoadPackagePath;

        /// <summary>
        /// 下载的文件的名称
        /// </summary>
        public string FileName;

        /// <summary>
        /// 下载的文件的大小
        /// </summary>
        public long FileSize;

        public async ETTask DownLoad()
        {
            string url = Path.Combine(AssetComponentConfig.BundleServerUrl, PackegName, FileName);
            byte[] data = await DownloadBundleHelper.DownloadDataByUrl(url);
            using (FileStream fs = new FileStream(Path.Combine(DownLoadPackagePath, FileName), FileMode.Create))
            {
                fs.Write(data, 0, data.Length);
                //await fs.WriteAsync(data, 0, data.Length);
                fs.Close();
            }
            UpdateBundleDataInfo.FinishUpdateSize += data.Length;
            UpdateBundleDataInfo.FinishDownLoadBundleCount++;
            foreach (Queue<DownLoadTask> downLoadTaskQueue in PackageDownLoadTask.Values)
            {
                if (downLoadTaskQueue.Count > 0)
                {
                    downLoadTaskQueue.Dequeue().DownLoad().Coroutine();
                    return;
                }
            }
            //说明下载完成了
            if (UpdateBundleDataInfo.FinishDownLoadBundleCount < UpdateBundleDataInfo.NeedDownLoadBundleCount)
            {
                return;
            }
            UpdateBundleDataInfo.FinishUpdate = true;
            DownLoadingKey.SetResult();
        }
        
    }
}