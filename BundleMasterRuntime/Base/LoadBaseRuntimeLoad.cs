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
        public AssetBundle AssetBundle = null;

        /// <summary>
        /// 需要统计进度
        /// </summary>
        private WebLoadProgress _loadProgress = null;

        private void AddRefCount()
        {
            _refCount++;
            if (_refCount == 1 && _loadState == LoadState.Finish)
            {
                AssetComponent.SubPreUnLoadPool(this);
            }
        }

        internal void SubRefCount()
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

        internal void LoadAssetBundle(string bundlePackageName)
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
                if (_assetBundleCreateRequest != null)
                {
                    AssetBundle = _assetBundleCreateRequest.assetBundle;
                    return;
                }
            }
            //资源没有加载过也没有正在加载就同步加载出来
            if (AssetComponent.BundleNameToRuntimeInfo[bundlePackageName].Encrypt)
            {
                string assetBundlePath = AssetComponent.BundleFileExistPath(bundlePackageName, AssetBundleName, true);
                byte[] data = VerifyHelper.GetDecryptData(assetBundlePath, AssetComponent.BundleNameToRuntimeInfo[bundlePackageName].SecretKey);
                AssetBundle = AssetBundle.LoadFromMemory(data);
            }
            else
            {
                string assetBundlePath = AssetComponent.BundleFileExistPath(bundlePackageName, AssetBundleName, false);
                AssetBundle = AssetBundle.LoadFromFile(assetBundlePath);
            }
            _loadState = LoadState.Finish;
        }
    
        /// <summary>
        /// 异步加载LoadBase的AssetBundle
        /// </summary>
        internal async ETTask LoadAssetBundleAsync(string bundlePackageName)
        {
            AddRefCount();
            if (_loadState == LoadState.Finish)
            {
                return;
            }
            BundleRuntimeInfo bundleRuntimeInfo = AssetComponent.BundleNameToRuntimeInfo[bundlePackageName];
            string assetBundlePath = AssetComponent.BundleFileExistPath(bundlePackageName, AssetBundleName, bundleRuntimeInfo.Encrypt);
            //获取一个协程锁
            CoroutineLock coroutineLock = await CoroutineLockComponent.Wait(CoroutineLockType.BundleMaster, LoadPathConvertHelper.LoadPathConvert(assetBundlePath));
            if (_loadState == LoadState.NoLoad)
            {
                _loadState = LoadState.Loading;
                if (bundleRuntimeInfo.Encrypt)
                {
                    await LoadDataFinish(assetBundlePath, bundleRuntimeInfo.SecretKey);
                }
                else
                {
                    await LoadBundleFinish(assetBundlePath);
                }
                _loadState = LoadState.Finish;
            }
            //协程锁解锁
            coroutineLock.Dispose();
        }

        /// <summary>
        /// 通过Byte加载完成(只有启用了异或加密才使用此加载方式)
        /// </summary>
        private async ETTask LoadDataFinish(string assetBundlePath, char[] bundlePackageSecretKey)
        {
            byte[] data = await VerifyHelper.GetDecryptDataAsync(assetBundlePath, _loadProgress, bundlePackageSecretKey);
            if (_loadState == LoadState.Finish)
            {
                return;
            }
            ETTask tcs = ETTask.Create(true);
            _assetBundleCreateRequest = AssetBundle.LoadFromMemoryAsync(data);
            _assetBundleCreateRequest.completed += operation =>
            {
                AssetBundle = _assetBundleCreateRequest.assetBundle;
                tcs.SetResult();
                //判断是否还需要
                if (_refCount <= 0)
                {
                    AssetComponent.AddPreUnLoadPool(this);
                }
            };
            await tcs;
        }
        
        /// <summary>
        /// 通过路径直接加载硬盘上的AssetBundle
        /// </summary>
        private async ETTask LoadBundleFinish(string assetBundlePath)
        {
            if (_loadState == LoadState.Finish)
            {
                return;
            }
            ETTask tcs = ETTask.Create(true);
            _assetBundleCreateRequest = AssetBundle.LoadFromFileAsync(assetBundlePath);
            _assetBundleCreateRequest.completed += operation =>
            {
                AssetBundle = _assetBundleCreateRequest.assetBundle;
                tcs.SetResult();
                //判断是否还需要
                if (_refCount <= 0)
                {
                    AssetComponent.AddPreUnLoadPool(this);
                }
            };
            await tcs;
        }
        
        /// <summary>
        /// 强制加载完成
        /// </summary>
        internal void ForceLoadFinish(string bundlePackageName)
        {
            if (_loadState == LoadState.Finish)
            {
                return;
            }
            if (_assetBundleCreateRequest != null)
            {
                AssetLogHelper.LogError("触发强制加载, 打断异步加载资源会导致所有异步加载的资源都立刻同步加载出来。资源名: " + FilePath + 
                                       "\nAssetBundle包名: " + AssetBundleName);
                AssetBundle = _assetBundleCreateRequest.assetBundle;
                return;
            }
            
            if (AssetComponent.BundleNameToRuntimeInfo[bundlePackageName].Encrypt)
            {
                string assetBundlePath = AssetComponent.BundleFileExistPath(bundlePackageName, AssetBundleName, AssetComponent.BundleNameToRuntimeInfo[bundlePackageName].Encrypt);
                byte[] data = VerifyHelper.GetDecryptData(assetBundlePath, AssetComponent.BundleNameToRuntimeInfo[bundlePackageName].SecretKey);
                AssetBundle = AssetBundle.LoadFromMemory(data);
            }
            else
            {
                string assetBundlePath = AssetComponent.BundleFileExistPath(bundlePackageName, AssetBundleName, false);
                AssetBundle = AssetBundle.LoadFromFile(assetBundlePath);
            }
            _loadState = LoadState.Finish;
            //判断是否还需要
            if (_refCount <= 0)
            {
                AssetComponent.AddPreUnLoadPool(this);
            }
        }
        
        /// <summary>
        /// 打开进度统计
        /// </summary>
        internal void OpenProgress()
        {
            _loadProgress = new WebLoadProgress();
        }
        
        internal float GetProgress()
        {
            if (_loadProgress == null)
            {
                AssetLogHelper.LogError("未打开进度统计无法获取进度");
                return 0;
            }
            if (_loadState == LoadState.Finish)
            {
                return 1;
            }
            if (_loadState == LoadState.NoLoad)
            {
                return 0;
            }

            if (_loadProgress.WeqOperation == null)
            {
                if (_assetBundleCreateRequest == null)
                {
                    return _loadProgress.GetWebProgress() / 2;
                }
                return _assetBundleCreateRequest.progress;
            }
            if (_assetBundleCreateRequest == null)
            {
                return _loadProgress.GetWebProgress() / 2;
            }
            return (_assetBundleCreateRequest.progress + 1.0f) / 2;
        }
        
    }

    /// <summary>
    /// AssetBundle加载的状态
    /// </summary>
    internal enum LoadState
    {
        NoLoad = 0,
        Loading = 1,
        Finish = 2
    }
}
