using System.IO;
using UnityEditor;
using UnityEngine;

namespace BM
{
    public static partial class AssetComponent
    {
        /// <summary>
        /// 判定一个资源是否存在，如果这个资源是组里的资源，那么只能检测到这个资源所在的组是否存在
        /// </summary>
        public static bool CheckAssetExist(string assetPath, string bundlePackageName = null)
        {
            if (bundlePackageName == null)
            {
                bundlePackageName = AssetComponentConfig.DefaultBundlePackageName;
            }
            if (AssetComponentConfig.AssetLoadMode == AssetLoadMode.Develop)
            {
#if UNITY_EDITOR
                assetPath = Path.Combine(Application.dataPath + "/../", assetPath);
                return File.Exists(assetPath);
#else
                AssetLogHelper.LogError("检查资源: " + assetPath + " 失败(资源检查Develop模式只能在编辑器下运行)");
                return false;
#endif
            }
            if (!BundleNameToRuntimeInfo.TryGetValue(bundlePackageName, out BundleRuntimeInfo bundleRuntimeInfo))
            {
                AssetLogHelper.LogError(bundlePackageName + "检查资源时分包没有初始化");
                return false;
            }
            string groupPath = GroupAssetHelper.IsGroupAsset(assetPath, bundleRuntimeInfo.LoadGroupDicKey);
            if (groupPath != null)
            {
                return true;
            }
            if (bundleRuntimeInfo.LoadFileDic.ContainsKey(assetPath))
            {
                return true;
            }
            return false;
        }
    }
}