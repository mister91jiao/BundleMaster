using System.Collections.Generic;

namespace BM
{
    /// <summary>
    /// 存放一个Bundle分包的初始化信息
    /// </summary>
    public class BundleRuntimeInfo
    {
        /// <summary>
        /// 主动加载的文件
        /// </summary>
        public readonly Dictionary<string, LoadFile> LoadFileDic = new Dictionary<string, LoadFile>();
        
        /// <summary>
        /// 依赖加载的文件
        /// </summary>
        public readonly Dictionary<string, LoadDepend> LoadDependDic = new Dictionary<string, LoadDepend>();
    }
}