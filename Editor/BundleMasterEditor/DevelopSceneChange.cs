using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace BM
{
    public static class DevelopSceneChange
    {
        /// <summary>
        /// 用于检测在Develop模式下将场景加入BuildSettings
        /// </summary>
        public static void CheckSceneChange(AssetLoadMode assetLoadMode) 
        {
            AssetLoadTable assetLoadTable = AssetDatabase.LoadAssetAtPath<AssetLoadTable>(BundleMasterWindow.AssetLoadTablePath);
            List<AssetsLoadSetting> assetsLoadSettings = new List<AssetsLoadSetting>();
            foreach (AssetsSetting assetsSetting in assetLoadTable.AssetsSettings)
            {
                if (assetsSetting is AssetsLoadSetting)
                {
                    assetsLoadSettings.Add(assetsSetting as AssetsLoadSetting);
                }
            }
            Dictionary<string, EditorBuildSettingsScene> editorBuildSettingsScenes = new Dictionary<string, EditorBuildSettingsScene>();
            for (int i = 0; i < assetLoadTable.InitScene.Count; i++)
            {
                string scenePath = AssetDatabase.GetAssetPath(assetLoadTable.InitScene[i]);
                if (!editorBuildSettingsScenes.ContainsKey(scenePath))
                {
                    editorBuildSettingsScenes.Add(scenePath, new EditorBuildSettingsScene(scenePath, true));
                }
            }
            if (assetLoadMode == AssetLoadMode.Develop)
            {
                foreach (AssetsLoadSetting assetsLoadSetting in assetsLoadSettings)
                {
                    if (assetsLoadSetting == null)
                    {
                        continue;
                    }
                    foreach (SceneAsset sceneAsset in assetsLoadSetting.Scene)
                    {
                        if (sceneAsset == null)
                        {
                            continue;
                        }
                        string scenePath = AssetDatabase.GetAssetPath(sceneAsset);
                        if (!editorBuildSettingsScenes.ContainsKey(scenePath))
                        {
                            editorBuildSettingsScenes.Add(scenePath, new EditorBuildSettingsScene(scenePath, true));
                        }
                    }
                }
            }
            EditorBuildSettings.scenes = editorBuildSettingsScenes.Values.ToArray();
        } 
    }
}