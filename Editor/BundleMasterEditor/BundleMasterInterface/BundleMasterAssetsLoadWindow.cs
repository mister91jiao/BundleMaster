using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BM
{
    public partial class BundleMasterWindow
    {
        private void AssetsLoadSettingRender()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("--- <选中的分包配置信息> --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------", GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("当前选择的分包配置信息文件: ", GUILayout.ExpandWidth(false));
            AssetsLoadSetting selectAssetsLoadSetting = _selectAssetsSetting as AssetsLoadSetting;
            selectAssetsLoadSetting = (AssetsLoadSetting)EditorGUILayout.ObjectField(selectAssetsLoadSetting, typeof(AssetsLoadSetting), true, GUILayout.Width(_w / 3),GUILayout.ExpandHeight(false));
            bool noLoadSetting = selectAssetsLoadSetting == null;
            EditorGUI.BeginDisabledGroup(noLoadSetting);
            GUILayout.Label("分包名: ", GUILayout.Width(_w / 20), GUILayout.ExpandWidth(false));
            string buildName = "";
            if (!noLoadSetting)
            {
                buildName = selectAssetsLoadSetting.BuildName;
            }
            buildName = EditorGUILayout.TextField(buildName, GUILayout.Width(_w / 8), GUILayout.ExpandWidth(false));
            if (!noLoadSetting)
            {
                if (!string.Equals(selectAssetsLoadSetting.BuildName, buildName, StringComparison.Ordinal))
                {
                    selectAssetsLoadSetting.BuildName = buildName;
                    needFlush = true;
                }
            }
            GUILayout.Label("版本索引: ", GUILayout.Width(_w / 17), GUILayout.ExpandWidth(false));
            int buildIndex = 0;
            if (!noLoadSetting)
            {
                buildIndex = selectAssetsLoadSetting.BuildIndex;
            }
            buildIndex = EditorGUILayout.IntField(buildIndex, GUILayout.Width(_w / 20), GUILayout.ExpandWidth(false));
            if (!noLoadSetting)
            {
                if (buildIndex != selectAssetsLoadSetting.BuildIndex)
                {
                    selectAssetsLoadSetting.BuildIndex = buildIndex;
                    needFlush = true;
                }
            }
            GUI.color = new Color(0.9921569F, 0.2745098F, 0.282353F);
            if (GUILayout.Button("删除当前选择的分包配置"))
            {
                _assetLoadTable.AssetsSettings.Remove(selectAssetsLoadSetting);
                AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(selectAssetsLoadSetting));
                _viewSub = false;
                needFlush = true;
            }
            GUI.color = Color.white;
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("AssetBundle后缀: ", GUILayout.Width(_w / 9), GUILayout.ExpandWidth(false));
            string bundleVariant = "";
            if (!noLoadSetting)
            {
                bundleVariant = selectAssetsLoadSetting.BundleVariant;
            }
            bundleVariant = EditorGUILayout.TextField(bundleVariant, GUILayout.Width(_w / 10), GUILayout.ExpandWidth(false));
            if (!noLoadSetting)
            {
                if (!string.Equals(selectAssetsLoadSetting.BundleVariant, bundleVariant, StringComparison.Ordinal))
                {
                    selectAssetsLoadSetting.BundleVariant = bundleVariant;
                    needFlush = true;
                }
            }
            GUILayout.Label("是否启用Hash名:", GUILayout.Width(_w / 10), GUILayout.ExpandWidth(false));
            bool nameByHash = false;
            if (!noLoadSetting)
            {
                nameByHash = selectAssetsLoadSetting.NameByHash;
            }
            bool nameByHashChange = EditorGUILayout.Toggle(nameByHash, GUILayout.Width(_w / 80), GUILayout.ExpandWidth(false));
            if (!noLoadSetting)
            {
                if (nameByHash != nameByHashChange)
                {
                    selectAssetsLoadSetting.NameByHash = nameByHashChange;
                    needFlush = true;
                }
            }
            GUILayout.Label("构建选项", GUILayout.Width(_w / 17), GUILayout.ExpandWidth(false));
            BuildAssetBundleOptions buildAssetBundleOptions = BuildAssetBundleOptions.None;
            if (!noLoadSetting)
            {
                buildAssetBundleOptions = selectAssetsLoadSetting.BuildAssetBundleOptions;
            }
            BuildAssetBundleOptions buildAssetBundleOptionsChange = (BuildAssetBundleOptions)EditorGUILayout.EnumFlagsField(buildAssetBundleOptions, GUILayout.Width(_w / 5), GUILayout.ExpandWidth(false));
            if (!noLoadSetting)
            {
                if (buildAssetBundleOptions != buildAssetBundleOptionsChange)
                {
                    selectAssetsLoadSetting.BuildAssetBundleOptions = buildAssetBundleOptionsChange;
                    needFlush = true;
                }
            }
            GUILayout.Label("生成加密资源: ", GUILayout.Width(_w / 12), GUILayout.ExpandWidth(false));
            bool encryptAssets = false;
            if (!noLoadSetting)
            {
                encryptAssets = selectAssetsLoadSetting.EncryptAssets;
            }
            bool encryptAssetsChange = EditorGUILayout.Toggle(encryptAssets, GUILayout.Width(_w / 80), GUILayout.ExpandWidth(false));
            if (!noLoadSetting)
            {
                if (encryptAssets != encryptAssetsChange)
                {
                    selectAssetsLoadSetting.EncryptAssets = encryptAssetsChange;
                    needFlush = true;
                }
            }
            GUILayout.Label("加密密钥: ", GUILayout.Width(_w / 16), GUILayout.ExpandWidth(false));
            string secretKey = "";
            if (!noLoadSetting)
            {
                secretKey = selectAssetsLoadSetting.SecretKey;
            }
            secretKey = EditorGUILayout.TextField(secretKey, GUILayout.Width(_w / 6), GUILayout.ExpandWidth(false));
            if (!noLoadSetting)
            {
                if (!string.Equals(selectAssetsLoadSetting.SecretKey, secretKey, StringComparison.Ordinal))
                {
                    selectAssetsLoadSetting.SecretKey = secretKey;
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
                        bool enable = selectAssetsLoadSetting.Scene.Contains(sceneAsset);
                        bool enableChange = EditorGUILayout.Toggle(enable);
                        if (enable != enableChange)
                        {
                            if (enableChange)
                            {
                                selectAssetsLoadSetting.Scene.Add(sceneAsset);
                            }
                            else
                            {
                                selectAssetsLoadSetting.Scene.Remove(sceneAsset);
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
                List<string> assetPaths = selectAssetsLoadSetting.AssetPath;
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
                    selectAssetsLoadSetting.AssetPath = changeAssetPath;
                    needFlush = true;
                }
                EditorGUILayout.EndScrollView();
                GUILayout.EndHorizontal();
            }
            GUILayout.BeginHorizontal();
            GUI.color = new Color(0.654902F, 0.9921569F, 0.2784314F);
            if (GUILayout.Button("添加一个新路径", GUILayout.Height(_h / 20), GUILayout.ExpandWidth(true)))
            {
                selectAssetsLoadSetting.AssetPath.Add(null);
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
        
        private void Flush()
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
}