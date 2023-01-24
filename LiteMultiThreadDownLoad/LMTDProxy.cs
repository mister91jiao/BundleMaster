using System;
using System.Net;

namespace LMTD
{
    public class LMTDProxy
    {
        private static readonly object LockObj = new object();
        private static WebProxy _proxy = null;
        
        public static WebProxy GetProxy()
        {
            lock (LockObj)
            {
                _proxy = new WebProxy();
                return _proxy;
            }
        }
        
    }
}