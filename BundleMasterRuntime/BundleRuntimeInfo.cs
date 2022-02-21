using System.Collections.Generic;
using ET;
using UnityEngine;

namespace BM
{
    /// <summary>
    /// 存放一个Bundle分包的初始化信息
    /// </summary>
    public class BundleRuntimeInfo
    {
        /// <summary>
        /// 主动加载的文件
        /// </summary>
        internal readonly Dictionary<string, LoadFile> LoadFileDic = new Dictionary<string, LoadFile>();
        
        /// <summary>
        /// 依赖加载的文件
        /// </summary>
        internal readonly Dictionary<string, LoadDepend> LoadDependDic = new Dictionary<string, LoadDepend>();
        
        /// <summary>
        /// 所有没有卸载的LoadHandler
        /// </summary>
        internal readonly Dictionary<uint, LoadHandlerBase> UnLoadHandler = new Dictionary<uint, LoadHandlerBase>();

        /// <summary>
        /// 分包的名称
        /// </summary>
        private string BundlePackageName;

        /// <summary>
        /// 分包是否加密
        /// </summary>
        internal bool Encrypt = false;
        
        /// <summary>
        /// 分包的加密Key
        /// </summary>
        internal char[] SecretKey = null;
        
        /// <summary>
        /// Shader的AssetBundle
        /// </summary>
        internal AssetBundle Shader = null;

        public BundleRuntimeInfo(string bundlePackageName, string secretKey = null)
        {
            BundlePackageName = bundlePackageName;
            if (secretKey != null)
            {
                SecretKey = secretKey.ToCharArray();
                Encrypt = true;
            }
        }
        
        public LoadHandler<T> Load<T>(string assetPath) where T : UnityEngine.Object => AssetComponent.Load<T>(assetPath, BundlePackageName);
        public async ETTask<LoadHandler<T>> LoadAsync<T>(string assetPath) where T : UnityEngine.Object
        {
            LoadHandler<T> loadHandler = await AssetComponent.LoadAsync<T>(assetPath, BundlePackageName);
            return loadHandler;
        }
        
        public LoadSceneHandler LoadScene(string scenePath) => AssetComponent.LoadScene(scenePath, BundlePackageName);
        public async ETTask<LoadSceneHandler> LoadSceneAsync(string scenePath)
        {
            LoadSceneHandler loadSceneHandler = await AssetComponent.LoadSceneAsync(scenePath, BundlePackageName);
            return loadSceneHandler;
        }
        public ETTask LoadSceneAsync(out LoadSceneHandler loadSceneHandler, string scenePath) => AssetComponent.LoadSceneAsync(out loadSceneHandler, scenePath, BundlePackageName);
    }
}