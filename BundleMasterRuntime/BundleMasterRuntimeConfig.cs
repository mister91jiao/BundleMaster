using UnityEngine;

namespace BM
{
    public class BundleMasterRuntimeConfig : ScriptableObject
    {
        /// <summary>
        /// 加载模式
        /// </summary>
        public AssetLoadMode AssetLoadMode;
        
        /// <summary>
        /// 最大同时下载的资源数量
        /// </summary>
        public int MaxDownLoadCount;

        /// <summary>
        /// 下载失败最多重试次数
        /// </summary>
        public int ReDownLoadCount;
    }
}