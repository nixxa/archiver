using System;
using System.Threading;

namespace Archiver
{
    internal class AsyncResult<T> : IAsyncResult, IDisposable
    {
        private readonly AsyncCallback callback_;
        private bool completed_;
        private bool completedSynchronously_;
        private readonly object asyncState_;
        private readonly ManualResetEvent waitHandle_;
        private T result_;
        private Exception e_;
        private readonly object syncRoot_;

        internal AsyncResult(AsyncCallback cb, object state)
            : this(cb, state, false)
        {
        }

        internal AsyncResult(AsyncCallback cb, object state, bool completed)
        {
            this.callback_ = cb;
            this.asyncState_ = state;
            this.completed_ = completed;
            this.completedSynchronously_ = completed;

            this.waitHandle_ = new ManualResetEvent(false);
            this.syncRoot_ = new object();
        }

        public object AsyncState
        {
            get { return this.asyncState_; }
        }

        public WaitHandle AsyncWaitHandle
        {
            get { return this.waitHandle_; }
        }

        public bool CompletedSynchronously
        {
            get
            {
                lock (this.syncRoot_)
                {
                    return this.completedSynchronously_;
                }
            }
        }

        public bool IsCompleted
        {
            get
            {
                lock (this.syncRoot_)
                {
                    return this.completed_;
                }
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                lock (this.syncRoot_)
                {
                    if (this.waitHandle_ != null)
                    {
                        ((IDisposable)this.waitHandle_).Dispose();
                    }
                }
            }
        }

        internal Exception Exception
        {
            get
            {
                lock (this.syncRoot_)
                {
                    return this.e_;
                }
            }
        }

        internal T Result
        {
            get
            {
                lock (this.syncRoot_)
                {
                    return this.result_;
                }
            }
        }

        internal void Complete(T result, bool completedSynchronously)
        {
            lock (this.syncRoot_)
            {
                this.completed_ = true;
                this.completedSynchronously_ = completedSynchronously;
                this.result_ = result;
            }
            this.SignalCompletion();
        }

        internal void HandleException(Exception e, bool completedSynchronously)
        {
            lock (this.syncRoot_)
            {
                this.completed_ = true;
                this.completedSynchronously_ = completedSynchronously;
                this.e_ = e;
            }

            this.SignalCompletion();
        }

        private void SignalCompletion()
        {
            this.waitHandle_.Set();
            ThreadPool.QueueUserWorkItem(new WaitCallback(this.InvokeCallback));
        }

        private void InvokeCallback(object state)
        {
            if (this.callback_ != null)
            {
                this.callback_(this);
            }
        }
    }
}
