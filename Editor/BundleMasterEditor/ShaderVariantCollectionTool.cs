using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace BM
{
    /// <summary>
    /// 收集分包场景中材质实际使用到的 Shader 变体。
    /// </summary>
    public class ShaderVariantCollectionTool : EditorWindow
    {
        private readonly List<AssetsLoadSetting> _assetsLoadSettings = new List<AssetsLoadSetting>();
        private readonly List<string> _assetsLoadSettingNames = new List<string>();
        private Vector2 _sceneScrollPos = Vector2.zero;
        private int _selectIndex = 0;

        [MenuItem("Tools/BuildAsset/搜集分包内引用的Shader变体")]
        public static void ShaderVariantCollection()
        {
            ShaderVariantCollectionTool window = GetWindow<ShaderVariantCollectionTool>(true, "Shader变体收集工具", true);
            window.minSize = new Vector2(620, 420);
            window.RefreshAssetsLoadSettings();
            window.Show();
        }

        private void OnEnable()
        {
            RefreshAssetsLoadSettings();
        }

        private void OnGUI()
        {
            GUILayout.Label("选择分包", EditorStyles.boldLabel);
            GUILayout.Space(4);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("刷新分包列表", GUILayout.Width(120)))
            {
                RefreshAssetsLoadSettings();
            }
            EditorGUI.BeginDisabledGroup(_assetsLoadSettings.Count == 0);
            _selectIndex = EditorGUILayout.Popup(_selectIndex, _assetsLoadSettingNames.ToArray());
            EditorGUI.EndDisabledGroup();
            GUILayout.EndHorizontal();

            if (_assetsLoadSettings.Count == 0)
            {
                EditorGUILayout.HelpBox("没有找到 AssetsLoadSetting 分包配置。", MessageType.Warning);
                return;
            }

            _selectIndex = Mathf.Clamp(_selectIndex, 0, _assetsLoadSettings.Count - 1);
            AssetsLoadSetting assetsLoadSetting = _assetsLoadSettings[_selectIndex];

            GUILayout.Space(8);
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField("当前分包配置", assetsLoadSetting, typeof(AssetsLoadSetting), false);
            EditorGUILayout.TextField("BuildName", assetsLoadSetting.BuildName);
            EditorGUI.EndDisabledGroup();

            GUILayout.Space(8);
            GUILayout.Label("此分包配置引用的场景", EditorStyles.boldLabel);
            _sceneScrollPos = EditorGUILayout.BeginScrollView(_sceneScrollPos, GUILayout.Height(180));
            if (assetsLoadSetting.Scene == null || assetsLoadSetting.Scene.Count == 0)
            {
                EditorGUILayout.HelpBox("此分包没有配置场景，无法从场景依赖中收集 Shader 变体。", MessageType.Info);
            }
            else
            {
                for (int i = 0; i < assetsLoadSetting.Scene.Count; i++)
                {
                    SceneAsset sceneAsset = assetsLoadSetting.Scene[i];
                    string scenePath = sceneAsset == null ? "空场景引用" : AssetDatabase.GetAssetPath(sceneAsset);
                    EditorGUILayout.LabelField((i + 1).ToString(), scenePath);
                }
            }
            EditorGUILayout.EndScrollView();

            GUILayout.Space(8);
            EditorGUILayout.HelpBox("生成内容基于场景依赖到的材质：每个材质当前启用的 Shader Keywords 会被写入 .shadervariants。", MessageType.None);

            EditorGUI.BeginDisabledGroup(assetsLoadSetting.Scene == null || assetsLoadSetting.Scene.Count == 0);
            if (GUILayout.Button("生成 .shadervariants 文件", GUILayout.Height(36)))
            {
                GenerateShaderVariantCollection(assetsLoadSetting);
            }
            EditorGUI.EndDisabledGroup();
        }

        private void RefreshAssetsLoadSettings()
        {
            _assetsLoadSettings.Clear();
            _assetsLoadSettingNames.Clear();

            HashSet<AssetsLoadSetting> addedSettings = new HashSet<AssetsLoadSetting>();
            AssetLoadTable assetLoadTable = AssetDatabase.LoadAssetAtPath<AssetLoadTable>(BundleMasterAssetsPath.AssetLoadTablePath);
            if (assetLoadTable != null)
            {
                foreach (AssetsSetting assetsSetting in assetLoadTable.AssetsSettings)
                {
                    if (assetsSetting is AssetsLoadSetting assetsLoadSetting)
                    {
                        AddAssetsLoadSetting(assetsLoadSetting, addedSettings, true);
                    }
                }
            }

            foreach (string guid in AssetDatabase.FindAssets($"t:{nameof(AssetsLoadSetting)}"))
            {
                AssetsLoadSetting assetsLoadSetting = AssetDatabase.LoadAssetAtPath<AssetsLoadSetting>(AssetDatabase.GUIDToAssetPath(guid));
                AddAssetsLoadSetting(assetsLoadSetting, addedSettings, false);
            }

            _selectIndex = Mathf.Clamp(_selectIndex, 0, Mathf.Max(0, _assetsLoadSettings.Count - 1));
        }

        private void AddAssetsLoadSetting(AssetsLoadSetting assetsLoadSetting, HashSet<AssetsLoadSetting> addedSettings, bool enabledInTable)
        {
            if (assetsLoadSetting == null || addedSettings.Contains(assetsLoadSetting))
            {
                return;
            }

            addedSettings.Add(assetsLoadSetting);
            _assetsLoadSettings.Add(assetsLoadSetting);

            string buildName = string.IsNullOrEmpty(assetsLoadSetting.BuildName) ? assetsLoadSetting.name : assetsLoadSetting.BuildName;
            string label = enabledInTable ? buildName : buildName + "（未启用）";
            _assetsLoadSettingNames.Add(label);
        }

        private static void GenerateShaderVariantCollection(AssetsLoadSetting assetsLoadSetting)
        {
            if (assetsLoadSetting == null)
            {
                EditorUtility.DisplayDialog("生成失败", "没有选择分包配置。", "确定");
                return;
            }

            string savePath = EditorUtility.SaveFilePanelInProject(
                "保存 Shader Variant Collection",
                assetsLoadSetting.BuildName + "_SceneShaderVariants",
                "shadervariants",
                "请选择 .shadervariants 文件保存位置。");

            if (string.IsNullOrEmpty(savePath))
            {
                return;
            }

            bool assetExists = AssetDatabase.LoadAssetAtPath<Object>(savePath) != null;
            if (assetExists && !EditorUtility.DisplayDialog("覆盖确认", "目标文件已存在，是否覆盖？\n" + savePath, "覆盖", "取消"))
            {
                return;
            }

            Dictionary<string, Material> materials = CollectSceneMaterials(assetsLoadSetting);
            if (materials.Count == 0)
            {
                EditorUtility.DisplayDialog("生成失败", "所选分包场景没有收集到材质。", "确定");
                return;
            }

            UnityEngine.ShaderVariantCollection collection = new UnityEngine.ShaderVariantCollection();
            BuildVariantCollection(collection, materials.Values, out int shaderCount, out int addCount, out int failCount);

            if (assetExists)
            {
                AssetDatabase.DeleteAsset(savePath);
            }

            AssetDatabase.CreateAsset(collection, savePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            bool savePathInBundle = IsPathInBundleAssetPath(savePath, assetsLoadSetting);
            string message =
                "Shader 变体收集完成\n" +
                "分包：" + assetsLoadSetting.BuildName + "\n" +
                "场景数量：" + CountValidScenes(assetsLoadSetting) + "\n" +
                "材质数量：" + materials.Count + "\n" +
                "Shader 数量：" + shaderCount + "\n" +
                "写入变体数量：" + addCount + "\n" +
                "写入失败数量：" + failCount + "\n" +
                "保存路径：" + savePath;

            if (!savePathInBundle)
            {
                message += "\n注意：保存位置不在当前分包 AssetPath 下，后续打包时可能不会进入此分包的 shader bundle。";
            }

            Debug.Log(message);
            EditorUtility.DisplayDialog("生成完成", message, "确定");
        }

        private static Dictionary<string, Material> CollectSceneMaterials(AssetsLoadSetting assetsLoadSetting)
        {
            Dictionary<string, Material> materials = new Dictionary<string, Material>();
            HashSet<string> dependencyPaths = new HashSet<string>();

            try
            {
                for (int i = 0; i < assetsLoadSetting.Scene.Count; i++)
                {
                    SceneAsset sceneAsset = assetsLoadSetting.Scene[i];
                    if (sceneAsset == null)
                    {
                        continue;
                    }

                    string scenePath = AssetDatabase.GetAssetPath(sceneAsset);
                    if (string.IsNullOrEmpty(scenePath))
                    {
                        continue;
                    }

                    EditorUtility.DisplayProgressBar("收集 Shader 变体", "分析场景依赖：" + scenePath, (float)i / assetsLoadSetting.Scene.Count);
                    string[] dependencies = AssetDatabase.GetDependencies(scenePath, true);
                    foreach (string dependencyPath in dependencies)
                    {
                        dependencyPaths.Add(dependencyPath);
                    }
                }

                int index = 0;
                foreach (string dependencyPath in dependencyPaths)
                {
                    EditorUtility.DisplayProgressBar("收集 Shader 变体", "收集材质：" + dependencyPath, (float)index / Mathf.Max(1, dependencyPaths.Count));
                    CollectMaterialsFromPath(dependencyPath, materials);
                    index++;
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            return materials;
        }

        private static void CollectMaterialsFromPath(string assetPath, Dictionary<string, Material> materials)
        {
            Material mainMaterial = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            AddMaterial(mainMaterial, materials);

            string extension = Path.GetExtension(assetPath);
            if (!string.Equals(extension, ".fbx", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(extension, ".asset", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            foreach (Object subAsset in subAssets)
            {
                AddMaterial(subAsset as Material, materials);
            }
        }

        private static void AddMaterial(Material material, Dictionary<string, Material> materials)
        {
            if (material == null || material.shader == null)
            {
                return;
            }

            string materialPath = AssetDatabase.GetAssetPath(material);
            string key = string.IsNullOrEmpty(materialPath) ? material.GetInstanceID().ToString() : materialPath + "#" + material.name;
            if (!materials.ContainsKey(key))
            {
                materials.Add(key, material);
            }
        }

        private static void BuildVariantCollection(
            UnityEngine.ShaderVariantCollection collection,
            IEnumerable<Material> materials,
            out int shaderCount,
            out int addCount,
            out int failCount)
        {
            HashSet<Shader> shaders = new HashSet<Shader>();
            HashSet<string> variantKeys = new HashSet<string>();
            Dictionary<Shader, PassType[]> shaderPassTypeCache = new Dictionary<Shader, PassType[]>();
            addCount = 0;
            failCount = 0;

            foreach (Material material in materials)
            {
                Shader shader = material.shader;
                if (shader == null)
                {
                    continue;
                }

                shaders.Add(shader);
                string[] keywords = GetValidMaterialKeywords(material);
                if (!shaderPassTypeCache.TryGetValue(shader, out PassType[] passTypes))
                {
                    passTypes = GetShaderPassTypes(shader);
                    shaderPassTypeCache.Add(shader, passTypes);
                }

                foreach (PassType passType in passTypes)
                {
                    string variantKey = BuildVariantKey(shader, passType, keywords);
                    if (!variantKeys.Add(variantKey))
                    {
                        continue;
                    }

                    try
                    {
                        UnityEngine.ShaderVariantCollection.ShaderVariant variant =
                            new UnityEngine.ShaderVariantCollection.ShaderVariant(shader, passType, keywords);

                        if (collection.Add(variant))
                        {
                            addCount++;
                        }
                    }
                    catch (Exception exception)
                    {
                        failCount++;
                        Debug.LogWarning("Shader 变体写入失败：" + shader.name + " / " + passType + "\n" + exception.Message);
                    }
                }
            }

            shaderCount = shaders.Count;
        }

        private static string[] GetValidMaterialKeywords(Material material)
        {
            string[] sourceKeywords = material.shaderKeywords;
            if (sourceKeywords == null || sourceKeywords.Length == 0)
            {
                return Array.Empty<string>();
            }

            List<string> keywords = new List<string>();
            Shader shader = material.shader;
            foreach (string keyword in sourceKeywords)
            {
                if (string.IsNullOrEmpty(keyword))
                {
                    continue;
                }

                if (!IsKeywordDeclared(shader, keyword))
                {
                    continue;
                }

                keywords.Add(keyword);
            }

            keywords.Sort(StringComparer.Ordinal);
            return keywords.ToArray();
        }

        private static bool IsKeywordDeclared(Shader shader, string keyword)
        {
#if UNITY_2021_2_OR_NEWER
            return shader.keywordSpace.FindKeyword(keyword).isValid;
#else
            return true;
#endif
        }

        private static PassType[] GetShaderPassTypes(Shader shader)
        {
            HashSet<PassType> passTypes = new HashSet<PassType>();
            if (!TryGetShaderPassTypesByShaderUtil(shader, passTypes) || passTypes.Count == 0)
            {
                foreach (PassType passType in GetCandidatePassTypes())
                {
                    passTypes.Add(passType);
                }
            }

            PassType[] result = new PassType[passTypes.Count];
            passTypes.CopyTo(result);
            return result;
        }

        private static bool TryGetShaderPassTypesByShaderUtil(Shader shader, HashSet<PassType> passTypes)
        {
            Type shaderUtilType = typeof(Editor).Assembly.GetType("UnityEditor.ShaderUtil");
            if (shaderUtilType == null)
            {
                return false;
            }

            System.Reflection.BindingFlags bindingFlags =
                System.Reflection.BindingFlags.Static |
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic;

            System.Reflection.MethodInfo getPassCountMethod =
                shaderUtilType.GetMethod("GetPassCount", bindingFlags, null, new[] { typeof(Shader) }, null) ??
                shaderUtilType.GetMethod("GetShaderPassCount", bindingFlags, null, new[] { typeof(Shader) }, null);

            System.Reflection.MethodInfo getPassTypeMethod =
                shaderUtilType.GetMethod("GetPassType", bindingFlags, null, new[] { typeof(Shader), typeof(int) }, null) ??
                shaderUtilType.GetMethod("GetShaderPassType", bindingFlags, null, new[] { typeof(Shader), typeof(int) }, null);

            if (getPassCountMethod == null || getPassTypeMethod == null)
            {
                return false;
            }

            try
            {
                int passCount = Convert.ToInt32(getPassCountMethod.Invoke(null, new object[] { shader }));
                for (int i = 0; i < passCount; i++)
                {
                    object passTypeValue = getPassTypeMethod.Invoke(null, new object[] { shader, i });
                    AddPassType(passTypeValue, passTypes);
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning("读取 Shader Pass 信息失败，将使用默认 PassType 集合：" + shader.name + "\n" + exception.Message);
                return false;
            }

            return passTypes.Count > 0;
        }

        private static void AddPassType(object passTypeValue, HashSet<PassType> passTypes)
        {
            if (passTypeValue is PassType passType)
            {
                passTypes.Add(passType);
                return;
            }

            if (passTypeValue is int passTypeIndex && Enum.IsDefined(typeof(PassType), passTypeIndex))
            {
                passTypes.Add((PassType)passTypeIndex);
                return;
            }

            if (passTypeValue is string passTypeName && Enum.TryParse(passTypeName, out PassType parsedPassType))
            {
                passTypes.Add(parsedPassType);
            }
        }

        private static PassType[] GetCandidatePassTypes()
        {
            List<PassType> passTypes = new List<PassType>();
            foreach (PassType passType in (PassType[])Enum.GetValues(typeof(PassType)))
            {
                if (string.Equals(passType.ToString(), "GrabPass", StringComparison.Ordinal))
                {
                    continue;
                }

                passTypes.Add(passType);
            }

            return passTypes.ToArray();
        }

        private static string BuildVariantKey(Shader shader, PassType passType, string[] keywords)
        {
            return AssetDatabase.GetAssetPath(shader) + "|" + shader.name + "|" + passType + "|" + string.Join(" ", keywords);
        }

        private static bool IsPathInBundleAssetPath(string assetPath, AssetsLoadSetting assetsLoadSetting)
        {
            if (assetsLoadSetting.AssetPath == null)
            {
                return false;
            }

            string normalizeAssetPath = NormalizeAssetPath(assetPath);
            foreach (string bundleAssetPath in assetsLoadSetting.AssetPath)
            {
                if (string.IsNullOrEmpty(bundleAssetPath))
                {
                    continue;
                }

                string normalizeBundleAssetPath = NormalizeAssetPath(bundleAssetPath).TrimEnd('/');
                if (string.Equals(normalizeAssetPath, normalizeBundleAssetPath, StringComparison.OrdinalIgnoreCase) ||
                    normalizeAssetPath.StartsWith(normalizeBundleAssetPath + "/", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static string NormalizeAssetPath(string assetPath)
        {
            return assetPath.Replace('\\', '/');
        }

        private static int CountValidScenes(AssetsLoadSetting assetsLoadSetting)
        {
            int count = 0;
            foreach (SceneAsset sceneAsset in assetsLoadSetting.Scene)
            {
                if (sceneAsset != null)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
