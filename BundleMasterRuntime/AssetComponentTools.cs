using System.Collections.Generic;
using System.IO;
using System.Text;
using ET;

namespace BM
{
    public static partial class AssetComponent
    {
        /// <summary>
        /// 获取Bundle信息文件的路径
        /// </summary>
        public static string BundleFileExistPath(string bundlePackageName, string fileName)
        {
            string path = Path.Combine(AssetComponentConfig.HotfixPath, bundlePackageName, fileName);
            if (!File.Exists(path))
            {
                path = Path.Combine(AssetComponentConfig.LocalBundlePath, bundlePackageName, fileName);
                if (!File.Exists(path))
                {
                    return null;
                }
            }
            return path;
        }

        /// <summary>
        /// 获取分包的更新索引列表
        /// </summary>
        private static async ETTask<string> GetRemoteBundlePackageVersionLog(string bundlePackageName)
        {
            byte[] data = await DownloadBundleHelper.DownloadDataByUrl(Path.Combine(AssetComponentConfig.BundleServerUrl, bundlePackageName, "VersionLogs.txt"));
            if (data == null)
            {
                AssetLogHelper.LogError(bundlePackageName + "获取更新索引列表失败");
                return null;
            }
            return System.Text.Encoding.UTF8.GetString(data);
        }
        
        /// <summary>
        /// 创建更新后的Log文件
        /// </summary>
        /// <param name="filePath">文件的全路径</param>
        /// <param name="fileData">文件的内容</param>
        public static void CreateUpdateLogFile(string filePath, string fileData)
        {
            using (StreamWriter sw = new StreamWriter(filePath))
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(fileData);
                sw.WriteLine(sb.ToString());
            }
        }
        
    }

    public class UpdateBundleDataInfo
    {
        /// <summary>
        /// 是否需要更新
        /// </summary>
        public bool NeedUpdate = false;

        /// <summary>
        /// 需要更新的总大小
        /// </summary>
        public long NeedUpdateSize = 0;
        
        /// <summary>
        /// 需要更新的Bundle的信息
        /// </summary>
        public readonly Dictionary<string, Dictionary<string, long>> PackageNeedUpdateBundlesInfos = new Dictionary<string, Dictionary<string, long>>();

        /// <summary>
        /// 需要更新的分包的版本索引列表文件
        /// </summary>
        public readonly Dictionary<string, List<string>> PackageAllRemoteVersionFile = new Dictionary<string, List<string>>();
        
        /// <summary>
        /// 更新完成的大小
        /// </summary>
        public long FinishUpdateSize = 0;

        /// <summary>
        /// 是否更新完成
        /// </summary>
        public bool FinishUpdate = false;

        /// <summary>
        /// 更新进度(1 - 100)
        /// </summary>
        public float Progress
        {
            get
            {
                float progress = ((float)FinishUpdateSize / NeedUpdateSize) * 100.0f;
                return progress;
            }
        }

        /// <summary>
        /// 总共需要下载的Bundle的数量
        /// </summary>
        public int NeedDownLoadBundleCount = 0;
        
        /// <summary>
        /// 下载完成的Bundle的数量
        /// </summary>
        public int FinishDownLoadBundleCount = 0;
    }
}