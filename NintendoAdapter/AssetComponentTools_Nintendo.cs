using System.IO;
using System.Text;
using ET;

namespace BM
{
#if Nintendo_Switch
    public static partial class AssetComponent
    {
        /// <summary>
        /// NintendoSwitch获取Bundle信息文件的路径
        /// </summary>
        internal static string BundleFileExistPath_Nintendo(string bundlePackageName, string fileName)
        {
            string path = Path.Combine(AssetComponentConfig.LocalBundlePath, bundlePackageName, fileName);
            //string path = "rom:/Data/StreamingAssets/" + bundlePackageName + "/" + fileName;
            return path;
        }

        internal static async ETTask<string> LoadNintendoFileText(string filePath)
        {
            nn.fs.FileHandle fileHandle = new nn.fs.FileHandle();
            nn.fs.EntryType entryType = nn.fs.EntryType.File;
            
            nn.Result result = nn.fs.FileSystem.GetEntryType(ref entryType, filePath);
            if (nn.fs.FileSystem.ResultPathNotFound.Includes(result))
            {
                AssetLogHelper.LogError("初始化分包未找到要读取的文件: \t" + filePath);
                result.abortUnlessSuccess();
                return "";
            }
            result.abortUnlessSuccess();
            
            
            result = nn.fs.File.Open(ref fileHandle, filePath, nn.fs.OpenFileMode.Read);
            result.abortUnlessSuccess();
            
            long fileSize = 0;
            result = nn.fs.File.GetSize(ref fileSize, fileHandle);
            result.abortUnlessSuccess();
            
            byte[] data = new byte[fileSize];
            result = nn.fs.File.Read(fileHandle, 0, data, fileSize);
            result.abortUnlessSuccess();
            
            nn.fs.File.Close(fileHandle);
            
            MemoryStream memoryStream = new MemoryStream(data);
            StreamReader streamReader = new StreamReader(memoryStream);
            
            //异步读取内容
            string str = await streamReader.ReadToEndAsync();
            
            //关闭引用
            streamReader.Close();
            streamReader.Dispose();
            memoryStream.Close();
            await memoryStream.DisposeAsync();
            
            return str;
        }
        
        
    }
#endif
}
