using UnityEngine;

namespace BM
{
    /// <summary>
    /// 资源加载初始化所需配置信息
    /// </summary>
    public class AssetComponentConfig
    {
        /// <summary>
        /// 运行时配置
        /// </summary>
        private static BundleMasterRuntimeConfig _bmRuntimeConfig = null;

        /// <summary>
        /// 资源更新目录 Application.dataPath + "/../HotfixBundles/"
        /// </summary>
        public static string HotfixPath = Application.persistentDataPath;

        /// <summary>
        /// 存放本地Bundle的位置 Application.streamingAssetsPath;
        /// </summary>
        public static string LocalBundlePath = Application.streamingAssetsPath;

        /// <summary>
        /// 资源服务器的地址 http://192.168.50.157/BundleData/
        /// </summary>
        public static string BundleServerUrl = "http://192.168.50.157/BundleData/";
        
        /// <summary>
        /// 默认加载的Bundle名
        /// </summary>
        public static string DefaultBundlePackageName = "";
        
        private static void InitRuntimeConfig()
        {
            if (_bmRuntimeConfig == null)
            {
                _bmRuntimeConfig = Resources.Load<BundleMasterRuntimeConfig>("BMConfig");
            }
        }
        
        /// <summary>
        /// 加载模式
        /// </summary>
        public static AssetLoadMode AssetLoadMode
        {
            get
            {
                InitRuntimeConfig();
                return _bmRuntimeConfig.AssetLoadMode;
            }
        }

        /// <summary>
        /// 最大同时下载的资源数量
        /// </summary>
        public static int MaxDownLoadCount
        {
            get
            {
                InitRuntimeConfig();
                return _bmRuntimeConfig.MaxDownLoadCount;
            }
        }

        /// <summary>
        /// 下载失败最多重试次数
        /// </summary>
        public static int ReDownLoadCount
        {
            get
            {
                InitRuntimeConfig();
                return _bmRuntimeConfig.ReDownLoadCount;
            }
        }
        
    }
}