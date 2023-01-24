using System.Collections.Generic;
using ET;

namespace BM
{
    public static class CoroutineLockComponent
    {
        /// <summary>
        /// 协程锁类型以及对应的协程锁队列
        /// </summary>
        private static readonly Dictionary<CoroutineLockType, CoroutineLockQueue> CoroutineLockTypeToQueue = new Dictionary<CoroutineLockType, CoroutineLockQueue>();
        
        /// <summary>
        /// 没有用到的CoroutineLock池
        /// </summary>
        internal static readonly Queue<CoroutineLock> CoroutineLockQueue = new Queue<CoroutineLock>();
        
        /// <summary>
        /// 缓存池的池
        /// </summary>
        internal static readonly Queue<Queue<CoroutineLock>> CoroutineLockQueuePool = new Queue<Queue<CoroutineLock>>();

        public static async ETTask<CoroutineLock> Wait(CoroutineLockType coroutineLockType, long key)
        {
            if (!CoroutineLockTypeToQueue.TryGetValue(coroutineLockType, out CoroutineLockQueue coroutineLockQueue))
            {
                coroutineLockQueue = new CoroutineLockQueue(coroutineLockType);
                CoroutineLockTypeToQueue.Add(coroutineLockType, coroutineLockQueue);
            }
            //取一个 CoroutineLock
            CoroutineLock coroutineLock = coroutineLockQueue.GetCoroutineLock(key);
            await coroutineLock.Wait();
            return coroutineLock;
        }
    }
}

