﻿using System.Collections.Generic;
using UnityEngine;
using ET;

namespace BM
{
    public class LoadSceneHandler : LoadHandlerBase
    {
        /// <summary>
        /// File文件AssetBundle的引用
        /// </summary>
        public AssetBundle FileAssetBundle;
    
        /// <summary>
        /// 资源所在的File包
        /// </summary>
        private LoadFile _loadFile = null;
    
        /// <summary>
        /// 依赖的Bundle包
        /// </summary>
        private List<LoadDepend> _loadDepends = new List<LoadDepend>();
    
        /// <summary>
        /// 依赖的其它File包
        /// </summary>
        private List<LoadFile> _loadDependFiles = new List<LoadFile>();
        
        public LoadSceneHandler(string scenePath, string bundlePackageName)
        {
            AssetPath = scenePath;
            BundlePackageName = bundlePackageName;
            if (AssetComponentConfig.AssetLoadMode == AssetLoadMode.Develop)
            {
                //Develop模式直接返回
                return;
            }
            //先找到对应加载的LoadFile类
            if (!AssetComponent.BundleNameToRuntimeInfo[BundlePackageName].LoadFileDic.TryGetValue(AssetPath, out LoadFile loadFile))
            {
                AssetLogHelper.LogError("没有找到资源: " + AssetPath);
                return;
            }
            _loadFile = loadFile;
            //需要记录loadFile的依赖
            for (int i = 0; i < _loadFile.DependFileName.Length; i++)
            {
                string dependFile = _loadFile.DependFileName[i];
                if (AssetComponent.BundleNameToRuntimeInfo[BundlePackageName].LoadDependDic.TryGetValue(dependFile, out LoadDepend loadDepend))
                {
                    _loadDepends.Add(loadDepend);
                    continue;
                }
                if (AssetComponent.BundleNameToRuntimeInfo[BundlePackageName].LoadFileDic.TryGetValue(dependFile, out LoadFile loadDependFile))
                {
                    _loadDependFiles.Add(loadDependFile);
                    continue;
                }
                AssetLogHelper.LogError("依赖的资源没有找到对应的类: " + dependFile);
            }
            
        }

        /// <summary>
        /// 同步加载场景资源所需的的AssetBundle包
        /// </summary>
        public void LoadSceneBundle()
        {
            _loadFile.LoadAssetBundle(BundlePackageName);
            for (int i = 0; i < _loadDepends.Count; i++)
            {
                _loadDepends[i].LoadAssetBundle(BundlePackageName);
            }
            for (int i = 0; i < _loadDependFiles.Count; i++)
            {
                _loadDependFiles[i].LoadAssetBundle(BundlePackageName);
            }
            FileAssetBundle = _loadFile.AssetBundle;
        }
        
        /// <summary>
        /// 异步加载场景的Bundle
        /// </summary>
        public async ETTask LoadSceneBundleAsync()
        {
            //计算出所有需要加载的Bundle包的总数
            RefLoadFinishCount = _loadDepends.Count + _loadDependFiles.Count + 1;
            ETTask tcs = ETTask.Create(true);
            LoadAsyncLoader(_loadFile, tcs).Coroutine();
            for (int i = 0; i < _loadDepends.Count; i++)
            {
                LoadAsyncLoader(_loadDepends[i], tcs).Coroutine();
            }
            for (int i = 0; i < _loadDependFiles.Count; i++)
            {
                LoadAsyncLoader(_loadDependFiles[i], tcs).Coroutine();
            }
            await tcs;
            FileAssetBundle = _loadFile.AssetBundle;
        }
        
        /// <summary>
        /// 卸载场景的AssetBundle包
        /// </summary>
        protected override void ClearAsset()
        {
            FileAssetBundle = null;
            foreach (LoadDepend loadDepends in _loadDepends)
            {
                loadDepends.SubRefCount();
            }
            _loadDepends.Clear();
            foreach (LoadFile loadDependFiles in _loadDependFiles)
            {
                loadDependFiles.SubRefCount();
            }
            _loadDependFiles.Clear();
            _loadFile.SubRefCount();
            _loadFile = null;
        }
    }
}