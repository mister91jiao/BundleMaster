using System.Collections.Generic;
using ET;

namespace BM
{
    public static partial class AssetComponent
    {
#if BMWebGL
        /// <summary>
        /// 检查分包是否需要更新
        /// </summary>
        /// <param name="bundlePackageNames">所有分包的名称以及是否验证文件CRC</param>
#pragma warning disable CS1998 // 异步方法缺少 "await" 运算符，将以同步方式运行
        public static async ETTask<UpdateBundleDataInfo> CheckAllBundlePackageUpdate(Dictionary<string, bool> bundlePackageNames)
#pragma warning restore CS1998 // 异步方法缺少 "await" 运算符，将以同步方式运行
        {
            UpdateBundleDataInfo updateBundleDataInfo = new UpdateBundleDataInfo();
            updateBundleDataInfo.NeedUpdate = false;
            
            if (AssetComponentConfig.AssetLoadMode != AssetLoadMode.Build)
            {
                if (AssetComponentConfig.AssetLoadMode == AssetLoadMode.Local)
                {
                    AssetComponentConfig.HotfixPath = AssetComponentConfig.LocalBundlePath;
                }
                else
                {
#if !UNITY_EDITOR
                    AssetLogHelper.LogError("AssetLoadMode = AssetLoadMode.Develop 只能在编辑器下运行");
#endif
                }
            }
            else
            {
                AssetLogHelper.LogError("AssetLoadMode = AssetLoadMode.Build WebGL无需更新，请用Local模式");
            }
            return updateBundleDataInfo;
        }

        /// <summary>
        /// 下载更新
        /// </summary>
#pragma warning disable CS1998 // 异步方法缺少 "await" 运算符，将以同步方式运行
        public static async ETTask DownLoadUpdate(UpdateBundleDataInfo updateBundleDataInfo)
#pragma warning restore CS1998 // 异步方法缺少 "await" 运算符，将以同步方式运行
        {
            AssetLogHelper.LogError("WebGL无需更新");
        }

#endif
    }
}
