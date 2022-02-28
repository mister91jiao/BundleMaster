using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Directory = UnityEngine.Windows.Directory;

namespace BM
{
    public static class BuildAssetsTools
    {
        /// <summary>
        /// 获取一个目录下所有的子文件
        /// </summary>
        public static void GetChildFiles(string basePath, HashSet<string> files)
        {
            DirectoryInfo basefolder = new DirectoryInfo(basePath);
            FileInfo[] basefil = basefolder.GetFiles();
            for (int i = 0; i < basefil.Length; i++)
            {
                    
                if (CantLoadType(basefil[i].FullName))
                {
                    files.Add(basePath + "/" + basefil[i].Name);
                }
            }
            Er(basePath);
            void Er(string subPath)
            {
                string[] subfolders = AssetDatabase.GetSubFolders(subPath);
                for (int i = 0; i < subfolders.Length; i++)
                {
                    DirectoryInfo subfolder = new DirectoryInfo(subfolders[i]);
                    FileInfo[] fil = subfolder.GetFiles();
                    for (int j = 0; j < fil.Length; j++)
                    {
                    
                        if (CantLoadType(fil[j].FullName))
                        {
                            files.Add(subfolders[i] + "/" + fil[j].Name);
                        }
                    }
                    Er(subfolders[i]);
                }
            }
        }
        
        /// <summary>
        /// 创建加密的AssetBundle
        /// </summary>
        public static void CreateEncryptAssets(string bundlePackagePath, string encryptAssetPath, AssetBundleManifest manifest, string secretKey)
        {
            string[] assetBundles = manifest.GetAllAssetBundles();
            foreach (string assetBundle in assetBundles)
            {
                string bundlePath = Path.Combine(bundlePackagePath, assetBundle);
                if (!Directory.Exists(encryptAssetPath))
                {
                    Directory.CreateDirectory(encryptAssetPath);
                }
                using (FileStream fs = new FileStream(Path.Combine(encryptAssetPath, assetBundle), FileMode.OpenOrCreate))
                {
                    byte[] encryptBytes = VerifyHelper.CreateEncryptData(bundlePath, secretKey);
                    fs.Write(encryptBytes, 0, encryptBytes.Length);
                }
            }
        }
        
        /// <summary>
        /// 需要忽略加载的格式
        /// </summary>
        public static bool CantLoadType(string fileFullName)
        {
            string suffix = Path.GetExtension(fileFullName);
            switch (suffix)
            {
                case ".dll":
                    return false;
                case ".cs":
                    return false;
                case ".meta":
                    return false;
                case ".js":
                    return false;
                case ".boo":
                    return false;
            }
            return true;
        }
        
        /// <summary>
        /// 是Shader资源
        /// </summary>
        public static bool IsShaderAsset(string fileFullName)
        {
            string suffix = Path.GetExtension(fileFullName);
            switch (suffix)
            {
                case ".shader":
                    return true;
                case ".shadervariants":
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 是否生成路径字段代码脚本
        /// </summary>
        public static void GeneratePathCode(HashSet<string> allAssetPaths, string scriptFilePaths)
        {
            using (StreamWriter sw = new StreamWriter(Path.Combine(Application.dataPath, scriptFilePaths, "BPath.cs")))
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("// ReSharper disable All\n");
                sb.Append("namespace BM\n");
                sb.Append("{\n");
                sb.Append("\tpublic class BPath\n");
                sb.Append("\t{\n");
                foreach (string assetPath in allAssetPaths)
                {
                    string name = assetPath.Replace("/", "_");
                    name = name.Replace(".", "__");
                    sb.Append("\t\tpublic const string " + name + " = \"" + assetPath + "\";\n");
                }
                sb.Append("\t}\n");
                sb.Append("}");
                sw.WriteLine(sb.ToString());
            }
        }
    }
}