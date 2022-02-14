using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;

namespace BM
{
    public class BuildAssets : EditorWindow
    {
        /// <summary>
        /// 分包配置文件资源目录
        /// </summary>
        private static string AssetLoadTablePath = "Assets/Editor/BundleMasterEditor/BuildSettings/AssetLoadTable.asset";
        
        [MenuItem("Tools/BuildAsset/创建分包总索引文件")]
        //[MenuItem("Assets/Create/BuildAsset/创建分包总索引文件")]
        public static void CreateBundleTableSetting()
        {
            AssetLoadTable assetLoadTable = ScriptableObject.CreateInstance<AssetLoadTable>();
            AssetDatabase.CreateAsset(assetLoadTable, AssetLoadTablePath);
        }
        
        [MenuItem("Tools/BuildAsset/创建分包配置文件")]
        //[MenuItem("Assets/Create/BuildAsset/创建分包配置文件")]
        public static void CreateSingleSetting()
        {
            AssetsLoadSetting assetsLoadSetting = ScriptableObject.CreateInstance<AssetsLoadSetting>();
            AssetDatabase.CreateAsset(assetsLoadSetting, "Assets/Editor/BundleMasterEditor/BuildSettings/AssetsLoadSetting.asset");
        }

        [MenuItem("Tools/BuildAsset/构建AssetBundle")]
        public static void BuildAllBundle()
        {
            AssetLoadTable assetLoadTable = AssetDatabase.LoadAssetAtPath<AssetLoadTable>(AssetLoadTablePath);
            List<AssetsLoadSetting> assetsLoadSettings = assetLoadTable.AssetsLoadSettings;
            foreach (AssetsLoadSetting assetsLoadSetting in assetsLoadSettings)
            {
                //获取单个Bundle的配置文件
                Build(assetLoadTable, assetsLoadSetting);
            }
            
            //构建完成后索引自动+1 需要自己取消注释
            foreach (AssetsLoadSetting assetsLoadSetting in assetLoadTable.AssetsLoadSettings)
            {
                assetsLoadSetting.BuildIndex++;
                EditorUtility.SetDirty(assetsLoadSetting);
            }
            AssetDatabase.SaveAssets();
            
            //打包结束
            AssetLogHelper.Log("打包结束");
        }
        
        [MenuItem("Tools/BuildAsset/Copy资源到StreamingAssets")]
        public static void CopyToStreamingAssets()
        {
            if (!Directory.Exists(Application.streamingAssetsPath))
            {
                Directory.CreateDirectory(Application.streamingAssetsPath);
            }
            DeleteHelper.DeleteDir(Application.streamingAssetsPath);
            AssetLoadTable assetLoadTable = AssetDatabase.LoadAssetAtPath<AssetLoadTable>(AssetLoadTablePath);
            DirectoryInfo buildBundlePath = new DirectoryInfo(assetLoadTable.BuildBundlePath);
            DirectoryInfo[] directoryInfos = buildBundlePath.GetDirectories();
            foreach (DirectoryInfo directoryInfo in directoryInfos)
            {
                string directoryPath = Path.Combine(Application.streamingAssetsPath, directoryInfo.Name);
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }
                DirectoryInfo subBundlePath = new DirectoryInfo(Path.Combine(assetLoadTable.BuildBundlePath, directoryInfo.Name));
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
            AssetLogHelper.Log("已将资源复制到StreamingAssets");
        }
        
