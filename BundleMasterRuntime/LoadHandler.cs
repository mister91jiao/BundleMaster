using System;
using System.Collections.Generic;
using ET;

namespace BM
{
    public class LoadHandler : LoadHandlerBase
    {
        internal Action<LoadHandler> CompleteCallback;
        public event Action<LoadHandler> Completed
        {
            add
            {
                if (Asset != null)
                {
                    value(this);
                }
                else
                {
                    this.CompleteCallback += value;
                }
            }
            remove => this.CompleteCallback -= value;
        }
        
        /// <summary>
        /// 是否进池
        /// </summary>
        private bool isPool;
        
        /// <summary>
        /// 加载出来的资源
        /// </summary>
        public UnityEngine.Object Asset = null;

        /// <summary>
        /// 加载的状态
        /// </summary>
        internal LoadState LoadState = LoadState.NoLoad;

        internal LoadHandler(bool isPool)
        {
            this.isPool = isPool;
        }
        
        internal void Init(string assetPath, string bundlePackageName, bool haveHandler)
        {
            AssetPath = assetPath;
            UniqueId = HandlerIdHelper.GetUniqueId();
            BundlePackageName = bundlePackageName;
            LoadState = LoadState.NoLoad;
            UnloadFinish = false;
            HaveHandler = haveHandler;
            if (AssetComponentConfig.AssetLoadMode == AssetLoadMode.Develop)
            {
                //Develop模式直接返回就行
                return;
            }
            //说明是组里的资源
            string groupPath = GroupAssetHelper.IsGroupAsset(AssetPath, AssetComponent.BundleNameToRuntimeInfo[BundlePackageName].LoadGroupDicKey);
            if (groupPath != null)
            {
                //先找到对应加载的LoadGroup类
                if (!AssetComponent.BundleNameToRuntimeInfo[BundlePackageName].LoadGroupDic.TryGetValue(groupPath, out LoadGroup loadGroup))
                {
                    AssetLogHelper.LogError("没有找到资源组: " + groupPath);
                    return;
                }
                LoadBase = loadGroup;
                //需要记录loadGroup的依赖
                for (int i = 0; i < loadGroup.DependFileName.Count; i++)
                {
                    string dependFile = loadGroup.DependFileName[i];
                    if (AssetComponent.BundleNameToRuntimeInfo[BundlePackageName].LoadDependDic.TryGetValue(dependFile, out LoadDepend loadDepend))
                    {
                        LoadDepends.Add(loadDepend);
                        continue;
                    }
                    if (AssetComponent.BundleNameToRuntimeInfo[BundlePackageName].LoadFileDic.TryGetValue(dependFile, out LoadFile loadDependFile))
                    {
                        LoadDependFiles.Add(loadDependFile);
                        continue;
                    }
                    if (AssetComponent.BundleNameToRuntimeInfo[BundlePackageName].LoadGroupDic.TryGetValue(dependFile, out LoadGroup loadDependGroup))
                    {
                        LoadDependGroups.Add(loadDependGroup);
                        continue;
                    }
                    AssetLogHelper.LogError("依赖的资源没有找到对应的类: " + dependFile);
                }
                return;
            }
            //先找到对应加载的LoadFile类
            if (!AssetComponent.BundleNameToRuntimeInfo[BundlePackageName].LoadFileDic.TryGetValue(AssetPath, out LoadFile loadFile))
            {
                AssetLogHelper.LogError("没有找到资源: " + AssetPath);
                return;
            }
            LoadBase = loadFile;
            //需要记录loadFile的依赖
            for (int i = 0; i < loadFile.DependFileName.Length; i++)
            {
                string dependFile = loadFile.DependFileName[i];
                if (AssetComponent.BundleNameToRuntimeInfo[BundlePackageName].LoadDependDic.TryGetValue(dependFile, out LoadDepend loadDepend))
                {
                    LoadDepends.Add(loadDepend);
                    continue;
                }
                if (AssetComponent.BundleNameToRuntimeInfo[BundlePackageName].LoadFileDic.TryGetValue(dependFile, out LoadFile loadDependFile))
                {
                    LoadDependFiles.Add(loadDependFile);
                    continue;
                }
                if (AssetComponent.BundleNameToRuntimeInfo[BundlePackageName].LoadGroupDic.TryGetValue(dependFile, out LoadGroup loadDependGroup))
                {
                    LoadDependGroups.Add(loadDependGroup);
                    continue;
                }
                AssetLogHelper.LogError("依赖的资源没有找到对应的类: " + dependFile);
            }
        }
    
