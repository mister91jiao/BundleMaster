using ET;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BM
{
    public static partial class AssetComponent
    {
        /// <summary>
        /// 同步加载(泛型)
        /// </summary>
        public static T Load<T>(string assetPath, string bundlePackageName = null) where T : UnityEngine.Object
        {
            T asset = null;
            if (bundlePackageName == null)
            {
                bundlePackageName = AssetComponentConfig.DefaultBundlePackageName;
            }
            if (AssetComponentConfig.AssetLoadMode == AssetLoadMode.Develop)
            {
#if UNITY_EDITOR
                asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
#else
                AssetLogHelper.LogError("加载资源: " + assetPath + " 失败(资源加载Develop模式只能在编辑器下运行)");
#endif
                return asset;
            }
            LoadHandler loadHandler = null;
            if (!BundleNameToRuntimeInfo.TryGetValue(bundlePackageName, out BundleRuntimeInfo bundleRuntimeInfo))
            {
                AssetLogHelper.LogError(bundlePackageName + "分包没有初始化");
                return null;
            }
            
            if (!bundleRuntimeInfo.AllAssetLoadHandler.TryGetValue(assetPath, out loadHandler))
            {
                loadHandler = LoadHandlerFactory.GetLoadHandler(assetPath, bundlePackageName);
                bundleRuntimeInfo.AllAssetLoadHandler.Add(assetPath, loadHandler);
                bundleRuntimeInfo.UnLoadHandler.Add(loadHandler.UniqueId, loadHandler);
            }
            if (loadHandler.LoadState == LoadState.NoLoad)
            {
                loadHandler.Load();
                asset = loadHandler.FileAssetBundle.LoadAsset<T>(assetPath);
                loadHandler.Asset = asset;
                return asset;
            }
            else if (loadHandler.LoadState == LoadState.Loading)
            {
                loadHandler.ForceAsyncLoadFinish();
                asset = loadHandler.FileAssetBundle.LoadAsset<T>(assetPath);
                loadHandler.Asset = asset;
            }
            if (loadHandler.LoadState == LoadState.Finish)
            {
                asset = loadHandler.Asset as T;
            }
            return asset;
        }
        
        /// <summary>
        /// 同步加载
        /// </summary>
        public static UnityEngine.Object Load(string assetPath, string bundlePackageName = null)
        {
           
            if (bundlePackageName == null)
            {
                bundlePackageName = AssetComponentConfig.DefaultBundlePackageName;
            }
            if (AssetComponentConfig.AssetLoadMode == AssetLoadMode.Develop)
            {
#if UNITY_EDITOR
                UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath(assetPath, typeof(UnityEngine.Object));
#else
                UnityEngine.Object asset = null;
                AssetLogHelper.LogError("加载资源: " + assetPath + " 失败(资源加载Develop模式只能在编辑器下运行)");
#endif
                return asset;
            }
            LoadHandler loadHandler = null;
            if (!BundleNameToRuntimeInfo.TryGetValue(bundlePackageName, out BundleRuntimeInfo bundleRuntimeInfo))
            {
                AssetLogHelper.LogError(bundlePackageName + "分包没有初始化");
                return null;
            }
            if (!bundleRuntimeInfo.AllAssetLoadHandler.TryGetValue(assetPath, out loadHandler))
            {
                loadHandler = LoadHandlerFactory.GetLoadHandler(assetPath, bundlePackageName);
                bundleRuntimeInfo.AllAssetLoadHandler.Add(assetPath, loadHandler);
                bundleRuntimeInfo.UnLoadHandler.Add(loadHandler.UniqueId, loadHandler);
            }
            if (loadHandler.LoadState == LoadState.NoLoad)
            {
                loadHandler.Load();
                loadHandler.Asset = loadHandler.FileAssetBundle.LoadAsset(assetPath);
                return loadHandler.Asset;
            }
            else if (loadHandler.LoadState == LoadState.Loading)
            {
                loadHandler.ForceAsyncLoadFinish();
                loadHandler.Asset = loadHandler.FileAssetBundle.LoadAsset(assetPath);
            }
            return loadHandler.Asset;
        }
    
        /// <summary>
        /// 异步加载(泛型)
        /// </summary>
        public static async ETTask<T> LoadAsync<T>(string assetPath, string bundlePackageName = null) where T : UnityEngine.Object
        {
            if (bundlePackageName == null)
            {
                bundlePackageName = AssetComponentConfig.DefaultBundlePackageName;
            }
            
            T asset = null;
            if (AssetComponentConfig.AssetLoadMode == AssetLoadMode.Develop)
            {
#if UNITY_EDITOR
                asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
#else
                AssetLogHelper.LogError("加载资源: " + assetPath + " 失败(资源加载Develop模式只能在编辑器下运行)");
#endif
                return asset;
            }
            LoadHandler loadHandler = null;
            if (!BundleNameToRuntimeInfo.TryGetValue(bundlePackageName, out BundleRuntimeInfo bundleRuntimeInfo))
            {
                AssetLogHelper.LogError(bundlePackageName + "分包没有初始化");
                return null;
            }
            if (!bundleRuntimeInfo.AllAssetLoadHandler.TryGetValue(assetPath, out loadHandler))
            {
                loadHandler = LoadHandlerFactory.GetLoadHandler(assetPath, bundlePackageName);
                bundleRuntimeInfo.AllAssetLoadHandler.Add(assetPath, loadHandler);
                bundleRuntimeInfo.UnLoadHandler.Add(loadHandler.UniqueId, loadHandler);
            }
            if (loadHandler.LoadState == LoadState.NoLoad)
            {
                ETTask tcs = ETTask.Create(true);
                loadHandler.AwaitEtTasks.Add(tcs);
                await loadHandler.LoadAsync();
                AssetBundleRequest loadAssetAsync = loadHandler.FileAssetBundle.LoadAssetAsync<T>(assetPath);
                loadAssetAsync.completed += operation =>
                {
                    loadHandler.Asset = loadAssetAsync.asset;
                    asset = loadHandler.Asset as T;
                    for (int i = 0; i < loadHandler.AwaitEtTasks.Count; i++)
                    {
                        ETTask etTask = loadHandler.AwaitEtTasks[i];
                        etTask.SetResult();
                    }
                    loadHandler.AwaitEtTasks.Clear();
                };
                await tcs;
                return asset;
            }
            else if (loadHandler.LoadState == LoadState.Loading)
            {
                ETTask tcs = ETTask.Create(true);
                loadHandler.AwaitEtTasks.Add(tcs);
                await tcs;
                return loadHandler.Asset as T;
            }
            else
            {
                return loadHandler.Asset as T;
            }
        }
        
        /// <summary>
        /// 异步加载
        /// </summary>
        public static async ETTask<UnityEngine.Object> LoadAsync(string assetPath, string bundlePackageName = null)
        {
            if (bundlePackageName == null)
            {
                bundlePackageName = AssetComponentConfig.DefaultBundlePackageName;
            }
            if (AssetComponentConfig.AssetLoadMode == AssetLoadMode.Develop)
            {
#if UNITY_EDITOR
                UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath(assetPath, typeof(UnityEngine.Object));
#else
                UnityEngine.Object asset = null;
                AssetLogHelper.LogError("加载资源: " + assetPath + " 失败(资源加载Develop模式只能在编辑器下运行)");
#endif
                return asset;
            }
            LoadHandler loadHandler = null;
            if (!BundleNameToRuntimeInfo.TryGetValue(bundlePackageName, out BundleRuntimeInfo bundleRuntimeInfo))
            {
                AssetLogHelper.LogError(bundlePackageName + "分包没有初始化");
                return null;
            }
            if (!bundleRuntimeInfo.AllAssetLoadHandler.TryGetValue(assetPath, out loadHandler))
            {
                loadHandler = LoadHandlerFactory.GetLoadHandler(assetPath, bundlePackageName);
                bundleRuntimeInfo.AllAssetLoadHandler.Add(assetPath, loadHandler);
                bundleRuntimeInfo.UnLoadHandler.Add(loadHandler.UniqueId, loadHandler);
            }
            if (loadHandler.LoadState == LoadState.NoLoad)
            {
                ETTask tcs = ETTask.Create(true);
                loadHandler.AwaitEtTasks.Add(tcs);
                await loadHandler.LoadAsync();
                AssetBundleRequest loadAssetAsync = loadHandler.FileAssetBundle.LoadAssetAsync(assetPath);
                loadAssetAsync.completed += operation =>
                {
                    loadHandler.Asset = loadAssetAsync.asset;
                    for (int i = 0; i < loadHandler.AwaitEtTasks.Count; i++)
                    {
                        ETTask etTask = loadHandler.AwaitEtTasks[i];
                        etTask.SetResult();
                    }
                    loadHandler.AwaitEtTasks.Clear();
                };
                await tcs;
                return loadHandler.Asset;
            }
            else if (loadHandler.LoadState == LoadState.Loading)
            {
                ETTask tcs = ETTask.Create(true);
                loadHandler.AwaitEtTasks.Add(tcs);
                await tcs;
                return loadHandler.Asset;
            }
            else
            {
                return loadHandler.Asset;
            }
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
                //Develop模式可以直接加载场景
                return loadSceneHandler;
            }
            if (!BundleNameToRuntimeInfo.TryGetValue(bundlePackageName, out BundleRuntimeInfo bundleRuntimeInfo))
            {
                AssetLogHelper.LogError(bundlePackageName + "分包没有初始化");
                return null;
            }
            bundleRuntimeInfo.UnLoadHandler.Add(loadSceneHandler.UniqueId, loadSceneHandler);
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
            bundleRuntimeInfo.UnLoadHandler.Add(loadSceneHandler.UniqueId, loadSceneHandler);
            ETTask tcs = ETTask.Create();
            await loadSceneHandler.LoadSceneBundleAsync(tcs);
            return loadSceneHandler;
        }

        /// <summary>
        /// 异步加载场景的AssetBundle包 提前返回LoadSceneHandler计算进度用
        /// </summary>
        public static ETTask LoadSceneAsync(out LoadSceneHandler loadSceneHandler, string scenePath, string bundlePackageName = null)
        {
            ETTask tcs = ETTask.Create();
            if (bundlePackageName == null)
            {
                bundlePackageName = AssetComponentConfig.DefaultBundlePackageName;
            }
            loadSceneHandler = new LoadSceneHandler(scenePath, bundlePackageName);
            if (AssetComponentConfig.AssetLoadMode == AssetLoadMode.Develop)
            {
                //Develop模式不需要加载场景
                tcs.SetResult();
                return tcs;
            }
            if (!BundleNameToRuntimeInfo.TryGetValue(bundlePackageName, out BundleRuntimeInfo bundleRuntimeInfo))
            {
                AssetLogHelper.LogError(bundlePackageName + "分包没有初始化");
                return null;
            }
            bundleRuntimeInfo.UnLoadHandler.Add(loadSceneHandler.UniqueId, loadSceneHandler);
            loadSceneHandler.LoadSceneBundleAsync(tcs).Coroutine();
            return tcs;
        }

        /// <summary>
        /// 获取一个已经初始化完成的分包的信息
        /// </summary>
        public static BundleRuntimeInfo GetBundleRuntimeInfo(string bundlePackageName)
        {
            
            if (AssetComponentConfig.AssetLoadMode == AssetLoadMode.Develop)
            {
#if UNITY_EDITOR
                BundleRuntimeInfo devBundleRuntimeInfo;
                if (!BundleNameToRuntimeInfo.TryGetValue(bundlePackageName, out devBundleRuntimeInfo))
                {
                    devBundleRuntimeInfo = new BundleRuntimeInfo(bundlePackageName);
                    BundleNameToRuntimeInfo.Add(bundlePackageName, devBundleRuntimeInfo);
                }
                return devBundleRuntimeInfo;
#else
                AssetLogHelper.LogError("资源加载Develop模式只能在编辑器下运行");
#endif
               
            }
            if (BundleNameToRuntimeInfo.TryGetValue(bundlePackageName, out BundleRuntimeInfo bundleRuntimeInfo))
            {
                return bundleRuntimeInfo;
            }
            else
            {
                AssetLogHelper.LogError("初始化的分包里没有这个分包: " + bundlePackageName);
                return null;
            }
        }
        
    }

    public enum AssetLoadMode
    {
        /// <summary>
        /// 开发模式(无需打包，编辑器下AssetDatabase加载)
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


