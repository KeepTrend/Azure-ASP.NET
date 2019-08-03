using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Hosting;

namespace Exchange.Global
{
    /// <summary>
    /// Every instance of this class represents a scheduled task that is executed in the current AppDomain
    /// </summary>
    public class BackgroundJob : IRegisteredObject
    {
        private bool disposed = false;
        private System.Threading.Timer recurringTimer;
        private Action work;
        private int intervalInMs;
        private CancellationTokenSource cts;
        private Task workTask = null;
        private const int TIMEOUT_TO_KILL = 10000;

        /// <summary>
        /// When the instance is constructed the background job is automatically started.
        /// </summary>
        /// <param name="work">Pass method without parameters that does not return a value to be executed the regularly.</param>
        /// <param name="intervalInSeconds">Interval in seconds between executions.</param>
        /// <param name="runImmediately">Flag that indicates if method runs immediately after the instance is created or waits until the interval elapses.</param>
        public BackgroundJob(Action work, int intervalInSeconds, bool runImmediately = true)
        {
            this.work = work;
            this.intervalInMs = intervalInSeconds * 1000;
            this.cts = new CancellationTokenSource();
            this.recurringTimer = new System.Threading.Timer(Callback, null,
                                                             runImmediately ? 1 : this.intervalInMs,
                                                             Timeout.Infinite);
            HostingEnvironment.RegisterObject(this);
        }

        private async void Callback(object state)
        {
            try
            {
                this.workTask = new Task(() => work.Invoke());
                this.workTask.Start();
                var ct = this.cts.Token;
                await this.workTask.WithCancellation(ct);
            }
            catch (OperationCanceledException)
            { }
            finally
            {
                this.recurringTimer.Change(this.intervalInMs, Timeout.Infinite);
            }
        }

        public void Stop(bool immediate)
        {
            try
            {
                if (this.workTask != null && this.workTask.Status == TaskStatus.Running)
                {
                    this.cts.CancelAfter(TIMEOUT_TO_KILL);
                    this.workTask.Wait();
                }
            }
            finally
            {
                HostingEnvironment.UnregisterObject(this);
            }
        }

        //IDisposable implementation omitted for space saving
    }
    public static class TaskExtensions
    {
        public static async Task WithCancellation(this Task originalTask, CancellationToken ct)
        {
            var cancelTask = new TaskCompletionSource<object>();
            using (ct.Register(t => ((TaskCompletionSource<object>)t).TrySetResult(new object()), cancelTask))
            {
                Task any = await Task.WhenAny(originalTask, cancelTask.Task);
                if (any == cancelTask.Task)
                    ct.ThrowIfCancellationRequested();
            }
            await originalTask;
        }
    }
}