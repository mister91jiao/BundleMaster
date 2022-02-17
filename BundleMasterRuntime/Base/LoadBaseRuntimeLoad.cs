using System.Collections.Generic;
using UnityEngine;
using ET;

namespace BM
{
    public partial class LoadBase
    {
        /// <summary>
        /// 引用计数
        /// </summary>
        private int _refCount = 0;

        /// <summary>
        /// AssetBundle加载的状态
        /// </summary>
        private LoadState _loadState = LoadState.NoLoad;

        /// <summary>
        /// 加载请求索引
        /// </summary>
        private AssetBundleCreateRequest _assetBundleCreateRequest;
    
        /// <summary>
        /// AssetBundle的引用
        /// </summary>
        public AssetBundle AssetBundle;

        /// <summary>
        /// 加载完成后需要执行的Task
        /// </summary>
        private List<ETTask> _loadFinishTasks = new List<ETTask>();

        public void AddRefCount()
        {
            _refCount++;
            if (_refCount == 1 && _loadState == LoadState.Finish)
            {
                AssetComponent.SubPreUnLoadPool(this);
            }
        }

        public void SubRefCount()
        {
            _refCount--;
            if (_loadState == LoadState.NoLoad)
            {
                AssetLogHelper.LogError("资源未被加载，引用不可能减少\n" + FilePath);
                return;
            }
            if (_loadState == LoadState.Loading)
            {
                AssetLogHelper.Log("资源加载中，等加载完成后再进入卸载逻辑\n" + FilePath);
                return;
            }
            if (_refCount <= 0)
            {
                //需要进入预卸载池等待卸载
                AssetComponent.AddPreUnLoadPool(this);
            }
        }

        public void LoadAssetBundle(string bundlePackageName)
        {
            AddRefCount();
            if (_loadState == LoadState.Finish)
            {
                return;
            }
            if (_loadState == LoadState.Loading)
            {
                AssetLogHelper.LogError("同步加载了正在异步加载的资源, 打断异步加载资源会导致所有异步加载的资源都立刻同步加载出来。资源名: " + FilePath + 
                                        "\nAssetBundle包名: " + AssetBundleName);
                AssetBundle = _assetBundleCreateRequest.assetBundle;
                return;
            }
            string assetBundlePath = AssetComponent.BundleFileExistPath(bundlePackageName, AssetBundleName);
            
            if (AssetComponent.BundleNameToSecretKey.ContainsKey(bundlePackageName))
            {
                AssetBundle = AssetBundle.LoadFromMemory(VerifyHelper.GetDecryptData(assetBundlePath, AssetComponent.BundleNameToSecretKey[bundlePackageName]));
            }
            else
            {
                AssetBundle = AssetBundle.LoadFromFile(assetBundlePath);
            }
            _loadState = LoadState.Finish;
            for (int i = 0; i < _loadFinishTasks.Count; i++)
            {
                _loadFinishTasks[i].SetResult();
            }
            _loadFinishTasks.Clear();
        }
    
        public void LoadAssetBundleAsync(ETTask tcs, string bundlePackageName)
        {
            AddRefCount();
            if (_loadState == LoadState.Finish)
            {
                tcs.SetResult();
                return;
            }
            if (_loadState == LoadState.Loading)
            {
                _loadFinishTasks.Add(tcs);
                return;
            }
            _loadFinishTasks.Add(tcs);
            _loadState = LoadState.Loading;
            string assetBundlePath = AssetComponent.BundleFileExistPath(bundlePackageName, AssetBundleName);
            if (AssetComponent.BundleNameToSecretKey.ContainsKey(bundlePackageName))
            {
                _assetBundleCreateRequest = AssetBundle.LoadFromMemoryAsync(VerifyHelper.GetDecryptData(assetBundlePath, AssetComponent.BundleNameToSecretKey[bundlePackageName]));
            }
            else
            {
                _assetBundleCreateRequest = AssetBundle.LoadFromFileAsync(assetBundlePath);
            }
            _assetBundleCreateRequest.completed += operation =>
            {
                AssetBundle = _assetBundleCreateRequest.assetBundle;
                for (int i = 0; i < _loadFinishTasks.Count; i++)
                {
                    _loadFinishTasks[i].SetResult();
                }
                _loadFinishTasks.Clear();
                _loadState = LoadState.Finish;
                //判断是否还需要
                if (_refCount <= 0)
                {
                    AssetComponent.AddPreUnLoadPool(this);
                }
            };
        }

    }

    /// <summary>
    /// AssetBundle加载的状态
    /// </summary>
    public enum LoadState
    {
        NoLoad = 0,
        Loading = 1,
        Finish = 2
    }
}
