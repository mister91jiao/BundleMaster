using System.Collections.Generic;
using ET;

namespace BM
{
    /// <summary>
    /// 每个协程锁类型与其对应的队列
    /// </summary>
    internal class CoroutineLockQueue
    {
        private CoroutineLockType _coroutineLockType;
        /// <summary>
        /// 每个key的以及其对应的队列
        /// </summary>
        private readonly Dictionary<long, Queue<CoroutineLock>> _coroutineLockKeyToQueue = new Dictionary<long, Queue<CoroutineLock>>();

        /// <summary>
        /// 可以瞬间完成的锁循环的数量(与设备栈大小有关)
        /// </summary>
        private const int LoopCount = 200;
        
        /// <summary>
        /// 递归循环次数
        /// </summary>
        private readonly Dictionary<long, int> _coroutineLockKeyToLoopCount = new Dictionary<long, int>();
        
        internal CoroutineLockQueue(CoroutineLockType coroutineLockType)
        {
            this._coroutineLockType = coroutineLockType;
        }
        
        internal async ETTask CoroutineLockDispose(long key)
        {
            Queue<CoroutineLock> keyToQueue = _coroutineLockKeyToQueue[key];
            if (keyToQueue.Count > 0)
            {
                if (_coroutineLockKeyToLoopCount[key] > 0)
                {
                    _coroutineLockKeyToLoopCount[key]--;
                    keyToQueue.Dequeue().Enable();
                    return;
                }
                _coroutineLockKeyToLoopCount[key] = LoopCount;
                await CoroutineLockComponent.WaitTask();
                keyToQueue.Dequeue().Enable();
                return;
            }
            keyToQueue.Clear();
            CoroutineLockComponent.CoroutineLockQueuePool.Enqueue(keyToQueue);
            _coroutineLockKeyToQueue.Remove(key);
            _coroutineLockKeyToLoopCount.Remove(key);
        }
        
        /// <summary>
        /// 获取一个锁头
        /// </summary>
        internal CoroutineLock GetCoroutineLock(long key)
        {
            CoroutineLock coroutineLock;
            if (CoroutineLockComponent.CoroutineLockQueue.Count > 0)
            {
                coroutineLock = CoroutineLockComponent.CoroutineLockQueue.Dequeue();
            }
            else
            {
                coroutineLock = new CoroutineLock();
            }
            coroutineLock.Init(key, this);
            if (!_coroutineLockKeyToQueue.ContainsKey(key))
            {
                Queue<CoroutineLock> coroutineLockQueue;
                if (CoroutineLockComponent.CoroutineLockQueuePool.Count > 0)
                {
                    coroutineLockQueue = CoroutineLockComponent.CoroutineLockQueuePool.Dequeue();
                }
                else
                {
                    coroutineLockQueue = new Queue<CoroutineLock>();
                }
                _coroutineLockKeyToLoopCount.Add(key, LoopCount);
                _coroutineLockKeyToQueue.Add(key, coroutineLockQueue);
                coroutineLock.Enable();
            }
            else
            {
                _coroutineLockKeyToQueue[key].Enqueue(coroutineLock);
            }
            return coroutineLock;
        }

    }
}