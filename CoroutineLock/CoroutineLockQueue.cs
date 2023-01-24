using System.Collections.Generic;

namespace BM
{
    internal class CoroutineLockQueue
    {
        private CoroutineLockType _coroutineLockType;
        private readonly Dictionary<long, Queue<CoroutineLock>> _coroutineLockKeyToQueue = new Dictionary<long, Queue<CoroutineLock>>();
        
        internal CoroutineLockQueue(CoroutineLockType coroutineLockType)
        {
            this._coroutineLockType = coroutineLockType;
        }
        
        internal void CoroutineLockDispose(CoroutineLock coroutineLock)
        {
            Queue<CoroutineLock> keyToQueue = _coroutineLockKeyToQueue[coroutineLock.Key];
            if (keyToQueue.Count > 0)
            {
                CoroutineLock nextCoroutineLock = keyToQueue.Dequeue();
                nextCoroutineLock.Enable();
                return;
            }
            keyToQueue.Clear();
            CoroutineLockComponent.CoroutineLockQueuePool.Enqueue(keyToQueue);
            _coroutineLockKeyToQueue.Remove(coroutineLock.Key);
        }
        
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