using System.Collections.Generic;
using UnityEngine;
using ET;

namespace BM
{
    public partial class LoadBase
    {
#if Nintendo_Switch
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
            string assetBundlePath = AssetComponent.BundleFileExistPath_Nintendo(bundlePackageName, AssetBundleName);
            AssetBundle = AssetBundle.LoadFromFile(assetBundlePath);
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
            string assetBundlePath = AssetComponent.BundleFileExistPath_Nintendo(bundlePackageName, AssetBundleName);
            //获取一个协程锁
            CoroutineLock coroutineLock = await CoroutineLockComponent.Wait(CoroutineLockType.BundleMaster, LoadPathConvertHelper.LoadPathConvert(assetBundlePath));
            if (_loadState == LoadState.NoLoad)
            {
                _loadState = LoadState.Loading;
                await LoadBundleFinish(assetBundlePath);
                _loadState = LoadState.Finish;
            }
            //协程锁解锁
            coroutineLock.Dispose();
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
            string assetBundlePath = AssetComponent.BundleFileExistPath_Nintendo(bundlePackageName, AssetBundleName);
            AssetBundle = AssetBundle.LoadFromFile(assetBundlePath);
            _loadState = LoadState.Finish;
            //判断是否还需要
            if (_refCount <= 0)
            {
                AssetComponent.AddPreUnLoadPool(this);
            }
        }
        
        /// <summary>
        /// 打开进度统计 Switch模式下仅做占位使用
        /// </summary>
        internal void OpenProgress(){}
        
        /// <summary>
        /// 获取当前资源加载进度
        /// </summary>
        internal float GetProgress()
        {
            if (_loadState == LoadState.Finish)
            {
                return 1;
            }
            if (_loadState == LoadState.NoLoad)
            {
                return 0;
            }
            if (_assetBundleCreateRequest == null)
            {
                AssetLogHelper.LogError("资源加载中但加载请求为Null");
                return 0;
            }
            return _assetBundleCreateRequest.progress;;
        }
        
#endif
        
    }
}
