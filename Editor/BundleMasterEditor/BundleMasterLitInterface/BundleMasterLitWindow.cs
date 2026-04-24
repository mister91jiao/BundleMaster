using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BM
{
    /// <summary>
    /// BundleMaster 的新版配置界面。
    /// </summary>
    public class BundleMasterLitWindow : EditorWindow
    {
        private const float MinWindowWidth = 1120f;
        private const float MinWindowHeight = 680f;
        private const float SidebarWidth = 330f;

        private readonly List<AssetsSetting> _allSettings = new List<AssetsSetting>();
        private readonly List<SceneAsset> _allScenes = new List<SceneAsset>();

        private BundleMasterRuntimeConfig _runtimeConfig;
        private AssetLoadTable _assetLoadTable;
        private AssetsSetting _selectedSetting;

        private Vector2 _mainScrollPos;
        private Vector2 _sidebarScrollPos;
        private Vector2 _entrySceneScrollPos;
        private Vector2 _bundleSceneScrollPos;
        private Vector2 _assetPathScrollPos;
        private Vector2 _groupPathScrollPos;
        private Vector2 _originScrollPos;

        private string _filterText = "";

        [MenuItem("Tools/BuildAsset/打开配置界面_新界面")]
        public static void Init()
        {
            BundleMasterLitWindow window = GetWindow<BundleMasterLitWindow>("BundleMaster Lit");
            window.minSize = new Vector2(MinWindowWidth, MinWindowHeight);
            window.RefreshAll();
            window.Show();
        }

        private void OnEnable()
        {
            RefreshAll();
        }

        private void OnGUI()
        {
            DrawToolbar();

            if (_runtimeConfig == null)
            {
                DrawRuntimeConfigMissing();
                return;
            }

            DrawRuntimeConfig();
            DrawContent();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("BundleMaster Lit", EditorStyles.boldLabel, GUILayout.Width(150));

            if (GUILayout.Button("刷新", EditorStyles.toolbarButton, GUILayout.Width(64)))
            {
                RefreshAll();
            }

            if (GUILayout.Button("构建AssetBundle", EditorStyles.toolbarButton, GUILayout.Width(120)))
            {
                BuildAssets.BuildAllBundle();
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawRuntimeConfigMissing()
        {
            GUILayout.FlexibleSpace();
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(480));
            GUILayout.Label("未找到运行时配置", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("需要先创建 BMConfig.asset。新版界面仍然使用旧工具相同的配置文件路径。", MessageType.Info);
            if (GUILayout.Button("创建运行时配置文件", GUILayout.Height(36)))
            {
                CreateRuntimeConfig();
            }
            EditorGUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
        }

        private void DrawRuntimeConfig()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("运行时加载配置", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal(GUILayout.Height(54));
            DrawLoadModeButton(AssetLoadMode.Develop, "开发模式", "AssetDatabase 直接加载", new Color(0.35f, 0.72f, 0.42f));
            DrawLoadModeButton(AssetLoadMode.Local, "本地模式", "加载本地最新 AB", new Color(0.35f, 0.55f, 0.9f));
            DrawLoadModeButton(AssetLoadMode.Build, "构建模式", "版本对比热更流程", new Color(0.92f, 0.62f, 0.28f));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            int maxDownLoadCount = EditorGUILayout.IntField("最大下载数", _runtimeConfig.MaxDownLoadCount);
            if (maxDownLoadCount != _runtimeConfig.MaxDownLoadCount)
            {
                _runtimeConfig.MaxDownLoadCount = Mathf.Max(1, maxDownLoadCount);
                MarkDirty(_runtimeConfig);
            }

            int reDownLoadCount = EditorGUILayout.IntField("失败重试数", _runtimeConfig.ReDownLoadCount);
            if (reDownLoadCount != _runtimeConfig.ReDownLoadCount)
            {
                _runtimeConfig.ReDownLoadCount = Mathf.Max(0, reDownLoadCount);
                MarkDirty(_runtimeConfig);
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void DrawLoadModeButton(AssetLoadMode mode, string title, string description, Color activeColor)
        {
            bool isActive = _runtimeConfig.AssetLoadMode == mode;
            Color oldBackgroundColor = GUI.backgroundColor;
            GUI.backgroundColor = isActive ? activeColor : Color.white;

            string buttonText = title + "\n" + description;
            if (GUILayout.Button(buttonText, GUILayout.Height(50), GUILayout.ExpandWidth(true)))
            {
                SetLoadMode(mode);
            }

            GUI.backgroundColor = oldBackgroundColor;
        }

        private void SetLoadMode(AssetLoadMode loadMode)
        {
            if (_runtimeConfig.AssetLoadMode == loadMode)
            {
                return;
            }

            _runtimeConfig.AssetLoadMode = loadMode;
            if (_assetLoadTable != null)
            {
                DevelopSceneChange.CheckSceneChange(loadMode);
            }
            MarkDirty(_runtimeConfig);
        }

        private void DrawContent()
        {
            EditorGUILayout.BeginHorizontal();
            DrawSidebar();
            DrawMainPanel();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawSidebar()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(SidebarWidth), GUILayout.ExpandHeight(true));
            GUILayout.Label("配置资产", EditorStyles.boldLabel);

            _assetLoadTable = (AssetLoadTable)EditorGUILayout.ObjectField("总索引", _assetLoadTable, typeof(AssetLoadTable), false);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("查找/创建总索引"))
            {
                EnsureAssetLoadTable();
            }
            EditorGUI.BeginDisabledGroup(_assetLoadTable == null);
            if (GUILayout.Button("新建分包"))
            {
                CreateAssetsLoadSetting();
            }
            if (GUILayout.Button("新建原生包"))
            {
                CreateAssetsOriginSetting();
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            if (_assetLoadTable == null)
            {
                EditorGUILayout.HelpBox("未选择 AssetLoadTable，无法编辑分包启用状态。", MessageType.Warning);
                EditorGUILayout.EndVertical();
                return;
            }
            EnsureAssetLoadTableLists(_assetLoadTable);

            GUILayout.Space(6);
            _filterText = EditorGUILayout.TextField("过滤", _filterText);

            GUILayout.Space(4);
            _sidebarScrollPos = EditorGUILayout.BeginScrollView(_sidebarScrollPos);
            foreach (AssetsSetting setting in _allSettings)
            {
                if (setting == null)
                {
                    continue;
                }

                string displayName = GetSettingDisplayName(setting);
                if (!string.IsNullOrEmpty(_filterText) &&
                    displayName.IndexOf(_filterText, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                DrawSettingListItem(setting, displayName);
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.EndVertical();
        }

        private void DrawSettingListItem(AssetsSetting setting, string displayName)
        {
            bool selected = setting == _selectedSetting;
            GUIStyle style = selected ? EditorStyles.helpBox : GUIStyle.none;
            EditorGUILayout.BeginVertical(style);
            EditorGUILayout.BeginHorizontal();

            bool enabled = _assetLoadTable.AssetsSettings.Contains(setting);
            bool enabledChange = EditorGUILayout.Toggle(enabled, GUILayout.Width(18));
            if (enabledChange != enabled)
            {
                if (enabledChange)
                {
                    _assetLoadTable.AssetsSettings.Add(setting);
                }
                else
                {
                    _assetLoadTable.AssetsSettings.Remove(setting);
                }
                MarkDirty(_assetLoadTable);
            }

            string typeName = setting is AssetsLoadSetting ? "AB" : "原生";
            GUILayout.Label(typeName, GUILayout.Width(34));
            if (GUILayout.Button(displayName, EditorStyles.label))
            {
                _selectedSetting = setting;
                GUI.FocusControl(null);
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void DrawMainPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            _mainScrollPos = EditorGUILayout.BeginScrollView(_mainScrollPos);

            DrawAssetLoadTable();

            GUILayout.Space(8);
            if (_selectedSetting is AssetsLoadSetting assetsLoadSetting)
            {
                DrawAssetsLoadSetting(assetsLoadSetting);
            }
            else if (_selectedSetting is AssetsOriginSetting assetsOriginSetting)
            {
                DrawAssetsOriginSetting(assetsOriginSetting);
            }
            else
            {
                EditorGUILayout.HelpBox("从左侧选择一个分包配置进行编辑。", MessageType.Info);
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawAssetLoadTable()
        {
            if (_assetLoadTable == null)
            {
                return;
            }
            EnsureAssetLoadTableLists(_assetLoadTable);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("总索引配置", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            string bundlePath = EditorGUILayout.TextField("构建路径文件夹", _assetLoadTable.BundlePath);
            if (!string.Equals(bundlePath, _assetLoadTable.BundlePath, StringComparison.Ordinal))
            {
                _assetLoadTable.BundlePath = bundlePath;
                MarkDirty(_assetLoadTable);
            }

            bool enableRelativePath = EditorGUILayout.Toggle("启用绝对路径", _assetLoadTable.EnableRelativePath);
            if (enableRelativePath != _assetLoadTable.EnableRelativePath)
            {
                _assetLoadTable.EnableRelativePath = enableRelativePath;
                MarkDirty(_assetLoadTable);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(!_assetLoadTable.EnableRelativePath);
            string relativePath = EditorGUILayout.TextField("绝对路径", _assetLoadTable.RelativePath);
            if (!string.Equals(relativePath, _assetLoadTable.RelativePath, StringComparison.Ordinal))
            {
                _assetLoadTable.RelativePath = relativePath;
                MarkDirty(_assetLoadTable);
            }
            EditorGUI.EndDisabledGroup();

#if !(Nintendo_Switch || BMWebGL)
            string encryptPathFolder = EditorGUILayout.TextField("加密目录", _assetLoadTable.EncryptPathFolder);
            if (!string.Equals(encryptPathFolder, _assetLoadTable.EncryptPathFolder, StringComparison.Ordinal))
            {
                _assetLoadTable.EncryptPathFolder = encryptPathFolder;
                MarkDirty(_assetLoadTable);
            }

#endif
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            bool generatePathCode = EditorGUILayout.Toggle("生成路径代码", _assetLoadTable.GeneratePathCode);
            if (generatePathCode != _assetLoadTable.GeneratePathCode)
            {
                _assetLoadTable.GeneratePathCode = generatePathCode;
                MarkDirty(_assetLoadTable);
            }

            string generateCodeScriptPath = EditorGUILayout.TextField("代码生成路径", _assetLoadTable.GenerateCodeScriptPath);
            if (!string.Equals(generateCodeScriptPath, _assetLoadTable.GenerateCodeScriptPath, StringComparison.Ordinal))
            {
                _assetLoadTable.GenerateCodeScriptPath = generateCodeScriptPath;
                MarkDirty(_assetLoadTable);
            }
            EditorGUILayout.EndHorizontal();

            DrawEntryScenes();
            EditorGUILayout.EndVertical();
        }

        private void DrawEntryScenes()
        {
            GUILayout.Space(6);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("入口场景", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("增加入口场景", GUILayout.Width(120)))
            {
                _assetLoadTable.InitScene.Add(null);
                MarkDirty(_assetLoadTable);
            }

            if (GUILayout.Button("清空入口场景", GUILayout.Width(120)))
            {
                if (EditorUtility.DisplayDialog("清空入口场景", "确定清空所有入口场景？", "清空", "取消"))
                {
                    _assetLoadTable.InitScene.Clear();
                    MarkDirty(_assetLoadTable);
                }
            }
            EditorGUILayout.EndHorizontal();

            _entrySceneScrollPos = EditorGUILayout.BeginScrollView(_entrySceneScrollPos, GUILayout.Height(110));
            for (int i = 0; i < _assetLoadTable.InitScene.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                SceneAsset sceneAsset = (SceneAsset)EditorGUILayout.ObjectField(_assetLoadTable.InitScene[i], typeof(SceneAsset), false);
                if (sceneAsset != _assetLoadTable.InitScene[i])
                {
                    _assetLoadTable.InitScene[i] = sceneAsset;
                    MarkDirty(_assetLoadTable);
                }

                if (GUILayout.Button("移除", GUILayout.Width(64)))
                {
                    _assetLoadTable.InitScene.RemoveAt(i);
                    MarkDirty(_assetLoadTable);
                    i--;
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawAssetsLoadSetting(AssetsLoadSetting setting)
        {
            EnsureAssetsLoadSettingLists(setting);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            DrawSettingHeader(setting, "普通 AssetBundle 分包");

            EditorGUILayout.BeginHorizontal();
            string buildName = EditorGUILayout.TextField("分包名", setting.BuildName);
            if (!string.Equals(buildName, setting.BuildName, StringComparison.Ordinal))
            {
                setting.BuildName = buildName;
                MarkDirty(setting);
            }

            int buildIndex = EditorGUILayout.IntField("版本索引", setting.BuildIndex);
            if (buildIndex != setting.BuildIndex)
            {
                setting.BuildIndex = buildIndex;
                MarkDirty(setting);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            string bundleVariant = EditorGUILayout.TextField("AssetBundle后缀", setting.BundleVariant);
            if (!string.Equals(bundleVariant, setting.BundleVariant, StringComparison.Ordinal))
            {
                setting.BundleVariant = bundleVariant;
                MarkDirty(setting);
            }

            bool nameByHash = EditorGUILayout.Toggle("Hash命名", setting.NameByHash);
            if (nameByHash != setting.NameByHash)
            {
                setting.NameByHash = nameByHash;
                MarkDirty(setting);
            }
            EditorGUILayout.EndHorizontal();

            BuildAssetBundleOptions options = (BuildAssetBundleOptions)EditorGUILayout.EnumFlagsField("构建选项", setting.BuildAssetBundleOptions);
            if (options != setting.BuildAssetBundleOptions)
            {
                setting.BuildAssetBundleOptions = options;
                MarkDirty(setting);
            }

#if !(Nintendo_Switch || BMWebGL)
            EditorGUILayout.BeginHorizontal();
            bool encryptAssets = EditorGUILayout.Toggle("生成加密资源", setting.EncryptAssets);
            if (encryptAssets != setting.EncryptAssets)
            {
                setting.EncryptAssets = encryptAssets;
                MarkDirty(setting);
            }

            EditorGUILayout.EndHorizontal();

            string secretKey = EditorGUILayout.TextField("加密密钥", setting.SecretKey);
            if (!string.Equals(secretKey, setting.SecretKey, StringComparison.Ordinal))
            {
                setting.SecretKey = secretKey;
                MarkDirty(setting);
            }
#endif

            GUILayout.Space(8);
            DrawBundleScenes(setting);
            GUILayout.Space(8);
            DrawStringPathList("主动加载资源路径", setting.AssetPath, setting, ref _assetPathScrollPos, true);
            GUILayout.Space(8);
            DrawStringPathList("组包资源路径", setting.AssetGroupPaths, setting, ref _groupPathScrollPos, true);

            EditorGUILayout.EndVertical();
        }

        private void DrawBundleScenes(AssetsLoadSetting setting)
        {
            GUILayout.Label("分包场景", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("入口场景不会出现在此列表中；Develop 模式会把分包场景同步到 Build Settings。", MessageType.None);

            _bundleSceneScrollPos = EditorGUILayout.BeginScrollView(_bundleSceneScrollPos, GUILayout.Height(210));
            foreach (SceneAsset sceneAsset in _allScenes)
            {
                if (sceneAsset == null || (_assetLoadTable != null && _assetLoadTable.InitScene.Contains(sceneAsset)))
                {
                    continue;
                }

                EditorGUILayout.BeginHorizontal();
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.ObjectField(sceneAsset, typeof(SceneAsset), false);
                EditorGUI.EndDisabledGroup();

                bool contains = setting.Scene.Contains(sceneAsset);
                bool containsChange = EditorGUILayout.ToggleLeft("打进分包", contains, GUILayout.Width(90));
                if (containsChange != contains)
                {
                    if (containsChange)
                    {
                        setting.Scene.Add(sceneAsset);
                    }
                    else
                    {
                        setting.Scene.Remove(sceneAsset);
                    }

                    MarkDirty(setting);
                    if (_runtimeConfig != null && _runtimeConfig.AssetLoadMode == AssetLoadMode.Develop)
                    {
                        DevelopSceneChange.CheckSceneChange(_runtimeConfig.AssetLoadMode);
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawAssetsOriginSetting(AssetsOriginSetting setting)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            DrawSettingHeader(setting, "原生资源分包");

            _originScrollPos = EditorGUILayout.BeginScrollView(_originScrollPos, GUILayout.Height(220));
            string buildName = EditorGUILayout.TextField("分包名", setting.BuildName);
            if (!string.Equals(buildName, setting.BuildName, StringComparison.Ordinal))
            {
                setting.BuildName = buildName;
                MarkDirty(setting);
            }

            int buildIndex = EditorGUILayout.IntField("版本索引", setting.BuildIndex);
            if (buildIndex != setting.BuildIndex)
            {
                setting.BuildIndex = buildIndex;
                MarkDirty(setting);
            }

            EditorGUILayout.BeginHorizontal();
            string originFilePath = EditorGUILayout.TextField("原生资源路径", setting.OriginFilePath);
            if (!string.Equals(originFilePath, setting.OriginFilePath, StringComparison.Ordinal))
            {
                setting.OriginFilePath = originFilePath;
                MarkDirty(setting);
            }

            if (GUILayout.Button("选择目录", GUILayout.Width(90)))
            {
                string selectedPath = PickAssetFolder(setting.OriginFilePath);
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    setting.OriginFilePath = selectedPath;
                    MarkDirty(setting);
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private static void EnsureAssetsLoadSettingLists(AssetsLoadSetting setting)
        {
            if (setting.AssetPath == null)
            {
                setting.AssetPath = new List<string>();
            }

            if (setting.AssetGroupPaths == null)
            {
                setting.AssetGroupPaths = new List<string>();
            }

            if (setting.Scene == null)
            {
                setting.Scene = new List<SceneAsset>();
            }
        }

        private static void EnsureAssetLoadTableLists(AssetLoadTable assetLoadTable)
        {
            if (assetLoadTable.InitScene == null)
            {
                assetLoadTable.InitScene = new List<SceneAsset>();
            }

            if (assetLoadTable.AssetsSettings == null)
            {
                assetLoadTable.AssetsSettings = new List<AssetsSetting>();
            }
        }

        private void DrawSettingHeader(AssetsSetting setting, string title)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(title, EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField(setting, setting.GetType(), false, GUILayout.Width(280));
            EditorGUI.EndDisabledGroup();

            GUI.color = new Color(1f, 0.45f, 0.45f);
            if (GUILayout.Button("删除配置", GUILayout.Width(90)))
            {
                DeleteSetting(setting);
            }
            GUI.color = Color.white;
            EditorGUILayout.EndHorizontal();
        }

        private void DrawStringPathList(string title, List<string> paths, UnityEngine.Object owner, ref Vector2 scrollPos, bool assetFolderOnly)
        {
            GUILayout.Label(title, EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("增加路径", GUILayout.Width(90)))
            {
                paths.Add("");
                MarkDirty(owner);
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(145));
            for (int i = 0; i < paths.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                string path = EditorGUILayout.TextField(paths[i]);
                if (!string.Equals(path, paths[i], StringComparison.Ordinal))
                {
                    paths[i] = path;
                    MarkDirty(owner);
                }

                if (assetFolderOnly && GUILayout.Button("选择", GUILayout.Width(54)))
                {
                    string selectedPath = PickAssetFolder(paths[i]);
                    if (!string.IsNullOrEmpty(selectedPath))
                    {
                        paths[i] = selectedPath;
                        MarkDirty(owner);
                    }
                }

                if (GUILayout.Button("删除", GUILayout.Width(54)))
                {
                    paths.RemoveAt(i);
                    MarkDirty(owner);
                    i--;
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }

        private void RefreshAll()
        {
            _runtimeConfig = AssetDatabase.LoadAssetAtPath<BundleMasterRuntimeConfig>(BundleMasterAssetsPath.RuntimeConfigPath);
            _assetLoadTable = AssetDatabase.LoadAssetAtPath<AssetLoadTable>(BundleMasterAssetsPath.AssetLoadTablePath);
            RefreshSettings();
            RefreshScenes();
        }

        private void RefreshSettings()
        {
            _allSettings.Clear();
            HashSet<AssetsSetting> addedSettings = new HashSet<AssetsSetting>();

            AddSettingsByType<AssetsLoadSetting>(addedSettings);
            AddSettingsByType<AssetsOriginSetting>(addedSettings);

            _allSettings.Sort((a, b) => string.Compare(GetSettingDisplayName(a), GetSettingDisplayName(b), StringComparison.OrdinalIgnoreCase));

            if (_selectedSetting != null && !_allSettings.Contains(_selectedSetting))
            {
                _selectedSetting = null;
            }
        }

        private void AddSettingsByType<T>(HashSet<AssetsSetting> addedSettings) where T : AssetsSetting
        {
            foreach (string guid in AssetDatabase.FindAssets("t:" + typeof(T).Name))
            {
                T setting = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
                if (setting != null && addedSettings.Add(setting))
                {
                    _allSettings.Add(setting);
                }
            }
        }

        private void RefreshScenes()
        {
            _allScenes.Clear();
            foreach (string guid in AssetDatabase.FindAssets("t:" + nameof(SceneAsset)))
            {
                SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(AssetDatabase.GUIDToAssetPath(guid));
                if (sceneAsset != null)
                {
                    _allScenes.Add(sceneAsset);
                }
            }

            _allScenes.Sort((a, b) => string.Compare(AssetDatabase.GetAssetPath(a), AssetDatabase.GetAssetPath(b), StringComparison.OrdinalIgnoreCase));
        }

        private void CreateRuntimeConfig()
        {
            EnsureAssetFolder(BundleMasterAssetsPath.RuntimeConfigPath);
            _runtimeConfig = ScriptableObject.CreateInstance<BundleMasterRuntimeConfig>();
            _runtimeConfig.AssetLoadMode = AssetLoadMode.Develop;
            _runtimeConfig.MaxDownLoadCount = 8;
            _runtimeConfig.ReDownLoadCount = 3;
            AssetDatabase.CreateAsset(_runtimeConfig, BundleMasterAssetsPath.RuntimeConfigPath);
            MarkDirty(_runtimeConfig);
            RefreshAll();
        }

        private void EnsureAssetLoadTable()
        {
            _assetLoadTable = AssetDatabase.LoadAssetAtPath<AssetLoadTable>(BundleMasterAssetsPath.AssetLoadTablePath);
            if (_assetLoadTable == null)
            {
                EnsureAssetFolder(BundleMasterAssetsPath.AssetLoadTablePath);
                _assetLoadTable = ScriptableObject.CreateInstance<AssetLoadTable>();
                AssetDatabase.CreateAsset(_assetLoadTable, BundleMasterAssetsPath.AssetLoadTablePath);
                MarkDirty(_assetLoadTable);
            }

            RefreshAll();
        }

        private void CreateAssetsLoadSetting()
        {
            AssetsLoadSetting setting = ScriptableObject.CreateInstance<AssetsLoadSetting>();
            string assetPath = GetNextAssetPath(BundleMasterAssetsPath.AssetsLoadSettingPath, "asset");
            EnsureAssetFolder(assetPath);
            AssetDatabase.CreateAsset(setting, assetPath);

            EnableSetting(setting);
            _selectedSetting = setting;
            MarkDirty(setting);
            RefreshAll();
            _selectedSetting = setting;
        }

        private void CreateAssetsOriginSetting()
        {
            AssetsOriginSetting setting = ScriptableObject.CreateInstance<AssetsOriginSetting>();
            string assetPath = GetNextAssetPath(BundleMasterAssetsPath.AssetsOriginSettingPath, "asset");
            EnsureAssetFolder(assetPath);
            AssetDatabase.CreateAsset(setting, assetPath);

            EnableSetting(setting);
            _selectedSetting = setting;
            MarkDirty(setting);
            RefreshAll();
            _selectedSetting = setting;
        }

        private void EnableSetting(AssetsSetting setting)
        {
            if (_assetLoadTable == null || setting == null)
            {
                return;
            }

            if (!_assetLoadTable.AssetsSettings.Contains(setting))
            {
                _assetLoadTable.AssetsSettings.Add(setting);
                MarkDirty(_assetLoadTable);
            }
        }

        private void DeleteSetting(AssetsSetting setting)
        {
            if (setting == null)
            {
                return;
            }

            string assetPath = AssetDatabase.GetAssetPath(setting);
            if (!EditorUtility.DisplayDialog("删除配置", "确定删除配置文件？\n" + assetPath, "删除", "取消"))
            {
                return;
            }

            if (_assetLoadTable != null)
            {
                _assetLoadTable.AssetsSettings.Remove(setting);
                MarkDirty(_assetLoadTable);
            }

            AssetDatabase.DeleteAsset(assetPath);
            if (_selectedSetting == setting)
            {
                _selectedSetting = null;
            }

            RefreshAll();
            SaveAssetDatabase();
        }

        private void MarkDirty(UnityEngine.Object target)
        {
            if (target != null)
            {
                EditorUtility.SetDirty(target);
            }
            SaveAssetDatabase();
            Repaint();
        }

        private static void SaveAssetDatabase()
        {
            AssetDatabase.SaveAssets();
        }

        private static string GetSettingDisplayName(AssetsSetting setting)
        {
            if (setting is AssetsLoadSetting assetsLoadSetting)
            {
                return string.IsNullOrEmpty(assetsLoadSetting.BuildName) ? assetsLoadSetting.name : assetsLoadSetting.BuildName;
            }

            if (setting is AssetsOriginSetting assetsOriginSetting)
            {
                return string.IsNullOrEmpty(assetsOriginSetting.BuildName) ? assetsOriginSetting.name : assetsOriginSetting.BuildName;
            }

            return setting == null ? "Null" : setting.name;
        }

        private static string PickAssetFolder(string currentAssetPath)
        {
            string defaultPath = Application.dataPath;
            if (!string.IsNullOrEmpty(currentAssetPath) && currentAssetPath.StartsWith("Assets", StringComparison.Ordinal))
            {
                defaultPath = AssetPathToFullPath(currentAssetPath);
            }

            string selectedFullPath = EditorUtility.OpenFolderPanel("选择 Assets 下的目录", defaultPath, "");
            if (string.IsNullOrEmpty(selectedFullPath))
            {
                return null;
            }

            string projectAssetsPath = NormalizePath(Application.dataPath);
            selectedFullPath = NormalizePath(selectedFullPath);
            if (!selectedFullPath.StartsWith(projectAssetsPath, StringComparison.OrdinalIgnoreCase))
            {
                EditorUtility.DisplayDialog("路径无效", "请选择当前工程 Assets 目录下的路径。", "确定");
                return null;
            }

            string relativePath = "Assets" + selectedFullPath.Substring(projectAssetsPath.Length);
            return NormalizePath(relativePath);
        }

        private static string AssetPathToFullPath(string assetPath)
        {
            string relativePath = assetPath.Substring("Assets".Length).TrimStart('/', '\\');
            return NormalizePath(Path.Combine(Application.dataPath, relativePath));
        }

        private static string GetNextAssetPath(string assetPathWithoutExtension, string extension)
        {
            int index = 0;
            while (true)
            {
                string assetPath = assetPathWithoutExtension + "_" + index + "." + extension;
                if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath) == null)
                {
                    return assetPath;
                }

                index++;
            }
        }

        private static void EnsureAssetFolder(string assetPath)
        {
            string folderPath = Path.GetDirectoryName(assetPath);
            if (string.IsNullOrEmpty(folderPath))
            {
                return;
            }

            string fullFolderPath = AssetPathToFullPath(folderPath);
            if (!Directory.Exists(fullFolderPath))
            {
                Directory.CreateDirectory(fullFolderPath);
                AssetDatabase.Refresh();
            }
        }

        private static string NormalizePath(string path)
        {
            return path.Replace('\\', '/');
        }
    }
}
