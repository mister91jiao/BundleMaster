namespace BM
{
    public class BundleMasterAssetsPath
    {
        /// <summary>
        /// 运行时配置文件的路径
        /// </summary>
        public static string RuntimeConfigPath = "Assets/Resources/BMConfig.asset";
        
        /// <summary>
        /// 分包文件资源索引配置
        /// </summary>
        public static string AssetLoadTablePath = "Assets/Editor/BundleMasterEditor/BuildSettings/AssetLoadTable.asset";

        /// <summary>
        /// 分包配置信息
        /// </summary>
        public static string AssetsLoadSettingPath = "Assets/Editor/BundleMasterEditor/BuildSettings/AssetsLoadSetting";
        
        /// <summary>
        /// 原生资源包配置信息
        /// </summary>
        public static string AssetsOriginSettingPath = "Assets/Editor/BundleMasterEditor/BuildSettings/AssetsOriginSetting";
    }
}