        private static void Build(AssetLoadTable assetLoadTable, AssetsLoadSetting assetsLoadSetting)
        {
            Dictionary<string, LoadFile> loadFileDic = new Dictionary<string, LoadFile>();
            Dictionary<string, LoadDepend> loadDependDic = new Dictionary<string, LoadDepend>();
        
            //需要主动加载的文件的路径以及它的依赖bundle名字
            Dictionary<string, string[]> allLoadBaseAndDepends = new Dictionary<string, string[]>();
            //所有需要主动加载的资源的路径
            string[] paths = assetsLoadSetting.AssetPath.ToArray();
            //一个被依赖的文件依赖的次数(依赖也是包含后缀的路径)
            Dictionary<string, int> dependenciesIndex = new Dictionary<string, int>();
            //所有shader的集合，shader单独一个包
            HashSet<string> shaders = new HashSet<string>();
            //所有选定的主动加载的文件(包含后缀)
            HashSet<string> files = new HashSet<string>();
            for (int i = 0; i < paths.Length; i++)
            {
                string path = paths[i];
                //获取所有需要主动加载的资源
                BuildAssetsTools.GetChildFiles(path, files);
            }
            //添加打包进去的场景
            SceneAsset[] sceneAssets = assetsLoadSetting.Scene.ToArray();
            for (int i = 0; i < sceneAssets.Length; i++)
            {
                SceneAsset sceneAsset = sceneAssets[i];
                string scenePath = AssetDatabase.GetAssetPath(sceneAsset);
                files.Add(scenePath);
            }
            //分析所有需要主动加载的资源
            List<string> needRemoveFile = new List<string>();
            foreach (string file in files)
            {
                if (BuildAssetsTools.IsShaderAsset(file))
                {
                    if (!shaders.Contains(file))
                    {
                        shaders.Add(file);
                    }
                    if (!needRemoveFile.Contains(file))
                    {
                        needRemoveFile.Add(file);
                    }
                    continue;
                }
                //获取依赖
                string[] depends = AssetDatabase.GetDependencies(file);
                //创建主动加载文件的加载所需信息
                LoadFile loadFile = new LoadFile();
                loadFile.FilePath = file;
                loadFile.AssetBundleName = GetBundleName(assetsLoadSetting, file) + "." + assetsLoadSetting.BundleVariant;
                loadFileDic.Add(file, loadFile);
                //过滤出真正需要加载的依赖
                List<string> realDepends = new List<string>();
                //分析依赖情况
                for (int i = 0; i < depends.Length; i++)
                {
                    string depend = depends[i];
                    //脚本不能作为依赖
                    if (depend.EndsWith(".cs"))
                    {
                        continue;
                    }
                    //shader单独进包
                    if (BuildAssetsTools.IsShaderAsset(depend))
                    {
                        if (!shaders.Contains(depend))
                        {
                            shaders.Add(depend);
                        }
                        continue;
                    }
                    if (files.Contains(depend))
                    {
                        continue;
                    }
                    if (!depend.StartsWith("Assets/"))
                    {
                        continue;
                    }
                    realDepends.Add(depend);
                    if (dependenciesIndex.ContainsKey(depend))
                    {
                        dependenciesIndex[depend]++;
                    }
                    else
                    {
                        dependenciesIndex.Add(depend, 1);
                    }
                }
                allLoadBaseAndDepends.Add(file, realDepends.ToArray());
            }
            foreach (string removeFile in needRemoveFile)
            {
                files.Remove(removeFile);
            }
            //被复合依赖的文件
            HashSet<string> compoundDepends = new HashSet<string>();
            foreach (var dependFile in dependenciesIndex)
            {
                if (dependFile.Value > 1)
                {
                    compoundDepends.Add(dependFile.Key);
                }
            }
            //添加依赖信息
            foreach (var fileAndDepend in allLoadBaseAndDepends)
            {
                LoadFile loadFile = loadFileDic[fileAndDepend.Key];
                List<string> depends = new List<string>();
                foreach (string depend in fileAndDepend.Value)
                {
                    if (compoundDepends.Contains(depend))
                    {
                        //说明这个被依赖项是一个单独的bundle
                        depends.Add(depend);
                        //被依赖项也要创建Load类
                        if (!loadDependDic.ContainsKey(depend))
                        {
                            LoadDepend loadDepend = new LoadDepend();
                            loadDepend.FilePath = depend;
                            loadDepend.AssetBundleName = GetBundleName(assetsLoadSetting, depend) + "." + assetsLoadSetting.BundleVariant;
                            loadDependDic.Add(depend, loadDepend);
                        }
                    }
                }
                loadFile.DependFileName = depends.ToArray();
            }
            //创建需要的Bundle包
            List<AssetBundleBuild> allAssetBundleBuild = new List<AssetBundleBuild>();
            //首先创建Shader
            AssetBundleBuild shaderBundle = new AssetBundleBuild();
            shaderBundle.assetBundleName = "shader_" + assetsLoadSetting.BuildName;
            shaderBundle.assetNames = shaders.ToArray();
            allAssetBundleBuild.Add(shaderBundle);
            //添加文件以及依赖的bundle包
            AddToAssetBundleBuilds(assetsLoadSetting, allAssetBundleBuild, files);
            AddToAssetBundleBuilds(assetsLoadSetting, allAssetBundleBuild, compoundDepends);
            //保存打包Log
            SaveLoadLog(assetLoadTable, assetsLoadSetting, loadFileDic, loadDependDic);
            //开始打包
            string bundlePackagePath = Path.Combine(assetLoadTable.BuildBundlePath, assetsLoadSetting.BuildName);
            AssetBundleManifest manifest = BuildPipeline.BuildAssetBundles(bundlePackagePath, allAssetBundleBuild.ToArray(), 
                BuildAssetBundleOptions.UncompressedAssetBundle, EditorUserBuildSettings.activeBuildTarget);
            //保存版本号文件
            SaveBundleVersionFile(bundlePackagePath, manifest, assetsLoadSetting);
        }
        
