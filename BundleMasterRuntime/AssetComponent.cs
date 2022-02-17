using ET;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BM
{
    public static partial class AssetComponent
    {
        public static LoadHandler<T> Load<T>(string assetPath, string bundlePackageName = null) where T : UnityEngine.Object
        {
            if (bundlePackageName == null)
            {
                bundlePackageName = AssetComponentConfig.DefaultBundlePackageName;
            }
            LoadHandler<T> loadHandler = new LoadHandler<T>(assetPath, bundlePackageName);
            if (AssetComponentConfig.AssetLoadMode == AssetLoadMode.Develop)
            {
#if UNITY_EDITOR
                loadHandler.Asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
#else
                AssetLogHelper.LogError("加载资源: " + assetPath + " 失败(资源加载Develop模式只能在编辑器下运行)");
#endif
                return loadHandler;
            }
            if (!BundleNameToRuntimeInfo.TryGetValue(bundlePackageName, out BundleRuntimeInfo bundleRuntimeInfo))
            {
                AssetLogHelper.LogError(bundlePackageName + "分包没有初始化");
                return null;
            }
            loadHandler.Load();
            loadHandler.Asset = loadHandler.FileAssetBundle.LoadAsset<T>(assetPath);
            return loadHandler;
        }
    
        /// <summary>
        /// 异步加载
        /// </summary>
        public static async ETTask<LoadHandler<T>> LoadAsync<T>(string assetPath, string bundlePackageName = null) where T : UnityEngine.Object
        {
            if (bundlePackageName == null)
            {
                bundlePackageName = AssetComponentConfig.DefaultBundlePackageName;
            }
            LoadHandler<T> loadHandler = new LoadHandler<T>(assetPath, bundlePackageName);
            if (AssetComponentConfig.AssetLoadMode == AssetLoadMode.Develop)
            {
#if UNITY_EDITOR
                loadHandler.Asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
#else
                AssetLogHelper.LogError("加载资源: " + assetPath + " 失败(资源加载Develop模式只能在编辑器下运行)");
#endif
                return loadHandler;
            }
            if (!BundleNameToRuntimeInfo.TryGetValue(bundlePackageName, out BundleRuntimeInfo bundleRuntimeInfo))
            {
                AssetLogHelper.LogError(bundlePackageName + "分包没有初始化");
                return null;
            }
            await loadHandler.LoadAsync();
            ETTask tcs = ETTask.Create(true);
            AssetBundleRequest loadAssetAsync = loadHandler.FileAssetBundle.LoadAssetAsync<T>(assetPath);
            loadAssetAsync.completed += operation => tcs.SetResult();
            await tcs;
            loadHandler.Asset = loadAssetAsync.asset as T;
            return loadHandler;
        }
        
        /// <summary>
        /// 同步加载场景的AssetBundle包
        /// </summary>
        public static LoadSceneHandler LoadScene(string scenePath, string bundlePackageName = null)
        {
            if (bundlePackageName == null)
            {
                bundlePackageName = AssetComponentConfig.DefaultBundlePackageName;
            }
            LoadSceneHandler loadSceneHandler = new LoadSceneHandler(scenePath, bundlePackageName);
            if (AssetComponentConfig.AssetLoadMode == AssetLoadMode.Develop)
            {
                //Develop模式加载场景待写
                
                return loadSceneHandler;
            }
            if (!BundleNameToRuntimeInfo.TryGetValue(bundlePackageName, out BundleRuntimeInfo bundleRuntimeInfo))
            {
                AssetLogHelper.LogError(bundlePackageName + "分包没有初始化");
                return null;
            }
            loadSceneHandler.LoadSceneBundle();
            return loadSceneHandler;
        }
        
        /// <summary>
        /// 异步加载场景的AssetBundle包
        /// </summary>
        public static async ETTask<LoadSceneHandler> LoadSceneAsync(string scenePath, string bundlePackageName = null)
        {
            if (bundlePackageName == null)
            {
                bundlePackageName = AssetComponentConfig.DefaultBundlePackageName;
            }
            LoadSceneHandler loadSceneHandler = new LoadSceneHandler(scenePath, bundlePackageName);
            if (AssetComponentConfig.AssetLoadMode == AssetLoadMode.Develop)
            {
                //Develop模式不需要加载场景
                return loadSceneHandler;
            }
            if (!BundleNameToRuntimeInfo.TryGetValue(bundlePackageName, out BundleRuntimeInfo bundleRuntimeInfo))
            {
                AssetLogHelper.LogError(bundlePackageName + "分包没有初始化");
                return null;
            }
            await loadSceneHandler.LoadSceneBundleAsync();
            return loadSceneHandler;
        }
        
    }

    public enum AssetLoadMode
    {
        /// <summary>
        /// 开发模式
        /// </summary>
        Develop = 0,
        
        /// <summary>
        /// 本地调试模式(需要打包，直接加载最新Bundle，不走热更逻辑)
        /// </summary>
        Local = 1,
        
        /// <summary>
        /// 发布模式(需要打包，走版本对比更新流程)
        /// </summary>
        Build = 2,
    }
    
}


