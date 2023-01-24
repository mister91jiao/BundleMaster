using System;
using ET;
using System.Collections.Generic;
using System.IO;
using LMTD;
using UnityEngine.Networking;
using UnityEngine;

namespace BM
{
    public class DownLoadTask
    {
        public UpdateBundleDataInfo UpdateBundleDataInfo;

        /// <summary>
        /// 资源更新下载完成回调位
        /// </summary>
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
            string url = Path.Combine(AssetComponentConfig.BundleServerUrl, PackegName, UnityWebRequest.EscapeURL(FileName));
            if (FileName.Contains("\\"))
            {
                string[] pathSplits = FileName.Split('\\');
                string filePath = "";
                string fileUrls = "";
                for (int i = 0; i < pathSplits.Length - 1; i++)
                {
                    filePath += (pathSplits[i] + "/");
                    fileUrls += (UnityWebRequest.EscapeURL(pathSplits[i]) + "/");
                }
                fileUrls += (UnityWebRequest.EscapeURL(pathSplits[pathSplits.Length - 1]));
                Directory.CreateDirectory(Path.Combine(AssetComponentConfig.HotfixPath, PackegName, filePath));
                url = Path.Combine(AssetComponentConfig.BundleServerUrl, PackegName, fileUrls);
            }
            float startDownLoadTime = Time.realtimeSinceStartup;
            DownLoadData downLoadData = await DownloadBundleHelper.DownloadRefDataByUrl(url);
            if (downLoadData.Data == null)
            {
                UpdateBundleDataInfo.CancelUpdate();
            }
            //说明下载更新已经被取消
            if (UpdateBundleDataInfo.Cancel)
            {
                DownLoadData.Recovery(downLoadData);
                return;
            }
            Debug.Assert(downLoadData.Data != null, "downLoadData.Data != null");
            int dataLength = downLoadData.Data.Length;
            
            string fileCreatePath = PathUnifiedHelper.UnifiedPath(Path.Combine(DownLoadPackagePath, FileName));
            using (FileStream fs = new FileStream(fileCreatePath, FileMode.Create))
            {
                //大于2M用异步
                if (dataLength > 2097152)
                {
                    await fs.WriteAsync(downLoadData.Data, 0, downLoadData.Data.Length);
                }
                else
                {
                    fs.Write(downLoadData.Data, 0, downLoadData.Data.Length);
                }
                fs.Close();
            }
            UpdateBundleDataInfo.AddCRCFileInfo(PackegName, FileName, VerifyHelper.GetCRC32(downLoadData));
            UpdateBundleDataInfo.FinishUpdateSize += downLoadData.Data.Length;
            UpdateBundleDataInfo.FinishDownLoadBundleCount++;
            DownLoadData.Recovery(downLoadData);
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
                DownLoadData.ClearPool();
                return;
            }
            UpdateBundleDataInfo.FinishUpdate = true;
            DownLoadingKey.SetResult();
        }
        
        
        public async ETTask ThreadDownLoad()
        {
            //计算URL
            string url = Path.Combine(AssetComponentConfig.BundleServerUrl, PackegName, UnityWebRequest.EscapeURL(FileName));
            if (FileName.Contains("\\"))
            {
                string[] pathSplits = FileName.Split('\\');
                string filePath = "";
                string fileUrls = "";
                for (int i = 0; i < pathSplits.Length - 1; i++)
                {
                    filePath += (pathSplits[i] + "/");
                    fileUrls += (UnityWebRequest.EscapeURL(pathSplits[i]) + "/");
                }
                fileUrls += (UnityWebRequest.EscapeURL(pathSplits[pathSplits.Length - 1]));
                Directory.CreateDirectory(Path.Combine(AssetComponentConfig.HotfixPath, PackegName, filePath));
                url = Path.Combine(AssetComponentConfig.BundleServerUrl, PackegName, fileUrls);
            }
            //计算文件存储路径
            string fileCreatePath = PathUnifiedHelper.UnifiedPath(Path.Combine(DownLoadPackagePath, FileName));
            //开始下载
            LmtDownloadInfo lmtDownloadInfo = await DownloadBundleHelper.DownloadData(url, fileCreatePath, UpdateBundleDataInfo);
            //说明下载更新已经被取消
            if (UpdateBundleDataInfo.Cancel)
            {
                return;
            }
            UpdateBundleDataInfo.AddCRCFileInfo(PackegName, FileName, lmtDownloadInfo.DownLoadFileCRC);
            UpdateBundleDataInfo.FinishDownLoadBundleCount++;
            //检查新的需要下载的资源
            foreach (Queue<DownLoadTask> downLoadTaskQueue in PackageDownLoadTask.Values)
            {
                if (downLoadTaskQueue.Count > 0)
                {
                    downLoadTaskQueue.Dequeue().ThreadDownLoad().Coroutine();
                    return;
                }
            }
            //说明下载完成了
            if (UpdateBundleDataInfo.FinishDownLoadBundleCount < UpdateBundleDataInfo.NeedDownLoadBundleCount)
            {
                DownLoadData.ClearPool();
                return;
            }
            UpdateBundleDataInfo.FinishUpdate = true;
            DownLoadingKey.SetResult();
        }
        
        
        
    }
}