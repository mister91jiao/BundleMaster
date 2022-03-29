using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BM
{
    /// <summary>
    /// 用于配置原生资源的更新
    /// </summary>
    public class AssetsOriginSetting : AssetsSetting
    {
        [Header("原生资源分包名字")]
        [Tooltip("原生资源的分包名(建议英文)")] public string BuildName;
        
        [Header("版本索引")]
        [Tooltip("表示当前原生的索引")] public int BuildIndex;
        
        [Header("资源路径")]
        [Tooltip("需要打包的资源所在的路径(不需要包含依赖, 只包括需要主动加载的资源)")]
        public string OriginFilePath = "";
    }
}