using System.IO;
using System.Text;
using ET;
using UnityEngine.Networking;

namespace BM
{
#if BMWebGL
    public static partial class AssetComponent
    {
        /// <summary>
        /// NintendoSwitch获取Bundle信息文件的路径
        /// </summary>
        internal static string BundleFileExistPath_WebGL(string bundlePackageName, string fileName)
        {
            string path = Path.Combine(AssetComponentConfig.LocalBundlePath, bundlePackageName, fileName);
            return path;
        }

        internal static async ETTask<string> LoadWebGLFileText(string filePath)
        {
            ETTask tcs = ETTask.Create();
            using (UnityWebRequest webRequest = UnityWebRequest.Get(filePath))
            {
                UnityWebRequestAsyncOperation weq = webRequest.SendWebRequest();
                weq.completed += (o) =>
                {
                    tcs.SetResult();
                };
                await tcs;
#if UNITY_2020_1_OR_NEWER
                if (webRequest.result == UnityWebRequest.Result.Success)
#else
                if (string.IsNullOrEmpty(webRequest.error))
#endif
                {
                    string str = webRequest.downloadHandler.text;
                    return str;
                }
                else
                {
                    AssetLogHelper.LogError("WebGL初始化分包未找到要读取的文件: \t" + filePath);
                    return "";
                }
                
            }
        }
        
        
    }
#endif
}
