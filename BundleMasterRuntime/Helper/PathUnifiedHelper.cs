namespace BM
{
    public static class PathUnifiedHelper
    {
        /// <summary>
        /// 解决路径资源是否存在的判定问题
        /// </summary>
        public static string UnifiedPath(string path)
        {
            
#if UNITY_ANDROID && !UNITY_EDITOR
            path = path.Replace("\\", "/");
#elif UNITY_IOS && !UNITY_EDITOR
            path = path.Replace("\\", "/");
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
                
#elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            
#endif
            return path;
        }
        
        
    }
}