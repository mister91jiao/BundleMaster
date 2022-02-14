using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using ET;

namespace BM
{
    public static partial class AssetComponent
    {
        /// <summary>
        /// Bundle初始化的信息
        /// </summary>
        public static readonly Dictionary<string, BundleRuntimeInfo> BundleNameToRuntimeInfo = new Dictionary<string, BundleRuntimeInfo>();
    
        /// <summary>
        /// 初始化
        /// </summary>
        public static async ETTask Initialize(string bundlePackageName)
        {
            if (AssetComponentConfig.AssetLoadMode == AssetLoadMode.Develop)
            {
                AssetLogHelper.Log("AssetLoadMode = Develop 不需要初始化Bundle配置文件");
                return;
            }
            if (BundleNameToRuntimeInfo.ContainsKey(bundlePackageName))
            {
                AssetLogHelper.LogError(bundlePackageName + " 重复初始化");
                return;
            }
            BundleRuntimeInfo bundleRuntimeInfo = new BundleRuntimeInfo();
            BundleNameToRuntimeInfo.Add(bundlePackageName, bundleRuntimeInfo);
            
            //判断Bundle信息文件是否存在
            string fileLogsPath = BundleFileExistPath(bundlePackageName, "FileLogs.txt");
            if (fileLogsPath == null)
            {
                AssetLogHelper.LogError("没有找到 " + bundlePackageName + " Bundle的FileLogs");
                return;
            }
            string dependLogsPath = BundleFileExistPath(bundlePackageName, "DependLogs.txt");
            if (dependLogsPath == null)
            {
                AssetLogHelper.LogError("没有找到 " + bundlePackageName + " Bundle的DependLogs");
                return;
            }
            using (StreamReader sr = new StreamReader(fileLogsPath))
            {
                string fileLogs = await sr.ReadToEndAsync();
                Regex reg = new Regex(@"\<(.+?)>");
                MatchCollection matchCollection = reg.Matches(fileLogs);
                foreach (Match m in matchCollection)
                {
                    string[] fileLog = m.Groups[1].Value.Split('|');
                    LoadFile loadFile = new LoadFile();
                    loadFile.FilePath = fileLog[0];
                    loadFile.AssetBundleName = fileLog[1];
                    List<string> dependFileName = new List<string>();
                    if (fileLog.Length > 2)
                    {
                        for (int i = 2; i < fileLog.Length; i++)
                        {
                            dependFileName.Add(fileLog[i]);
                        }
                    }
                    loadFile.DependFileName = dependFileName.ToArray();
                    bundleRuntimeInfo.LoadFileDic.Add(loadFile.FilePath, loadFile);
                }
            }
            using (StreamReader sr = new StreamReader(dependLogsPath))
            {
                string dependLogs = await sr.ReadToEndAsync();
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
            //加载当前分包的shader
            await LoadShader(bundlePackageName);
        }
        
        /// <summary>
        /// 加载Shader文件
        /// </summary>
        private static ETTask LoadShader(string bundlePackageName)
        {
            ETTask tcs = ETTask.Create();
            string shaderPath = BundleFileExistPath(bundlePackageName, "shader_" + bundlePackageName.ToLower());
            if (shaderPath != null)
            {
                AssetBundleCreateRequest request = AssetBundle.LoadFromFileAsync(shaderPath);
                request.completed += operation => tcs.SetResult();
            }
            else
            {
                tcs.SetResult();
            }
            return tcs;
        }
        
    }
}