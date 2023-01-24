using System;
using ET;

namespace BM
{
    public class CoroutineLock : IDisposable
    {
        private bool _isDispose = false;
        internal long Key;
        private CoroutineLockQueue _coroutineLockQueue;
        private ETTask _waitTask;
        
        internal void Init(long key, CoroutineLockQueue coroutineLockQueue)
        {
            _isDispose = false;
            this.Key = key;
            this._coroutineLockQueue = coroutineLockQueue;
            _waitTask = ETTask.Create(true);
        }

        internal void Enable()
        {
            _waitTask.SetResult();
        }

        internal ETTask Wait()
        {
            return _waitTask;
        }
        
        
        public void Dispose()
        {
            if (_isDispose)
            {
                //Debug.LogError("协程锁重复释放");
                return;
            }
            _waitTask = null;
            _isDispose = true;
            _coroutineLockQueue.CoroutineLockDispose(this);
            _coroutineLockQueue = null;
            CoroutineLockComponent.CoroutineLockQueue.Enqueue(this);
            
        }
    }
}