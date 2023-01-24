using System;
using System.Collections.Generic;
using System.Threading;

namespace LMTD
{
    internal class LiteThread
    {
        private readonly Thread thread;
        private ILiteThreadAction _liteThreadAction = null;
        
        internal LiteThread()
        {
            ThreadFactory.ThreadCount++;
            thread = new Thread(Run);
            thread.Start();
        }

        internal void Action(ILiteThreadAction liteThreadAction)
        {
            this._liteThreadAction = liteThreadAction;
        }

        private void Run()
        {
            while (!ThreadFactory.RecoverKey)
            {
                Thread.Sleep(1);
                if (_liteThreadAction != null)
                {
                    _liteThreadAction.Logic();
                    _liteThreadAction = null;
                    lock (ThreadFactory.ThreadPool)
                    {
                        //执行完逻辑后自己进池
                        ThreadFactory.ThreadPool.Enqueue(this);
                    }
                }
                
            }
            Recovery();
        }

        /// <summary>
        /// 回收这个线程
        /// </summary>
        internal void Recovery()
        {
            ThreadFactory.ThreadCount--;
            thread.Abort();
        }
    }
}