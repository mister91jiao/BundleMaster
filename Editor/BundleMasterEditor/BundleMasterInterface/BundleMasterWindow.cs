using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BM
{
    [SuppressMessage("ReSharper", "PossibleLossOfFraction")]
    public class BundleMasterWindow : EditorWindow
    {
        private static BundleMasterWindow _instance = null;

        /// <summary>
        /// 运行时配置文件
        /// </summary>
        private static BundleMasterRuntimeConfig _bundleMasterRuntimeConfig = null;

        private static bool _runtimeConfigLoad = false;
        
        private static int _w = 960;
        private static int _h = 540;
        
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
        
        private static AssetLoadTable _assetLoadTable = null;

        /// <summary>
        /// 选中查看的分包信息
        /// </summary>
        private static AssetsLoadSetting _selectAssetsLoadSetting = null;
        
        /// <summary>
        /// 是否查看子页面
        /// </summary>
        private static bool _viewSub = false;
        
        [MenuItem("Tools/BuildAsset/打开配置界面")]
        public static void Init()
        {
            Open(true);
        }
        
        Vector2 scrollScenePos = Vector2.zero;
        Vector2 scrollPos = Vector2.zero;
        Vector2 scrollBundleScenePos = Vector2.zero;
        Vector2 scrollPathPos = Vector2.zero;
        
        private static void Open(bool focus)
        {
            if (_instance != null)
            {
                return;
            }
            _viewSub = false;
            _instance = (BundleMasterWindow)EditorWindow.GetWindow(typeof(BundleMasterWindow), true, "BundleMasterEditor", focus);
            //_instance.position = new Rect(_w / 2, _h / 2, _w, _h);
            _instance.maxSize = new Vector2(_w, _h);
            _instance.minSize = new Vector2(_w, _h);
            //加载配置文件
            _bundleMasterRuntimeConfig = AssetDatabase.LoadAssetAtPath<BundleMasterRuntimeConfig>(RuntimeConfigPath);
            _runtimeConfigLoad = false;
            if (_bundleMasterRuntimeConfig != null)
            {
                _runtimeConfigLoad = true;
            }
        }
        public void OnGUI()
        {
            Open(false);
            if (!_runtimeConfigLoad)
            {
                GUILayout.BeginArea(new Rect(_w / 4, _h / 8, _w / 2, _h / 4));
                if (GUILayout.Button("创建运行时配置文件", GUILayout.Width(_w / 2), GUILayout.Height(_h / 4), GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false)))
                {
                    _bundleMasterRuntimeConfig = ScriptableObject.CreateInstance<BundleMasterRuntimeConfig>();
                    _bundleMasterRuntimeConfig.AssetLoadMode = AssetLoadMode.Develop;
                    _bundleMasterRuntimeConfig.BundleServerUrl = "";
                    _bundleMasterRuntimeConfig.MaxDownLoadCount = 8;
                    _bundleMasterRuntimeConfig.ReDownLoadCount = 3;
                    if (!Directory.Exists(Path.Combine(Application.dataPath, "Resources")))
                    {
                        Directory.CreateDirectory(Path.Combine(Application.dataPath, "Resources"));
                    }
                    AssetDatabase.CreateAsset(_bundleMasterRuntimeConfig, RuntimeConfigPath);
                    AssetDatabase.Refresh();
                    _runtimeConfigLoad = true;
                }
                GUILayout.EndArea();
                return;
            }

            bool needFlush = false;
            
            GUILayout.BeginHorizontal();
            GUILayout.Label("当前资源加载模式: \t" + _bundleMasterRuntimeConfig.AssetLoadMode, GUILayout.Width(_w / 4), GUILayout.Height(_h / 8), GUILayout.ExpandWidth(false));
            if (GUILayout.Button("开发模式", GUILayout.Width(_w / 6), GUILayout.Height(_h / 8), GUILayout.ExpandWidth(true)))
            {
                _bundleMasterRuntimeConfig.AssetLoadMode = AssetLoadMode.Develop;
                DevelopSceneChange.CheckSceneChange(_bundleMasterRuntimeConfig.AssetLoadMode);
                needFlush = true;
            }
            if (GUILayout.Button("本地模式", GUILayout.Width(_w / 6), GUILayout.Height(_h / 8), GUILayout.ExpandWidth(true)))
            {
                _bundleMasterRuntimeConfig.AssetLoadMode = AssetLoadMode.Local;
                DevelopSceneChange.CheckSceneChange(_bundleMasterRuntimeConfig.AssetLoadMode);
                needFlush = true;
            }
            if (GUILayout.Button("构建模式", GUILayout.Width(_w / 6), GUILayout.Height(_h / 8), GUILayout.ExpandWidth(true)))
            {
                _bundleMasterRuntimeConfig.AssetLoadMode = AssetLoadMode.Build;
                DevelopSceneChange.CheckSceneChange(_bundleMasterRuntimeConfig.AssetLoadMode);
                needFlush = true;
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal(GUILayout.ExpandHeight(false));
            GUILayout.Label("资源服务器地址: ", GUILayout.Width(_w / 10), GUILayout.ExpandWidth(false));
            string bundleServerUrl = _bundleMasterRuntimeConfig.BundleServerUrl;
            bundleServerUrl = EditorGUILayout.TextField(bundleServerUrl, GUILayout.MinWidth(_w / 5), GUILayout.ExpandWidth(true));
            if (!string.Equals(_bundleMasterRuntimeConfig.BundleServerUrl, bundleServerUrl, StringComparison.Ordinal))
            {
                _bundleMasterRuntimeConfig.BundleServerUrl = bundleServerUrl;
                needFlush = true;
            }
            GUILayout.Label("最大同时下载资源数: ", GUILayout.Width(_w / 8), GUILayout.ExpandWidth(false));
            int maxDownLoadCount = _bundleMasterRuntimeConfig.MaxDownLoadCount;
            maxDownLoadCount = EditorGUILayout.IntField(maxDownLoadCount, GUILayout.Width(_w / 16), GUILayout.ExpandWidth(false));
            if (_bundleMasterRuntimeConfig.MaxDownLoadCount != maxDownLoadCount)
            {
                _bundleMasterRuntimeConfig.MaxDownLoadCount = maxDownLoadCount;
                needFlush = true;
            }
            GUILayout.Label("下载失败重试数: ", GUILayout.Width(_w / 10), GUILayout.ExpandWidth(false));
            int reDownLoadCount = _bundleMasterRuntimeConfig.ReDownLoadCount;
            reDownLoadCount = EditorGUILayout.IntField(reDownLoadCount, GUILayout.Width(_w / 16), GUILayout.ExpandWidth(false));
            if (_bundleMasterRuntimeConfig.ReDownLoadCount != reDownLoadCount)
            {
                _bundleMasterRuntimeConfig.ReDownLoadCount = reDownLoadCount;
                needFlush = true;
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("--- <构建AssetBundle配置> ----------------------------------------------------------------------------------------------------------------------------------------------------------------", GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("分包配置总索引文件: ", GUILayout.Width(_w / 8), GUILayout.ExpandWidth(false));
            _assetLoadTable =  (AssetLoadTable)EditorGUILayout.ObjectField(_assetLoadTable, typeof(AssetLoadTable), true, GUILayout.Width(_w / 3), GUILayout.ExpandWidth(false));
            bool noTable = _assetLoadTable == null;
            if (GUILayout.Button("查找或创建分包配置总索引文件", GUILayout.Width(_w / 5.5f), GUILayout.ExpandWidth(true)))
            {
                _assetLoadTable = AssetDatabase.LoadAssetAtPath<AssetLoadTable>(AssetLoadTablePath);
                if (_assetLoadTable == null)
                {
                    _assetLoadTable = ScriptableObject.CreateInstance<AssetLoadTable>();
                    AssetDatabase.CreateAsset(_assetLoadTable, BundleMasterWindow.AssetLoadTablePath);
                    needFlush = true;
                }
            }
            EditorGUI.BeginDisabledGroup(noTable);
            GUI.color = new Color(0.654902F, 0.9921569F, 0.2784314F);
            if (GUILayout.Button("添加一个分包配置", GUILayout.Width(_w / 6), GUILayout.ExpandWidth(true)))
            {
                int index = 0;
                while (true)
                {
                    AssetsLoadSetting assetLoadTable = AssetDatabase.LoadAssetAtPath<AssetsLoadSetting>(AssetsLoadSettingPath + "_" + index + ".asset");
                    if (assetLoadTable == null)
                    {
                        AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<AssetsLoadSetting>(), AssetsLoadSettingPath + "_" + index + ".asset");
                        break;
                    }
                    else
                    {
                        index++;
                    }
                }
                needFlush = true;
            }
            GUI.color = Color.white;
            GUILayout.EndHorizontal();

            if (!_viewSub)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("--- <入口场景> ----------------------------------------------------------------------------------------------------------------------------------------------------------------", GUILayout.ExpandWidth(false));
                GUILayout.EndHorizontal();
                GUILayout.BeginArea(new Rect(_w / 1.5f, 175, 400, 200));
                GUI.color = new Color(0.9921569F, 0.7960784F, 0.509804F);
                GUILayout.Label("初始场景是不需要打进AssetBundle里的, 这\n里填的初始场景会自动放入 Build Settings 中\n的 Scenes In Build 里。");
                GUI.color = Color.white;
                GUILayout.EndArea();
                GUILayout.BeginHorizontal();
                EditorGUI.BeginDisabledGroup(noTable);
                if (GUILayout.Button("增加一个入口场景", GUILayout.Width(_w / 6), GUILayout.ExpandWidth(false)))
                {
                    _assetLoadTable.InitScene.Add(null);
                    needFlush = true;
                }
                if (GUILayout.Button("清空所有入口场景", GUILayout.Width(_w / 6), GUILayout.ExpandWidth(false)))
                {
                    _assetLoadTable.InitScene.Clear();
                    needFlush = true;
                }
                EditorGUI.EndDisabledGroup();
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                scrollScenePos = EditorGUILayout.BeginScrollView(scrollScenePos, false, false, GUILayout.Height(_h / 6), GUILayout.ExpandHeight(true));
                if (!noTable)
                {
                    HashSet<int> needRemoveScene = new HashSet<int>();
                    for (int i = 0; i < _assetLoadTable.InitScene.Count; i++)
                    {
                        GUILayout.BeginHorizontal();
                        SceneAsset sceneAsset = _assetLoadTable.InitScene[i];
                        if (sceneAsset != null)
                        {
                            SceneAsset asset = (SceneAsset)EditorGUILayout.ObjectField(sceneAsset, typeof(SceneAsset), false, GUILayout.Width(_w / 3),GUILayout.ExpandHeight(false));
                            if (asset == null || asset != sceneAsset)
                            {
                                _assetLoadTable.InitScene[i] = asset;
                                needFlush = true;
                            }
                        }
                        else
                        {
                            SceneAsset asset = (SceneAsset)EditorGUILayout.ObjectField(null, typeof(SceneAsset), false, GUILayout.Width(_w / 3),GUILayout.ExpandHeight(false));
                            if (asset != null)
                            {
                                _assetLoadTable.InitScene[i] = asset;
                                needFlush = true;
                            }
                        }
                        if (GUILayout.Button("将此场景从入口场景中移除", GUILayout.Width(_w / 6), GUILayout.ExpandWidth(false)))
                        {
                            needRemoveScene.Add(i);
                        }
                        GUILayout.EndHorizontal();
                    }
                    if (needRemoveScene.Count > 0)
                    {
                        List<SceneAsset> changeSceneList = new List<SceneAsset>();
                        for (int i = 0; i < _assetLoadTable.InitScene.Count; i++)
                        {
                            if (!needRemoveScene.Contains(i))
                            {
                                changeSceneList.Add(_assetLoadTable.InitScene[i]);
                            }
                        }
                        _assetLoadTable.InitScene = changeSceneList;
                        needFlush = true;
                    }
                    
                }
                EditorGUILayout.EndScrollView();
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.Label("--- <配置索引索引文件信息> --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------", GUILayout.ExpandWidth(false));
                GUILayout.EndHorizontal();
                EditorGUI.BeginDisabledGroup(noTable);
                GUILayout.BeginHorizontal();
                GUILayout.Label("相对构建路径文件夹名称[BundlePath]: ", GUILayout.Width(_w / 4.5f), GUILayout.ExpandWidth(false));
                string bundlePath = "";
                if (!noTable)
                {
                    bundlePath = _assetLoadTable.BundlePath;
                }
                bundlePath = EditorGUILayout.TextField(bundlePath, GUILayout.Width(_w / 8), GUILayout.ExpandWidth(false));
                if (!noTable)
                {
                    if (!string.Equals(_assetLoadTable.BundlePath, bundlePath, StringComparison.Ordinal))
                    {
                        _assetLoadTable.BundlePath = bundlePath;
                        needFlush = true;
                    }
                }
                GUILayout.Label("是否启用绝对路径: ", GUILayout.Width(_w / 9), GUILayout.ExpandWidth(false));
                bool enableRelativePath = false;
                if (!noTable)
                {
                    enableRelativePath = _assetLoadTable.EnableRelativePath;
                }
                bool enableRelativePathChange = EditorGUILayout.Toggle(enableRelativePath, GUILayout.Width(_w / 80), GUILayout.ExpandWidth(false));
                if (!noTable)
                {
                    if (enableRelativePath != enableRelativePathChange)
                    {
                        _assetLoadTable.EnableRelativePath = enableRelativePathChange;
                        needFlush = true;
                    }
                }
                GUILayout.Label("绝对路径: ", GUILayout.Width(_w / 16), GUILayout.ExpandWidth(false));
                string relativePath = "";
                if (!noTable)
                {
                    relativePath = _assetLoadTable.RelativePath;
                }
                relativePath = EditorGUILayout.TextField(relativePath, GUILayout.Width(_w / 2.5f), GUILayout.ExpandWidth(false));
                if (!noTable)
                {
                    if (!string.Equals(_assetLoadTable.RelativePath, relativePath, StringComparison.Ordinal))
                    {
                        _assetLoadTable.RelativePath = relativePath;
                        needFlush = true;
                    }
                }
                GUILayout.EndHorizontal();
                GUILayout.Space(_h / 64);
                GUILayout.BeginHorizontal();
                GUILayout.Label("加密文件夹名称: ", GUILayout.Width(_w / 10), GUILayout.ExpandWidth(false));
                string encryptPathFolder = "";
                if (!noTable)
                {
                    encryptPathFolder = _assetLoadTable.EncryptPathFolder;
                }
                encryptPathFolder = EditorGUILayout.TextField(encryptPathFolder, GUILayout.Width(_w / 8), GUILayout.ExpandWidth(false));
                if (!noTable)
                {
                    if (!string.Equals(_assetLoadTable.EncryptPathFolder, encryptPathFolder, StringComparison.Ordinal))
                    {
                        _assetLoadTable.EncryptPathFolder = encryptPathFolder;
                        needFlush = true;
                    }
                }
                GUILayout.Label("是否生成路径代码: ", GUILayout.Width(_w / 9), GUILayout.ExpandWidth(false));
                bool generatePathCode = false;
                if (!noTable)
                {
                    generatePathCode = _assetLoadTable.GeneratePathCode;
                }
                bool generatePathCodeChange = EditorGUILayout.Toggle(generatePathCode, GUILayout.Width(_w / 80), GUILayout.ExpandWidth(false));
                if (!noTable)
                {
                    if (generatePathCode != generatePathCodeChange)
                    {
                        _assetLoadTable.GeneratePathCode = generatePathCodeChange;
                        needFlush = true;
                    }
                }
                GUILayout.Label("Assets下代码生成路径: ", GUILayout.Width(_w / 7), GUILayout.ExpandWidth(false));
                string generateCodeScriptPath = "";
                if (!noTable)
                {
                    generateCodeScriptPath = _assetLoadTable.GenerateCodeScriptPath;
                }
                generateCodeScriptPath = EditorGUILayout.TextField(generateCodeScriptPath, GUILayout.Width(_w / 4), GUILayout.ExpandWidth(false));
                if (!noTable)
                {
                    if (!string.Equals(_assetLoadTable.GenerateCodeScriptPath, generateCodeScriptPath, StringComparison.Ordinal))
                    {
                        _assetLoadTable.GenerateCodeScriptPath = generateCodeScriptPath;
                        needFlush = true;
                    }
                }
                GUILayout.EndHorizontal();
                EditorGUI.EndDisabledGroup();
                GUILayout.BeginHorizontal();
                GUILayout.Label("--- <所有分包配置文件> --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------", GUILayout.ExpandWidth(false));
                GUILayout.EndHorizontal();
                //处理单个分包
                GUILayout.BeginHorizontal();
                scrollPos = EditorGUILayout.BeginScrollView(scrollPos, false, false, GUILayout.ExpandHeight(true));
                foreach (string guid in AssetDatabase.FindAssets($"t:{nameof(AssetsLoadSetting)}"))
                {
                    AssetsLoadSetting loadSetting = AssetDatabase.LoadAssetAtPath<AssetsLoadSetting>(AssetDatabase.GUIDToAssetPath(guid));
                    GUILayout.BeginHorizontal();
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.ObjectField(loadSetting, typeof(AssetsLoadSetting), false, GUILayout.Width(_w / 3),GUILayout.ExpandHeight(false));
                    EditorGUI.EndDisabledGroup();
                    GUILayout.Label("是否启用当前分包配置 ", GUILayout.Width(_w / 7),GUILayout.ExpandHeight(false));
                    if (!noTable)
                    {
                        bool enable = _assetLoadTable.AssetsLoadSettings.Contains(loadSetting);
                        bool enableChange = EditorGUILayout.Toggle(enable);
                        if (enable != enableChange)
                        {
                            if (enableChange)
                            {
                                _assetLoadTable.AssetsLoadSettings.Add(loadSetting);
                            }
                            else
                            {
                                _assetLoadTable.AssetsLoadSettings.Remove(loadSetting);
                            }
                            needFlush = true;
                        }
                    }
                    GUI.color = new Color(0.9921569F, 0.7960784F, 0.509804F);
                    if (GUILayout.Button("选择查看此分包配置信息", GUILayout.Width(_w / 4), GUILayout.ExpandWidth(false)))
                    {
                        _selectAssetsLoadSetting = loadSetting;
                        _viewSub = true;
                    }
                    GUI.color = Color.white;
                    GUILayout.EndHorizontal();
                }
                EditorGUILayout.EndScrollView();
                GUILayout.EndHorizontal();
            }
            else
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("--- <选中的分包配置信息> --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------", GUILayout.ExpandWidth(false));
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.Label("当前选择的分包配置信息文件: ", GUILayout.ExpandWidth(false));
                _selectAssetsLoadSetting = (AssetsLoadSetting)EditorGUILayout.ObjectField(_selectAssetsLoadSetting, typeof(AssetsLoadSetting), true, GUILayout.Width(_w / 3),GUILayout.ExpandHeight(false));
                bool noLoadSetting = _selectAssetsLoadSetting == null;
                EditorGUI.BeginDisabledGroup(noLoadSetting);
                GUILayout.Label("分包名: ", GUILayout.Width(_w / 20), GUILayout.ExpandWidth(false));
                string buildName = "";
                if (!noLoadSetting)
                {
                    buildName = _selectAssetsLoadSetting.BuildName;
                }
                buildName = EditorGUILayout.TextField(buildName, GUILayout.Width(_w / 8), GUILayout.ExpandWidth(false));
                if (!noLoadSetting)
                {
                    if (!string.Equals(_selectAssetsLoadSetting.BuildName, buildName, StringComparison.Ordinal))
                    {
                        _selectAssetsLoadSetting.BuildName = buildName;
                        needFlush = true;
                    }
                }
                GUILayout.Label("版本索引: ", GUILayout.Width(_w / 17), GUILayout.ExpandWidth(false));
                int buildIndex = 0;
                if (!noLoadSetting)
                {
                    buildIndex = _selectAssetsLoadSetting.BuildIndex;
                }
                buildIndex = EditorGUILayout.IntField(buildIndex, GUILayout.Width(_w / 20), GUILayout.ExpandWidth(false));
                if (!noLoadSetting)
                {
                    if (buildIndex != _selectAssetsLoadSetting.BuildIndex)
                    {
                        _selectAssetsLoadSetting.BuildIndex = buildIndex;
                        needFlush = true;
                    }
                }
                GUI.color = new Color(0.9921569F, 0.2745098F, 0.282353F);
                if (GUILayout.Button("删除当前选择的分包配置"))
                {
                    _assetLoadTable.AssetsLoadSettings.Remove(_selectAssetsLoadSetting);
                    AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(_selectAssetsLoadSetting));
                    needFlush = true;
                }
                GUI.color = Color.white;
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.Label("AssetBundle后缀: ", GUILayout.Width(_w / 9), GUILayout.ExpandWidth(false));
                string bundleVariant = "";
                if (!noLoadSetting)
                {
                    bundleVariant = _selectAssetsLoadSetting.BundleVariant;
                }
                bundleVariant = EditorGUILayout.TextField(bundleVariant, GUILayout.Width(_w / 10), GUILayout.ExpandWidth(false));
                if (!noLoadSetting)
                {
                    if (!string.Equals(_selectAssetsLoadSetting.BundleVariant, bundleVariant, StringComparison.Ordinal))
                    {
                        _selectAssetsLoadSetting.BundleVariant = bundleVariant;
                        needFlush = true;
                    }
                }
                GUILayout.Label("是否启用Hash名:", GUILayout.Width(_w / 10), GUILayout.ExpandWidth(false));
                bool nameByHash = false;
                if (!noLoadSetting)
                {
                    nameByHash = _selectAssetsLoadSetting.NameByHash;
                }
                bool nameByHashChange = EditorGUILayout.Toggle(nameByHash, GUILayout.Width(_w / 80), GUILayout.ExpandWidth(false));
                if (!noLoadSetting)
                {
                    if (nameByHash != nameByHashChange)
                    {
                        _selectAssetsLoadSetting.NameByHash = nameByHashChange;
                        needFlush = true;
                    }
                }
                GUILayout.Label("构建选项", GUILayout.Width(_w / 17), GUILayout.ExpandWidth(false));
                BuildAssetBundleOptions buildAssetBundleOptions = BuildAssetBundleOptions.None;
                if (!noLoadSetting)
                {
                    buildAssetBundleOptions = _selectAssetsLoadSetting.BuildAssetBundleOptions;
                }
                BuildAssetBundleOptions buildAssetBundleOptionsChange = (BuildAssetBundleOptions)EditorGUILayout.EnumFlagsField(buildAssetBundleOptions, GUILayout.Width(_w / 5), GUILayout.ExpandWidth(false));
                if (!noLoadSetting)
                {
                    if (buildAssetBundleOptions != buildAssetBundleOptionsChange)
                    {
                        _selectAssetsLoadSetting.BuildAssetBundleOptions = buildAssetBundleOptionsChange;
                        needFlush = true;
                    }
                }
                GUILayout.Label("生成加密资源: ", GUILayout.Width(_w / 12), GUILayout.ExpandWidth(false));
                bool encryptAssets = false;
                if (!noLoadSetting)
                {
                    encryptAssets = _selectAssetsLoadSetting.EncryptAssets;
                }
                bool encryptAssetsChange = EditorGUILayout.Toggle(encryptAssets, GUILayout.Width(_w / 80), GUILayout.ExpandWidth(false));
                if (!noLoadSetting)
                {
                    if (encryptAssets != encryptAssetsChange)
                    {
                        _selectAssetsLoadSetting.EncryptAssets = encryptAssetsChange;
                        needFlush = true;
                    }
                }
                GUILayout.Label("加密密钥: ", GUILayout.Width(_w / 16), GUILayout.ExpandWidth(false));
                string secretKey = "";
                if (!noLoadSetting)
                {
                    secretKey = _selectAssetsLoadSetting.SecretKey;
                }
                secretKey = EditorGUILayout.TextField(secretKey, GUILayout.Width(_w / 6), GUILayout.ExpandWidth(false));
                if (!noLoadSetting)
                {
                    if (!string.Equals(_selectAssetsLoadSetting.SecretKey, secretKey, StringComparison.Ordinal))
                    {
                        _selectAssetsLoadSetting.SecretKey = secretKey;
                        needFlush = true;
                    }
                }
                GUILayout.EndHorizontal();
                //遍历
                if (!noLoadSetting)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("--- <分包包含场景> --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------", GUILayout.ExpandWidth(false));
                    GUILayout.EndHorizontal();
                    //遍历引用场景
                    GUILayout.BeginHorizontal();
                    scrollBundleScenePos = EditorGUILayout.BeginScrollView(scrollBundleScenePos, false, false, GUILayout.Height(_h / 4), GUILayout.ExpandHeight(true));
                    foreach (string guid in AssetDatabase.FindAssets($"t:{nameof(SceneAsset)}"))
                    {
                        SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(AssetDatabase.GUIDToAssetPath(guid));
                        if (!_assetLoadTable.InitScene.Contains(sceneAsset))
                        {
                            GUILayout.BeginHorizontal();
                            EditorGUI.BeginDisabledGroup(true);
                            EditorGUILayout.ObjectField(sceneAsset, typeof(SceneAsset), false, GUILayout.Width(_w / 6),GUILayout.ExpandHeight(false));
                            EditorGUI.EndDisabledGroup();
                            GUILayout.Label("是否将当前场景放进分包: ", GUILayout.Width(_w / 6),GUILayout.ExpandHeight(false));
                            bool enable = _selectAssetsLoadSetting.Scene.Contains(sceneAsset);
                            bool enableChange = EditorGUILayout.Toggle(enable);
                            if (enable != enableChange)
                            {
                                if (enableChange)
                                {
                                    _selectAssetsLoadSetting.Scene.Add(sceneAsset);
                                }
                                else
                                {
                                    _selectAssetsLoadSetting.Scene.Remove(sceneAsset);
                                }
                                needFlush = true;
                            }
                            GUILayout.EndHorizontal();
                        }
                    }
                    EditorGUILayout.EndScrollView();
                    GUILayout.EndHorizontal();
                
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("--- <分包资源路径> --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------", GUILayout.ExpandWidth(false));
                    GUILayout.EndHorizontal();
                
                    //遍历构建路径
                    GUILayout.BeginHorizontal();
                    scrollPathPos = EditorGUILayout.BeginScrollView(scrollPathPos, false, false, GUILayout.ExpandHeight(true));
                    List<string> assetPaths = _selectAssetsLoadSetting.AssetPath;
                    HashSet<int> needRemovePath = new HashSet<int>();
                    for (int i = 0; i < assetPaths.Count; i++)
                    {
                        GUILayout.BeginHorizontal();
                        string assetPath = assetPaths[i];
                        assetPath = EditorGUILayout.TextField(assetPath, GUILayout.ExpandWidth(true));
                        if (!string.Equals(assetPaths[i], assetPath, StringComparison.Ordinal))
                        {
                            assetPaths[i] = assetPath;
                            needFlush = true;
                        }
                        GUI.color = new Color(0.9921569F, 0.2745098F, 0.282353F);
                        if (GUILayout.Button("删除当前路径", GUILayout.MaxWidth(_h / 6), GUILayout.ExpandWidth(false)))
                        {
                            needRemovePath.Add(i);
                        }
                        GUI.color = Color.white;
                        GUILayout.EndHorizontal();
                    }
                    if (needRemovePath.Count > 0)
                    {
                        List<string> changeAssetPath = new List<string>();
                        for (int i = 0; i < assetPaths.Count; i++)
                        {
                            if (!needRemovePath.Contains(i))
                            {
                                changeAssetPath.Add(assetPaths[i]);
                            }
                        }
                        _selectAssetsLoadSetting.AssetPath = changeAssetPath;
                        needFlush = true;
                    }
                    EditorGUILayout.EndScrollView();
                    GUILayout.EndHorizontal();
                }
                GUILayout.BeginHorizontal();
                GUI.color = new Color(0.654902F, 0.9921569F, 0.2784314F);
                if (GUILayout.Button("添加一个新路径", GUILayout.Height(_h / 20), GUILayout.ExpandWidth(true)))
                {
                    _selectAssetsLoadSetting.AssetPath.Add(null);
                    needFlush = true;
                }
                GUI.color = Color.white;
                EditorGUI.EndDisabledGroup();
                GUI.color = new Color(0.9921569F, 0.7960784F, 0.509804F);
                if (GUILayout.Button("分包编辑完成", GUILayout.Width(100), GUILayout.Height(_h / 20), GUILayout.ExpandWidth(true)))
                {
                    _viewSub = false;
                }
                GUI.color = Color.white;
                GUILayout.EndHorizontal();
            }
            
            if (needFlush)
            {
                foreach (string guid in AssetDatabase.FindAssets($"t:{nameof(BundleMasterRuntimeConfig)}"))
                {
                    EditorUtility.SetDirty(AssetDatabase.LoadAssetAtPath<BundleMasterRuntimeConfig>(AssetDatabase.GUIDToAssetPath(guid)));
                }
                foreach (string guid in AssetDatabase.FindAssets($"t:{nameof(AssetLoadTable)}"))
                {
                    EditorUtility.SetDirty(AssetDatabase.LoadAssetAtPath<AssetLoadTable>(AssetDatabase.GUIDToAssetPath(guid)));
                }
                foreach (string guid in AssetDatabase.FindAssets($"t:{nameof(AssetsLoadSetting)}"))
                {
                    EditorUtility.SetDirty(AssetDatabase.LoadAssetAtPath<AssetsLoadSetting>(AssetDatabase.GUIDToAssetPath(guid)));
                }
                AssetDatabase.SaveAssets();
            }
        }
        
        public void OnDestroy()
        {
            _instance = null;
        }
    }
}