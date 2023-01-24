using System;
using System.Collections.Generic;
using System.Threading;

namespace LMTD
{
    public static class ThreadFactory
    {
        /// <summary>
        /// 所有线程的数量
        /// </summary>
        internal static uint ThreadCount = 0;
        
        /// <summary>
        /// 所有线程的池
        /// </summary>
        internal static readonly Queue<LiteThread> ThreadPool = new Queue<LiteThread>();
        
        private static readonly HashSet<LiteThread> AllLiteThread = new HashSet<LiteThread>();

        /// <summary>
        /// 是否开启回收进程，默认不开启
        /// </summary>
        internal static bool RecoverKey
        {
            set
            {
                if (value == false)
                {
                    //说明所有进程都已经被回收
                }
            }
            get => recoverKey;
        }
        private static bool recoverKey = false;

        /// <summary>
        /// 生命周期
        /// </summary>
        private static Thread _threadFactoryLife = null;
        
        /// <summary>
        /// 执行一个逻辑
        /// </summary>
        public static void ThreadAction(ILiteThreadAction liteThreadAction)
        {
            if (_threadFactoryLife == null)
            {
                _threadFactoryLife = new Thread(ThreadUpdate);
                _threadFactoryLife.Start();
            }
            LiteThread liteThread;
            lock (ThreadPool)
            {
                if (ThreadPool.Count > 0)
                {
                    liteThread = ThreadPool.Dequeue();
                }
                else
                {
                    liteThread = new LiteThread();
                    AllLiteThread.Add(liteThread);
                }
            }
            AllLiteThread.Add(liteThread);
            liteThread.Action(liteThreadAction);
        }


        private static long _lastTime = 0;
        /// <summary>
        /// 线程池清空标志位(如果5-10秒内池有多余的线程线程就清空)
        /// </summary>
        private static bool _poolClear = false;
        /// <summary>
        /// 线程自动回收机制
        /// </summary>
        private static void ThreadUpdate()
        {
            _lastTime = DateTime.Now.Ticks;
            while (true)
            {
                long nowTime = DateTime.Now.Ticks;
                if (nowTime - _lastTime > 50000000)
                {
                    _lastTime = nowTime;
                    //每隔5s一次循环
                    lock (ThreadPool)
                    {
                        if (ThreadPool.Count == 0)
                        {
                            continue;
                        }
                        if (!_poolClear)
                        {
                            _poolClear = true;
                            continue;
                        }
                        LiteThread liteThread = ThreadPool.Dequeue();
                        AllLiteThread.Remove(liteThread);
                        liteThread.Recovery();
                        _poolClear = false;
                        if (ThreadCount == 0)
                        {
                            _lastTime = 0;
                            _threadFactoryLife?.Abort();
                            _threadFactoryLife = null;
                        }
                    }
                }
                Thread.Sleep(1);
            }
        }

        /// <summary>
        /// 线程池销毁
        /// </summary>
        public static void Destroy()
        {
            foreach (LiteThread liteThread in AllLiteThread)
            {
                liteThread.Recovery();
            }
            AllLiteThread.Clear();
            lock (ThreadPool)
            {
                ThreadPool.Clear();
            }
            ThreadCount = 0;
            _lastTime = 0;
            _poolClear = false;
            _threadFactoryLife?.Abort();
            _threadFactoryLife = null;
        }

    }
}