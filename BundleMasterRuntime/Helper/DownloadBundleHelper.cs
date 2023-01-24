using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.Networking;
using ET;
using LMTD;

namespace BM
{
    public static class DownloadBundleHelper
    {
        public static async ETTask<byte[]> DownloadDataByUrl(string url)
        {
            for (int i = 0; i < AssetComponentConfig.ReDownLoadCount; i++)
            {
                byte[] data = await DownloadData(url);
                if (data != null)
                {
                    return data;
                }
            }
            AssetLogHelper.LogError("下载资源失败: " + url);
            return null;
        }
        
        private static async ETTask<byte[]> DownloadData(string url)
        {
            using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
            {
                UnityWebRequestAsyncOperation webRequestAsync = webRequest.SendWebRequest();
                ETTask waitDown = ETTask.Create(true);
                webRequestAsync.completed += (asyncOperation) =>
                {
                    waitDown.SetResult();
                };
                await waitDown;
#if UNITY_2020_1_OR_NEWER
                if (webRequest.result != UnityWebRequest.Result.Success)
#else
                if (!string.IsNullOrEmpty(webRequest.error))
#endif
                {
                    AssetLogHelper.Log("下载Bundle失败 重试\n" + webRequest.error + "\nURL：" + url);
                    return null;
                }
                return webRequest.downloadHandler.data;
            }
        }
        
        public static async ETTask<DownLoadData> DownloadRefDataByUrl(string url)
        {
            DownLoadData downLoadData = DownLoadData.Get();
            for (int i = 0; i < AssetComponentConfig.ReDownLoadCount; i++)
            {
                await DownloadData(url, downLoadData);
                if (downLoadData.Data != null)
                {
                    return downLoadData;
                }
            }
            AssetLogHelper.LogError("下载资源失败: " + url);
            return null;
        }

        private static async ETTask DownloadData(string url, DownLoadData downLoadData)
        {
            using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
            {
                UnityWebRequestAsyncOperation webRequestAsync = webRequest.SendWebRequest();
                ETTask waitDown = ETTask.Create(true);
                webRequestAsync.completed += (asyncOperation) =>
                {
                    waitDown.SetResult();
                };
                await waitDown;
#if UNITY_2020_1_OR_NEWER
                if (webRequest.result != UnityWebRequest.Result.Success)
#else
                if (!string.IsNullOrEmpty(webRequest.error))
#endif
                {
                    AssetLogHelper.Log("下载Bundle失败 重试\n" + webRequest.error + "\nURL：" + url);
                    return;
                }
                downLoadData.Data = webRequest.downloadHandler.data;
                webRequest.downloadHandler.Dispose();
            }
        }

        /// <summary>
        /// 下载完成回调位
        /// </summary>
        internal static readonly Queue<ETTask> DownLoadFinishQueue = new Queue<ETTask>();

        /// <summary>
        /// 多线程下载资源直存
        /// </summary>
        public static async ETTask<LmtDownloadInfo> DownloadData(string url, string filePath, UpdateBundleDataInfo updateBundleDataInfo)
        {
            LmtDownloadInfo lmtDownloadInfo = new LmtDownloadInfo();
            for (int i = 0; i < AssetComponentConfig.ReDownLoadCount; i++)
            {
                //说明下载更新已经被取消
                if (updateBundleDataInfo.Cancel)
                {
                    return lmtDownloadInfo;
                }
                
                lmtDownloadInfo = await DownloadOneData(url, filePath, updateBundleDataInfo);
                if (lmtDownloadInfo.LmtDownloadResult == LmtDownloadResult.Success)
                {
                    return lmtDownloadInfo;
                }
            }
            //多次下载失败直接取消下载
            updateBundleDataInfo.CancelUpdate();
            return lmtDownloadInfo;
        }

        private static async ETTask<LmtDownloadInfo> DownloadOneData(string url, string filePath, UpdateBundleDataInfo updateBundleDataInfo)
        {
            ETTask tcs = ETTask.Create(true);
            LMTDownLoad lmtDownLoad = LMTDownLoad.Create(url, filePath);
            long lastDownSize = 0;
            lmtDownLoad.UpDateInfo += () =>
            {
                // ReSharper disable AccessToDisposedClosure
                updateBundleDataInfo.FinishUpdateSize += lmtDownLoad.LmtDownloadInfo.DownLoadSize - lastDownSize;
                lastDownSize = lmtDownLoad.LmtDownloadInfo.DownLoadSize;
                // ReSharper restore AccessToDisposedClosure
                lock (updateBundleDataInfo)
                {
                    if (updateBundleDataInfo.Cancel)
                    { 
                        // ReSharper disable once AccessToDisposedClosure
                        lmtDownLoad.CancelLock = true;
                    }
                }
            };
            lmtDownLoad.Completed += (info) =>
            {
                lock (DownLoadFinishQueue)
                {
                    DownLoadFinishQueue.Enqueue(tcs);
                }
            };
            ThreadFactory.ThreadAction(lmtDownLoad);
            await tcs;
            LmtDownloadInfo lmtDownloadInfo = lmtDownLoad.LmtDownloadInfo;
            if (lmtDownloadInfo.LmtDownloadResult != LmtDownloadResult.Success)
            {
                updateBundleDataInfo.FinishUpdateSize -= lmtDownLoad.LmtDownloadInfo.DownLoadSize;
                //主动取消下载就没必要再输出log了
                if (lmtDownloadInfo.LmtDownloadResult != LmtDownloadResult.CancelDownLoad)
                {
                    AssetLogHelper.LogError("下载资源失败: " + lmtDownloadInfo.LmtDownloadResult);
                }
            }
            lmtDownLoad.Dispose();
            return lmtDownloadInfo;
        }
        
        /// <summary>
        /// 另一种多线程下载的方式，不建议使用
        /// </summary>
        public static async Task<LmtDownloadInfo> DownloadDataTask(string url, string filePath)
        {
            LMTDownLoad lmtDownLoad = LMTDownLoad.Create(url, filePath);
            Task<LmtDownloadInfo> tcs = new Task<LmtDownloadInfo>(lmtDownLoad.DownLoad);
            tcs.Start();
            await tcs;
            lmtDownLoad.Dispose();
            return lmtDownLoad.LmtDownloadInfo;
        }
        
    }

    public class DownLoadData
    {
        public byte[] Data;

        private static readonly Queue<DownLoadData> DownLoadDataPool = new Queue<DownLoadData>();

        public static DownLoadData Get()
        {
            if (DownLoadDataPool.Count > 0)
            {
                return DownLoadDataPool.Dequeue();
            }
            else
            {
                return new DownLoadData();
            }
        }

        public static void Recovery(DownLoadData downLoadData)
        {
            downLoadData.Data = null;
            DownLoadDataPool.Enqueue(downLoadData);
        }

        public static void ClearPool()
        {
            DownLoadDataPool.Clear();
        }
    }
    
}