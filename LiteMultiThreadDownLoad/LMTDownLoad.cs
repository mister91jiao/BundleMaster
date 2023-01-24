using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace LMTD
{
    public class LMTDownLoad : ILiteThreadAction, IDisposable
    {
        private static readonly Queue<LMTDownLoad> LmtDownLoadQueue = new Queue<LMTDownLoad>();
        
        private Action<LmtDownloadInfo> _completeCallback;
        
        /// <summary>
        /// 下载完成后的回调
        /// </summary>
        public event Action<LmtDownloadInfo> Completed
        {
            add => _completeCallback += value;
            remove => this._completeCallback -= value;
        }
        
        private Action _upDateInfoCallback;
        
        /// <summary>
        /// 下载更新循环
        /// </summary>
        public event Action UpDateInfo
        {
            add => _upDateInfoCallback += value;
            remove => this._upDateInfoCallback -= value;
        }

        /// <summary>
        /// 创建一个下载器
        /// </summary>
        public static LMTDownLoad Create(string url, string filePath)
        {
            LMTDownLoad lmtDownLoad;
            lock (LmtDownLoadQueue)
            {
                if (LmtDownLoadQueue.Count > 0)
                {
                    lmtDownLoad = LmtDownLoadQueue.Dequeue();
                    lmtDownLoad.url = url;
                    lmtDownLoad.filePath = filePath;
                }
                else
                {
                    lmtDownLoad = new LMTDownLoad(url, filePath);
                }
            }
            lmtDownLoad.CancelLock = false;
            return lmtDownLoad;
        }
        
        private LMTDownLoad(string url, string filePath)
        {
            this.url = url;
            this.filePath = filePath;
        }

        public bool CancelLock = false;

        /// <summary>
        /// 下载地址
        /// </summary>
        private string url = null;
        
        /// <summary>
        /// 文件存储路径
        /// </summary>
        private string filePath = null;

        /// <summary>
        /// 下载信息
        /// </summary>
        public LmtDownloadInfo LmtDownloadInfo;
        
        /// <summary>
        /// 返回下载文件的信息
        /// </summary>
        public LmtDownloadInfo DownLoad()
        {
            //需要返回的数据
            LmtDownloadInfo.DownLoadFileCRC = 0xFFFFFFFF;
            LmtDownloadInfo.downLoadSizeValue = 0;
            //创建下载请求
            HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
            httpWebRequest.Proxy = LMTDProxy.GetProxy();
            HttpWebResponse httpWebResponse;
            try
            {
                httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            }
            catch
            {
#if UNITY_5_3_OR_NEWER
                UnityEngine.Debug.LogError("下载资源失败\n" + url + "\n");
#else
                Console.WriteLine("下载失败资源失败\n" + url + "\n");
#endif
                LmtDownloadInfo.LmtDownloadResult = LmtDownloadResult.ResponseFail;
                return LmtDownloadInfo;
            }
            //创建一块存储的大小 1mb
            byte[] blockBytes = new byte[1048576];
            using FileStream fileStream = new FileStream(filePath, FileMode.Create);
            try
            {
                //获取文件流
                using Stream receiveStream = httpWebResponse.GetResponseStream();
                // ReSharper disable once PossibleNullReferenceException
                int blockSize = receiveStream.Read(blockBytes, 0, blockBytes.Length);
                while (blockSize > 0)
                {
                    if (CancelLock)
                    {
                        LmtDownloadInfo.LmtDownloadResult = LmtDownloadResult.CancelDownLoad;
                        return LmtDownloadInfo;
                    }
                    //计算CRC
                    for (uint i = 0; i < blockSize; i++)
                    {
                        LmtDownloadInfo.DownLoadFileCRC = (LmtDownloadInfo.DownLoadFileCRC << 8) ^ LmtdTable.CRCTable[(LmtDownloadInfo.DownLoadFileCRC >> 24) ^ blockBytes[i]];
                    }
                    LmtDownloadInfo.downLoadSizeValue += blockSize;
                    _upDateInfoCallback?.Invoke();
                    //循环写入读取数据
                    fileStream.Write(blockBytes, 0, blockSize);
                    blockSize = receiveStream.Read(blockBytes, 0, blockBytes.Length);
                }
                receiveStream.Close();
            }
            catch
            {
#if UNITY_5_3_OR_NEWER
                UnityEngine.Debug.LogError("下载资源中断\n" + url + "\n");
#else
                Console.WriteLine("下载资源中断\n" + url + "\n");
#endif
                LmtDownloadInfo.LmtDownloadResult = LmtDownloadResult.DownLoadFail;
                return LmtDownloadInfo;
            }
            finally
            {
                fileStream.Close();
                httpWebResponse.Close();
                httpWebResponse.Dispose();
            }
            LmtDownloadInfo.LmtDownloadResult = LmtDownloadResult.Success;
            return LmtDownloadInfo;
        }
        
        public void Logic()
        {
            LmtDownloadInfo lmtDownloadInfo = DownLoad();
            _completeCallback?.Invoke(lmtDownloadInfo);
        }

        public void Dispose()
        {
            CancelLock = false;
            url = null;
            filePath = null;
            _completeCallback = null;
            _upDateInfoCallback = null;
            lock (LmtDownLoadQueue)
            {
                LmtDownLoadQueue.Enqueue(this);
            }
        }
        
    }
    
    /// <summary>
    /// 下载完成后的回调信息
    /// </summary>
    public struct LmtDownloadInfo
    {
        public LmtDownloadResult LmtDownloadResult; 
        public uint DownLoadFileCRC;

        internal long downLoadSizeValue;
        /// <summary>
        /// 已经下载了多少
        /// </summary>
        public long DownLoadSize
        {
            get { return downLoadSizeValue; }
        }
        
    }
    
    public enum LmtDownloadResult
    {
        /// <summary>
        /// 成功
        /// </summary>
        Success = 0,
        /// <summary>
        /// 请求连接失败
        /// </summary>
        ResponseFail = 1,
        /// <summary>
        /// 下载失败
        /// </summary>
        DownLoadFail = 2,
        /// <summary>
        /// 取消下载
        /// </summary>
        CancelDownLoad = 3
    }
}