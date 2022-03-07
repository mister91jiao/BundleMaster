using System.Collections.Generic;

namespace BM
{
    internal static class LoadHandlerFactory
    {
        private static Queue<LoadHandler> _loadHandlerPool = new Queue<LoadHandler>();

        internal static LoadHandler GetLoadHandler(string assetPath, string bundlePackageName)
        {
            LoadHandler loadHandler;
            if (_loadHandlerPool.Count > 0)
            {
                loadHandler = _loadHandlerPool.Dequeue();
            }
            else
            {
                loadHandler = new LoadHandler();
            }
            loadHandler.Init(assetPath, bundlePackageName);
            return loadHandler;
        }
        
        internal static void EnterPool(LoadHandler loadHandler)
        {
            _loadHandlerPool.Enqueue(loadHandler);
        }
    }
}