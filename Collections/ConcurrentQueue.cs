﻿using System.Collections.Generic;
using System.Threading;

namespace Archiver.Collections
{
    public class ConcurrentQueue<T>
    {
        protected readonly Queue<T> Internal = new Queue<T>();
        protected readonly ReaderWriterLockSlim Lock = new ReaderWriterLockSlim();
        protected readonly int MaxLength;
        protected readonly ManualResetEvent CanWrite = new ManualResetEvent(true);
        protected readonly bool VerboseOutput;

        private bool _started = false;
        private AutoResetEvent _startedEvent = new AutoResetEvent(false);

        public ConcurrentQueue(int maxLength = short.MaxValue, bool verboseOutput = false)
        {
            MaxLength = maxLength;
            VerboseOutput = verboseOutput;
        }

        public void WaitForInput()
        {
            _startedEvent.WaitOne();
        }

        public virtual void Enqueue(T obj)
        {
            CanWrite.WaitOne();
            Lock.EnterWriteLock();
            try
            {
                Internal.Enqueue(obj);
                if (!_started)
                {
                    _started = true;
                    _startedEvent.Set();
                }
                if (Internal.Count == MaxLength)
                {
                    CanWrite.Reset();
                }
            }
            finally
            {
                Lock.ExitWriteLock();
            }
        }

        public virtual T Dequeue()
        {
            Lock.EnterWriteLock();
            try
            {
                var result = Internal.Dequeue();
                if (Internal.Count < MaxLength)
                {
                    CanWrite.Set();
                }
                return result;
            }
            finally
            {
                Lock.ExitWriteLock();
            }
        }

        public int Count
        {
            get
            {
                Lock.EnterReadLock();
                try
                {
                    return Internal.Count;
                }
                finally
                {
                    Lock.ExitReadLock();
                }
            }
        }
    }
}
