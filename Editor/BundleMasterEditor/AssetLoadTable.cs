using System.Collections.Generic;
using UnityEngine;

namespace BM
{
    /// <summary>
    /// 用于配置所有分包的构建信息
    /// </summary>
    public class AssetLoadTable : ScriptableObject
    {
        [Header("构建路径")]
        [Tooltip("构建的资源的所在路径")] public string BuildBundlePath;
        
        [Header("所有分包配置信息")]
        [Tooltip("每一个分包的配置信息")]
        public List<AssetsLoadSetting> AssetsLoadSettings = new List<AssetsLoadSetting>();
    }
}