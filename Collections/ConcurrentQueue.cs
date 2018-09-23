using System.Collections.Generic;
using System.Threading;
using CancellationToken = Archiver.Threading.CancellationToken;

namespace Archiver.Collections
{
    /// <summary>
    /// Synchronized queue with FIFO strategy, 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ConcurrentQueue<T> : DelayedLimitedCollection
    {
        protected readonly Queue<T> Internal = new Queue<T>();
        protected readonly ReaderWriterLockSlim Lock = new ReaderWriterLockSlim();        
        protected readonly bool VerboseOutput;

        public ConcurrentQueue(CancellationToken cancellationToken, int maxLength = short.MaxValue, bool verboseOutput = false)
            : base(cancellationToken, maxLength)
        {
            VerboseOutput = verboseOutput;
        }

        ~ConcurrentQueue()
        {
            if (Lock != null)
            {
                Lock.Dispose();
            }
        }

        /// <summary>
        /// Add item to a queue
        /// </summary>
        /// <param name="obj">queue item</param>
        public virtual void Enqueue(T obj)
        {
            WaitFor(CanWrite);
            Lock.EnterWriteLock();
            try
            {
                Internal.Enqueue(obj);
            }
            finally
            {
                Lock.ExitWriteLock();
            }
            SetChanged();
            Block(Internal.Count);
        }

        /// <summary>
        /// Get next item in queue
        /// </summary>
        /// <returns>A queue item</returns>
        public virtual T Dequeue()
        {
            T result = default(T);
            Lock.EnterWriteLock();
            try
            {
                result = Internal.Dequeue();
            }
            finally
            {
                Lock.ExitWriteLock();
            }
            Unblock(Internal.Count);
            return result;
        }

        public virtual bool TryDequeue(out T item)
        {
            Lock.EnterUpgradeableReadLock();
            try
            {
                if (Internal.Count > 0)
                {
                    Lock.EnterWriteLock();
                    try
                    {
                        item = Internal.Dequeue();
                    }
                    finally
                    {
                        Lock.ExitWriteLock();
                    }
                    Unblock(Internal.Count);
                    return true;
                }
                else
                {
                    item = default(T);
                }
                return false;
            }
            finally
            {
                Lock.ExitUpgradeableReadLock();
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
