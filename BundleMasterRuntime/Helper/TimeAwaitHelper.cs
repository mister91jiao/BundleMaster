using System.Collections.Generic;
using ET;

namespace BM
{
    public static class TimeAwaitHelper
    {
        internal static readonly Queue<TimerAwait> TimerFactoryQueue = new Queue<TimerAwait>();

        /// <summary>
        /// 等待一定时间
        /// </summary>
        /// <param name="time">单位 秒</param>
        /// <param name="cancellationToken">异步取消锁</param>
        public static ETTask AwaitTime(float time, ETCancellationToken cancellationToken = null)
        {
            TimerAwait timerAwait;
            if (TimerFactoryQueue.Count > 0)
            {
                timerAwait = TimerFactoryQueue.Dequeue();
            }
            else
            {
                timerAwait = new TimerAwait();
            }
            ETTask tcs = ETTask.Create(true);
            timerAwait.Init(time, tcs);
            cancellationToken?.Add(() =>
            {
                timerAwait.Cancel();
                tcs.SetResult();
            });
            return tcs;
        }
    }
    

    internal class TimerAwait
    {
        private float remainingTime = 0;
        private ETTask tcs;
        private bool cancelTimer = false;

        internal void Init(float time, ETTask task)
        {
            this.remainingTime = time;
            this.tcs = task;
            AssetComponent.TimerAwaitQueue.Enqueue(this);
            cancelTimer = false;
        }
        
        internal bool CalcSubTime(float time)
        {
            remainingTime -= time;
            if (remainingTime > 0)
            {
                return false;
            }
            if (!cancelTimer)
            {
                tcs.SetResult();
            }
            tcs = null;
            remainingTime = 0;
            TimeAwaitHelper.TimerFactoryQueue.Enqueue(this);
            return true;
        }
        
        internal void Cancel()
        {
            cancelTimer = true;
        }
    }
}