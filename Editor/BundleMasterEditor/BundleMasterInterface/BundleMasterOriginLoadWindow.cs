using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BM
{
    public partial class BundleMasterWindow
    {
        private void OriginLoadSettingRender()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("--- <选中的原生资源配置信息> --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------", GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("当前选择的分包配置信息文件: ", GUILayout.ExpandWidth(false));
            AssetsOriginSetting selectAssetsOriginSetting = _selectAssetsSetting as AssetsOriginSetting;
            selectAssetsOriginSetting = (AssetsOriginSetting)EditorGUILayout.ObjectField(selectAssetsOriginSetting, typeof(AssetsOriginSetting), true, GUILayout.Width(_w / 3),GUILayout.ExpandHeight(false));
            bool noLoadSetting = selectAssetsOriginSetting == null;
            EditorGUI.BeginDisabledGroup(noLoadSetting);
            GUILayout.Label("分包名: ", GUILayout.Width(_w / 20), GUILayout.ExpandWidth(false));
            string buildName = "";
            if (!noLoadSetting)
            {
                buildName = selectAssetsOriginSetting.BuildName;
            }
            buildName = EditorGUILayout.TextField(buildName, GUILayout.Width(_w / 8), GUILayout.ExpandWidth(false));
            if (!noLoadSetting)
            {
                if (!string.Equals(selectAssetsOriginSetting.BuildName, buildName, StringComparison.Ordinal))
                {
                    selectAssetsOriginSetting.BuildName = buildName;
                    needFlush = true;
                }
            }
            GUILayout.Label("版本索引: ", GUILayout.Width(_w / 17), GUILayout.ExpandWidth(false));
            int buildIndex = 0;
            if (!noLoadSetting)
            {
                buildIndex = selectAssetsOriginSetting.BuildIndex;
            }
            buildIndex = EditorGUILayout.IntField(buildIndex, GUILayout.Width(_w / 20), GUILayout.ExpandWidth(false));
            if (!noLoadSetting)
            {
                if (buildIndex != selectAssetsOriginSetting.BuildIndex)
                {
                    selectAssetsOriginSetting.BuildIndex = buildIndex;
                    needFlush = true;
                }
            }
            GUI.color = new Color(0.9921569F, 0.2745098F, 0.282353F);
            if (GUILayout.Button("删除当前选择的分包配置"))
            {
                _assetLoadTable.AssetsSettings.Remove(selectAssetsOriginSetting);
                AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(selectAssetsOriginSetting));
                _viewSub = false;
                needFlush = true;
            }
            GUI.color = Color.white;
            GUILayout.EndHorizontal();
            
            GUILayout.Space(_h / 15);
            
            GUILayout.BeginHorizontal();
            string originAssetPath = selectAssetsOriginSetting.OriginFilePath;
            originAssetPath = EditorGUILayout.TextField(originAssetPath);
            if (!string.Equals(selectAssetsOriginSetting.OriginFilePath, originAssetPath, StringComparison.Ordinal))
            {
                selectAssetsOriginSetting.OriginFilePath = originAssetPath;
                needFlush = true;
            }
            GUILayout.EndHorizontal();
            
            GUILayout.Space(_h / 8);
            
            GUILayout.BeginHorizontal();
            GUI.color = new Color(0.9921569F, 0.7960784F, 0.509804F);
            if (GUILayout.Button("分包编辑完成", GUILayout.Height(_h / 10)))
            {
                _viewSub = false;
            }
            GUI.color = Color.white;
            GUILayout.EndHorizontal();
        }
    }
}