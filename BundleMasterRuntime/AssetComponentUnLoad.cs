using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BM
{
    public static partial class AssetComponent
    {
        /// <summary>
        /// 一个卸载周期的循环时间
        /// </summary>
        private static float _unLoadCirculateTime = 5.0f;
        
        /// <summary>
        /// 预卸载池
        /// </summary>
        private static readonly Dictionary<string, LoadBase> PreUnLoadPool = new Dictionary<string, LoadBase>();
        
        /// <summary>
        /// 真卸载池
        /// </summary>
        private static readonly Dictionary<string, LoadBase> TrueUnLoadPool = new Dictionary<string, LoadBase>();
        
        /// <summary>
        /// 添加进预卸载池
        /// </summary>
        internal static void AddPreUnLoadPool(LoadBase loadBase)
        {
            PreUnLoadPool.Add(loadBase.AssetBundleName, loadBase);
        }

        /// <summary>
        /// 从预卸载池里面取出
        /// </summary>
        internal static void SubPreUnLoadPool(LoadBase loadBase)
        {
            if (PreUnLoadPool.ContainsKey(loadBase.AssetBundleName))
            {
                PreUnLoadPool.Remove(loadBase.AssetBundleName);
            }
            if (TrueUnLoadPool.ContainsKey(loadBase.AssetBundleName))
            {
                TrueUnLoadPool.Remove(loadBase.AssetBundleName);
            }
        }
        
        /// <summary>
        /// 强制卸载所有待卸载的资源
        /// </summary>
        public static void ForceUnLoadAll()
        {
            TrueUnLoadPool.Clear();
            foreach (var loadBase in PreUnLoadPool)
            {
                loadBase.Value.UnLoad();
            }
            PreUnLoadPool.Clear();
        }
        
        /// <summary>
        /// 自动添加到真卸载池子
        /// </summary>
        private static void AutoAddToTrueUnLoadPool()
        {
            //卸载之前就已经添加到真卸载池的资源
            foreach (var loadBase in TrueUnLoadPool)
            {
                PreUnLoadPool.Remove(loadBase.Key);
                loadBase.Value.UnLoad();
            }
            TrueUnLoadPool.Clear();
            foreach (var loadBase in PreUnLoadPool)
            {
                TrueUnLoadPool.Add(loadBase.Key, loadBase.Value);
            }
        }


        /// <summary>
        /// 计时
        /// </summary>
        private static float _timer = 0;
        
        /// <summary>
        /// 卸载周期计时循环
        /// </summary>
        public static void Update()
        {
            _timer += Time.deltaTime;
            if (_timer >= _unLoadCirculateTime)
            {
                _timer = 0;
                AutoAddToTrueUnLoadPool();
            }
        }

        /// <summary>
        /// 取消一个分包的初始化
        /// </summary>
        public static void UnInitialize(string bundlePackageName)
        {
            if (AssetComponentConfig.AssetLoadMode == AssetLoadMode.Develop)
            {
                AssetLogHelper.Log("AssetLoadMode = Develop 无法取消初始化分包");
                return;
            }
            UnInitializePackage(bundlePackageName);
            ForceUnLoadAll();
        }
        
        /// <summary>
        /// 取消一个分包的初始化
        /// </summary>
        public static void UnInitializeAll()
        {
            if (AssetComponentConfig.AssetLoadMode == AssetLoadMode.Develop)
            {
                AssetLogHelper.Log("AssetLoadMode = Develop 无法取消初始化分包");
                return;
            }
            string[] bundlePackageNames = BundleNameToRuntimeInfo.Keys.ToArray();
            for (int i = 0; i < bundlePackageNames.Length; i++)
            {
                UnInitializePackage(bundlePackageNames[i]);
            }
            ForceUnLoadAll();
        }

        private static void UnInitializePackage(string bundlePackageName)
        {
            if (!BundleNameToRuntimeInfo.ContainsKey(bundlePackageName))
            {
                AssetLogHelper.LogError("找不到要取消初始化的分包: " + bundlePackageName);
                return;
            }
            if (BundleNameToSecretKey.ContainsKey(bundlePackageName))
            {
                BundleNameToSecretKey.Remove(bundlePackageName);
            }
            BundleRuntimeInfo bundleRuntimeInfo = BundleNameToRuntimeInfo[bundlePackageName];
            LoadHandlerBase[] loadHandlers = bundleRuntimeInfo.UnLoadHandler.Values.ToArray();
            for (int i = 0; i < loadHandlers.Length; i++)
            {
                loadHandlers[i].UnLoad();
            }
            if (bundleRuntimeInfo.Shader != null)
            {
                bundleRuntimeInfo.Shader.Unload(true);
            }
            BundleNameToRuntimeInfo.Remove(bundlePackageName);
        }
        
    }
}