        /// <summary>
        /// 创建AssetBundleBuild并添加管理
        /// </summary>
        private static void AddToAssetBundleBuilds(AssetsLoadSetting assetsLoadSetting, List<AssetBundleBuild> assetBundleBuild, HashSet<string> filePaths)
        {
            foreach (string filePath in filePaths)
            {
                AssetBundleBuild abb = new AssetBundleBuild();
                abb.assetBundleName = GetBundleName(assetsLoadSetting, filePath);
                abb.assetNames = new string[] { filePath };
                abb.assetBundleVariant = assetsLoadSetting.BundleVariant;
                assetBundleBuild.Add(abb);
            }
        }

        /// <summary>
        /// 保存加载用的Log
        /// </summary>
        private static void SaveLoadLog(AssetLoadTable assetLoadTable, AssetsLoadSetting assetsLoadSetting, Dictionary<string, LoadFile> loadFiles, Dictionary<string, LoadDepend> loadDepends)
        {
            
            if (!Directory.Exists(Path.Combine(assetLoadTable.BuildBundlePath, assetsLoadSetting.BuildName)))
            {
                Directory.CreateDirectory(Path.Combine(assetLoadTable.BuildBundlePath, assetsLoadSetting.BuildName));
            }
            using (StreamWriter sw = new StreamWriter(Path.Combine(assetLoadTable.BuildBundlePath, assetsLoadSetting.BuildName, "FileLogs.txt")))
            {
                StringBuilder sb = new StringBuilder();
                foreach (var loadFile in loadFiles)
                {
                    string data = "<" + loadFile.Key + "|" + loadFile.Value.AssetBundleName + "|";
                    foreach (string depend in loadFile.Value.DependFileName)
                    {
                        data += depend + "|";
                    }
                    data = data.Substring(0, data.Length - 1);
                    data += ">" + "\n";
                    sb.Append(data);
                }
                sw.WriteLine(sb.ToString());
            }
            using (StreamWriter sw = new StreamWriter(Path.Combine(assetLoadTable.BuildBundlePath, assetsLoadSetting.BuildName, "DependLogs.txt")))
            {
                StringBuilder sb = new StringBuilder();
                foreach (var loadDepend in loadDepends)
                {
                    string data = "<" + loadDepend.Key + "|" + loadDepend.Value.AssetBundleName + ">\n";
                    sb.Append(data);
                }
                sw.WriteLine(sb.ToString());
            }
        }

        /// <summary>
        /// 保存Bundle的版本号文件
        /// </summary>
        private static void SaveBundleVersionFile(string bundlePackagePath, AssetBundleManifest manifest, AssetsLoadSetting assetsLoadSetting)
        {
            string[] assetBundles = manifest.GetAllAssetBundles();
            using (StreamWriter sw = new StreamWriter(Path.Combine(bundlePackagePath, "VersionLogs.txt")))
            {
                StringBuilder sb = new StringBuilder();
                string versionHandler = System.DateTime.Now + "|" + assetsLoadSetting.BuildIndex + "\n";
                sb.Append(versionHandler);
                foreach (string assetBundle in assetBundles)
                {
                    string bundlePath = Path.Combine(bundlePackagePath, assetBundle);
                    uint crc32 = VerifyHelper.GetFileCRC32(bundlePath);
                    string info = assetBundle + "|" + VerifyHelper.GetFileLength(bundlePath) + "|" + crc32 + "\n";
                    sb.Append(info);
                }
                sw.WriteLine(sb.ToString());
            }
        }
    
        private static string GetBundleName(AssetsLoadSetting assetsLoadSetting, string filePath)
        {
            if (assetsLoadSetting.NameByHash)
            {
                filePath = VerifyHelper.GetMd5Hash(filePath);
            }
            else
            {
                filePath = filePath.Replace('/', '_');
                filePath = filePath.Replace('.', '_');
            }
            return filePath;
        }
        
    }
}


