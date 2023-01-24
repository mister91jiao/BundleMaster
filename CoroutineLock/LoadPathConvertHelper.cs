using System.Collections.Generic;

namespace BM
{
    /// <summary>
    /// 负责将加载路径转化为唯一的long key
    /// </summary>
    public class LoadPathConvertHelper
    {
        /// <summary>
        /// 初始值
        /// </summary>
        private static long _originKey = 0;
        
        private static readonly Dictionary<string, long> LoadPathToKey = new Dictionary<string, long>();

        public static long LoadPathConvert(string loadPath)
        {
            if (LoadPathToKey.TryGetValue(loadPath, out long key))
            {
                return key;
            }
            _originKey++;
            key = _originKey;
            LoadPathToKey.Add(loadPath, key);
            return key;
        }
    }
}