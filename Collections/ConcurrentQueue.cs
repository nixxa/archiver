using System.Collections.Generic;
using System.Threading;
using CancellationToken = Archiver.Threading.CancellationToken;

namespace Archiver.Collections
{
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

        public virtual void Enqueue(T obj)
        {
            WaitFor(CanWrite);
            Lock.EnterWriteLock();
            try
            {
                lock (Internal)
                    Internal.Enqueue(obj);
                SetChanged();
                Block(Internal.Count);
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
                lock (Internal)
                {
                    var result = Internal.Dequeue();
                    Unblock(Internal.Count);
                    return result;
                }
            }
            finally
            {
                Lock.ExitWriteLock();
            }
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
                        Unblock(Internal.Count);
                    }
                    finally
                    {
                        Lock.ExitWriteLock();
                    }
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
                    lock (Internal)
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