        /// <summary>
        /// 同步加载所有的Bundle
        /// </summary>
        internal void Load()
        {
            LoadBase.LoadAssetBundle(BundlePackageName);
            for (int i = 0; i < LoadDepends.Count; i++)
            {
                LoadDepends[i].LoadAssetBundle(BundlePackageName);
            }
            for (int i = 0; i < LoadDependFiles.Count; i++)
            {
                LoadDependFiles[i].LoadAssetBundle(BundlePackageName);
            }
            for (int i = 0; i < LoadDependGroups.Count; i++)
            {
                LoadDependGroups[i].LoadAssetBundle(BundlePackageName);
            }
            FileAssetBundle = LoadBase.AssetBundle;
            LoadState = LoadState.Finish;
        }

        /// <summary>
        /// 异步加载所有的Bundle
        /// </summary>
        internal async ETTask LoadAsync()
        {
            LoadState = LoadState.Loading;
            //计算出所有需要加载的Bundle包的总数
            RefLoadFinishCount = LoadDepends.Count + LoadDependFiles.Count + LoadDependGroups.Count + 1;
            ETTask tcs = ETTask.Create(true);
            LoadAsyncLoader(LoadBase, tcs).Coroutine();
            for (int i = 0; i < LoadDepends.Count; i++)
            {
                LoadAsyncLoader(LoadDepends[i], tcs).Coroutine();
            }
            for (int i = 0; i < LoadDependFiles.Count; i++)
            {
                LoadAsyncLoader(LoadDependFiles[i], tcs).Coroutine();
            }
            for (int i = 0; i < LoadDependGroups.Count; i++)
            {
                LoadAsyncLoader(LoadDependGroups[i], tcs).Coroutine();
            }
            await tcs;
            if (LoadState == LoadState.Finish)
            {
                AssetLogHelper.Log("此资源异步加载时触发了强制加载: " + AssetPath);
            }
            LoadState = LoadState.Finish;
            FileAssetBundle = LoadBase.AssetBundle;
        }
        
        /// <summary>
        /// 强制异步加载完成
        /// </summary>
        internal void ForceAsyncLoadFinish()
        {
            LoadBase.ForceLoadFinish(BundlePackageName);
            for (int i = 0; i < LoadDepends.Count; i++)
            {
                LoadDepends[i].ForceLoadFinish(BundlePackageName);
            }
            for (int i = 0; i < LoadDependFiles.Count; i++)
            {
                LoadDependFiles[i].ForceLoadFinish(BundlePackageName);
            }
            for (int i = 0; i < LoadDependGroups.Count; i++)
            {
                LoadDependGroups[i].ForceLoadFinish(BundlePackageName);
            }
            FileAssetBundle = LoadBase.AssetBundle;
            LoadState = LoadState.Finish;
        }
        
        /// <summary>
        /// 清理引用
        /// </summary>
        protected override void ClearAsset()
        {
            Asset = null;
            FileAssetBundle = null;
            LoadState = LoadState.NoLoad;
            foreach (LoadDepend loadDepends in LoadDepends)
            {
                loadDepends.SubRefCount();
            }
            LoadDepends.Clear();
            foreach (LoadFile loadDependFiles in LoadDependFiles)
            {
                loadDependFiles.SubRefCount();
            }
            LoadDependFiles.Clear();
            foreach (LoadGroup loadDependGroups in LoadDependGroups)
            {
                loadDependGroups.SubRefCount();
            }
            LoadDependGroups.Clear();
            LoadBase.SubRefCount();
            LoadBase = null;
            //从缓存里取出进池
            if (!AssetComponent.BundleNameToRuntimeInfo.TryGetValue(BundlePackageName, out BundleRuntimeInfo bundleRuntimeInfo))
            {
                AssetLogHelper.LogError("没要找到分包的信息: " + BundlePackageName);
                return;
            }
            if (HaveHandler)
            {
                if (!bundleRuntimeInfo.AllAssetLoadHandler.ContainsKey(AssetPath))
                {
                    AssetLogHelper.LogError("没要找到缓存的LoadHandler: " + AssetPath);
                    return;
                }
                bundleRuntimeInfo.AllAssetLoadHandler.Remove(AssetPath);
            }
            CompleteCallback = null;
            if (isPool)
            {
                //进池
                LoadHandlerFactory.EnterPool(this);
            }
        }
        
    }
}