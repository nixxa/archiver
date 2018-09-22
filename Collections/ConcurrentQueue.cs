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
                var result = Internal.Dequeue();
                Unblock(Internal.Count);
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
