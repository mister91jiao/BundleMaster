using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using ET;
using UnityEngine.Networking;

namespace BM
{
    public static partial class AssetComponent
    {
#if BMWebGL
        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="secretKey">WebGL模式下 secretKey 参数失效，仅作占位使用</param>
        public static async ETTask<bool> Initialize(string bundlePackageName, string secretKey = null)
        {
            if (AssetComponentConfig.AssetLoadMode == AssetLoadMode.Develop)
            {
                AssetLogHelper.Log("AssetLoadMode = Develop 不需要初始化Bundle配置文件");
                return false;
            }
            if (BundleNameToRuntimeInfo.ContainsKey(bundlePackageName))
            {
                AssetLogHelper.LogError(bundlePackageName + " 重复初始化");
                return false;
            }
            BundleRuntimeInfo bundleRuntimeInfo = new BundleRuntimeInfo(bundlePackageName);
            BundleNameToRuntimeInfo.Add(bundlePackageName, bundleRuntimeInfo);

            string filePath = BundleFileExistPath_WebGL(bundlePackageName, "FileLogs.txt");
            string fileLogs = await LoadWebGLFileText(filePath);
            {
                Regex reg = new Regex(@"\<(.+?)>");
                MatchCollection matchCollection = reg.Matches(fileLogs);
                List<string> dependFileName = new List<string>();
                foreach (Match m in matchCollection)
                {
                    string[] fileLog = m.Groups[1].Value.Split('|');
                    LoadFile loadFile = new LoadFile();
                    loadFile.FilePath = fileLog[0];
                    loadFile.AssetBundleName = fileLog[1];
                    
                    if (fileLog.Length > 2)
                    {
                        for (int i = 2; i < fileLog.Length; i++)
                        {
                            dependFileName.Add(fileLog[i]);
                        }
                    }
                    loadFile.DependFileName = dependFileName.ToArray();
                    dependFileName.Clear();
                    bundleRuntimeInfo.LoadFileDic.Add(loadFile.FilePath, loadFile);
                }
            }
            
            string dependPath = BundleFileExistPath_WebGL(bundlePackageName, "DependLogs.txt");
            string dependLogs = await LoadWebGLFileText(dependPath);
            {
                Regex reg = new Regex(@"\<(.+?)>");
                MatchCollection matchCollection = reg.Matches(dependLogs);
                foreach (Match m in matchCollection)
                {
                    string[] dependLog = m.Groups[1].Value.Split('|');
                    LoadDepend loadDepend = new LoadDepend();
                    loadDepend.FilePath = dependLog[0];
                    loadDepend.AssetBundleName = dependLog[1];
                    bundleRuntimeInfo.LoadDependDic.Add(loadDepend.FilePath, loadDepend);
                }
            }
            
            ETTask groupTcs = ETTask.Create();
            string groupPath = BundleFileExistPath_WebGL(bundlePackageName, "GroupLogs.txt");
            string groupLogs = await LoadWebGLFileText(groupPath);
            {
                Regex reg = new Regex(@"\<(.+?)>");
                MatchCollection matchCollection = reg.Matches(groupLogs);
                foreach (Match m in matchCollection)
                {
                    string[] groupLog = m.Groups[1].Value.Split('|');
                    LoadGroup loadGroup = new LoadGroup();
                    loadGroup.FilePath = groupLog[0];
                    loadGroup.AssetBundleName = groupLog[1];
                    if (groupLog.Length > 2)
                    {
                        for (int i = 2; i < groupLog.Length; i++)
                        {
                            loadGroup.DependFileName.Add(groupLog[i]);
                        }
                    }
                    bundleRuntimeInfo.LoadGroupDic.Add(loadGroup.FilePath, loadGroup);
                    bundleRuntimeInfo.LoadGroupDicKey.Add(loadGroup.FilePath);
                }
            }
            
            //加载当前分包的shader
            await LoadShader_WebGL(bundlePackageName);
            return true;
        }

        private static async ETTask LoadShader_WebGL(string bundlePackageName)
        {
            ETTask tcs = ETTask.Create();
            string shaderPath = BundleFileExistPath_WebGL(bundlePackageName, "shader_" + bundlePackageName.ToLower());
            //判断文件是否存在
            using (UnityWebRequest webRequest = UnityWebRequestAssetBundle.GetAssetBundle(shaderPath))
            {
                UnityWebRequestAsyncOperation weq = webRequest.SendWebRequest();
                weq.completed += (o) => { tcs.SetResult(); };
                await tcs;
#if UNITY_2020_1_OR_NEWER
                if (webRequest.result == UnityWebRequest.Result.Success)
#else
                if (string.IsNullOrEmpty(webRequest.error))
#endif
                {
                    AssetBundle assetBundle = DownloadHandlerAssetBundle.GetContent(webRequest);
                    BundleNameToRuntimeInfo[bundlePackageName].Shader = assetBundle;
                }
                else
                {
                    return;
                }
            }
        }
#endif
    }
}
