using UnityEngine.Networking;
using ET;

namespace BM
{
    public static class DownloadBundleHelper
    {
        public static async ETTask<byte[]> DownloadDataByUrl(string url)
        {
            for (int i = 0; i < AssetComponentConfig.MaxDownLoadCount; i++)
            {
                byte[] data = await DownloadData(url);
                if (data != null)
                {
                    return data;
                }
            }
            AssetLogHelper.LogError("下载资源失败: " + url);
            return null;
        }

        private static async ETTask<byte[]> DownloadData(string url)
        {
            using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
            {
                webRequest.timeout = 5;
                UnityWebRequestAsyncOperation webRequestAsync = webRequest.SendWebRequest();
                ETTask waitDown = ETTask.Create();
                webRequestAsync.completed += (asyncOperation) =>
                {
                    waitDown.SetResult();
                };
                await waitDown;
                if (webRequest.result != UnityWebRequest.Result.Success)
                {
                    AssetLogHelper.Log("下载Bundle失败 重试\n" + webRequest.error);
                    return null;
                }
                return webRequest.downloadHandler.data;
            }
        }
        
        
    }
}