using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace BM
{
    [InitializeOnLoad]
    public class DevelopSceneChange
    {
        /// <summary>
        /// 每次脚本编译后执行, 用于检测在Develop模式下将场景加入BuildSettings, 如果不想每次编译后执行可以自己封装
        /// </summary>
        static DevelopSceneChange() 
        {
            AssetLoadTable assetLoadTable = AssetDatabase.LoadAssetAtPath<AssetLoadTable>(BuildAssets.AssetLoadTablePath);
            List<AssetsLoadSetting> assetsLoadSettings = assetLoadTable.AssetsLoadSettings;
            Dictionary<string, EditorBuildSettingsScene> editorBuildSettingsScenes = new Dictionary<string, EditorBuildSettingsScene>();
            for (int i = 0; i < assetLoadTable.InitScene.Count; i++)
            {
                string scenePath = AssetDatabase.GetAssetPath(assetLoadTable.InitScene[i]);
                if (!editorBuildSettingsScenes.ContainsKey(scenePath))
                {
                    editorBuildSettingsScenes.Add(scenePath, new EditorBuildSettingsScene(scenePath, true));
                }
            }
            if (AssetComponentConfig.AssetLoadMode == AssetLoadMode.Develop)
            {
                foreach (AssetsLoadSetting assetsLoadSetting in assetsLoadSettings)
                {
                    foreach (SceneAsset sceneAsset in assetsLoadSetting.Scene)
                    {
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