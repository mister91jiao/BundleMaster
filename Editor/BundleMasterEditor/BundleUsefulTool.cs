using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BM
{
    /// <summary>
    /// 一些打包会用到的实用的功能
    /// </summary>
    public class BundleUsefulTool : EditorWindow
    {
        [MenuItem("Tools/BuildAsset/Copy资源到StreamingAssets")]
        public static void CopyToStreamingAssets()
        {
            if (!Directory.Exists(Application.streamingAssetsPath))
            {
                Directory.CreateDirectory(Application.streamingAssetsPath);
            }
            DeleteHelper.DeleteDir(Application.streamingAssetsPath);
            AssetLoadTable assetLoadTable = AssetDatabase.LoadAssetAtPath<AssetLoadTable>(BundleMasterWindow.AssetLoadTablePath);
            foreach (AssetsSetting assetsSetting in assetLoadTable.AssetsSettings)
            {
                if (!(assetsSetting is AssetsLoadSetting assetsLoadSetting))
                {
                    continue;
                }
                string assetPathFolder;
                if (assetsLoadSetting.EncryptAssets)
                {
                    assetPathFolder = Path.Combine(assetLoadTable.BuildBundlePath + "/../", assetLoadTable.EncryptPathFolder, assetsLoadSetting.BuildName);
                }
                else
                {
                    assetPathFolder = Path.Combine(assetLoadTable.BuildBundlePath, assetsLoadSetting.BuildName);
                }
                string directoryPath = Path.Combine(Application.streamingAssetsPath, assetsLoadSetting.BuildName);
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }
                DirectoryInfo subBundlePath = new DirectoryInfo(assetPathFolder);
                FileInfo[] fileInfos = subBundlePath.GetFiles();
                foreach (FileInfo fileInfo in fileInfos)
                {
                    if (fileInfo.DirectoryName == null)
                    {
                        AssetLogHelper.LogError("找不到文件的路径: " + fileInfo.Name);
                        continue;
                    }
                    string filePath = Path.Combine(fileInfo.DirectoryName, fileInfo.Name);
                    string suffix = Path.GetExtension(filePath);
                    if ((!fileInfo.Name.StartsWith("shader_") && string.IsNullOrWhiteSpace(suffix)) || suffix == ".manifest")
                    {
                        continue;
                    }
                    File.Copy(filePath, Path.Combine(directoryPath, fileInfo.Name));
                }
            }
            foreach (AssetsSetting assetsSetting in assetLoadTable.AssetsSettings)
            {
                if (!(assetsSetting is AssetsOriginSetting assetsOriginSetting))
                {
                    continue;
                }
                string assetPathFolder = Path.Combine(assetLoadTable.BuildBundlePath, assetsOriginSetting.BuildName);
                string directoryPath = Path.Combine(Application.streamingAssetsPath, assetsOriginSetting.BuildName);
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }
                //获取所有资源目录
                HashSet<string> files = new HashSet<string>();
                HashSet<string> dirs = new HashSet<string>();
                BuildAssetsTools.GetOriginsPath(assetPathFolder, files, dirs);
                //Copy资源
                foreach (string dir in dirs)
                {
                    Directory.CreateDirectory(dir.Replace(assetPathFolder, directoryPath));
                }
                foreach (string file in files)
                {
                    File.Copy(file, file.Replace(assetPathFolder, directoryPath), true);
                }
            }
            AssetDatabase.Refresh();
            AssetLogHelper.Log("已将资源复制到StreamingAssets");
        }
        
        [MenuItem("Tools/BuildAsset/实用工具/清空热更目录下的文件")]
        public static void ClearHotfixPathPath()
        {
            DeleteHelper.DeleteDir(AssetComponentConfig.HotfixPath);
            AssetDatabase.Refresh();
        }
        
        [MenuItem("Tools/BuildAsset/实用工具/清空本地目录下的文件")]
        public static void ClearLocalBundlePath()
        {
            DeleteHelper.DeleteDir(AssetComponentConfig.LocalBundlePath);
            AssetDatabase.Refresh();
        }
        
        [MenuItem("Tools/BuildAsset/实用工具/清空打包目录下的文件")]
        public static void ClearLocalBuildPath()
        {
            AssetLoadTable assetLoadTable = AssetDatabase.LoadAssetAtPath<AssetLoadTable>(BundleMasterWindow.AssetLoadTablePath);
            DeleteHelper.DeleteDir(assetLoadTable.BundlePath);
        }
        
        [MenuItem("Tools/BuildAsset/实用工具/清空加密目录下的文件")]
        public static void ClearLocalEncryptPath()
        {
            AssetLoadTable assetLoadTable = AssetDatabase.LoadAssetAtPath<AssetLoadTable>(BundleMasterWindow.AssetLoadTablePath);
            DeleteHelper.DeleteDir(assetLoadTable.EncryptPathFolder);
        }
    }
    
    
}