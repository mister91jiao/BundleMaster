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
        internal static string BundleFileExistPath(string bundlePackageName, string fileName, bool isWebLoad)
        {
            string path = GetBasePath(bundlePackageName, fileName);
            if (isWebLoad)
            {
                //通过webReq加载
#if UNITY_ANDROID && !UNITY_EDITOR
                if (!path.Contains("file:///"))
                {
                    path = "file://" + path;
                }
#elif UNITY_IOS && !UNITY_EDITOR
                path = "file://" + path;
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
                path = "file://" + path;
#elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
#else
#endif
                return path;
            }
            else
            {
                //直接加载
#if UNITY_ANDROID && !UNITY_EDITOR
#elif UNITY_IOS && !UNITY_EDITOR
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
#elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
#else  
#endif
                return path;
            }
        }

        /// <summary>
        /// 得到基础的路径
        /// </summary>
        private static string GetBasePath(string bundlePackageName, string fileName)
        {
            if (AssetComponentConfig.AssetLoadMode == AssetLoadMode.Local)
            {
                string path = Path.Combine(AssetComponentConfig.LocalBundlePath, bundlePackageName, fileName);
                return path;
            }
            else
            {
                string path = Path.Combine(AssetComponentConfig.HotfixPath, bundlePackageName, fileName);
                if (!File.Exists(path))
                {
                    //热更目录不存在，返回streaming目录
                    path = Path.Combine(AssetComponentConfig.LocalBundlePath, bundlePackageName, fileName);
                }
                //热更目录存在，返回热更目录
                return path;
            }
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
        private static void CreateUpdateLogFile(string filePath, string fileData)
        {
            using (StreamWriter sw = new StreamWriter(filePath))
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(fileData);
                sw.WriteLine(sb.ToString());
            }
        }
        
    }

}