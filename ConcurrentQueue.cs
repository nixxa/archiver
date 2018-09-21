using System.Collections.Generic;
using System.Threading;

namespace Archiver
{
    public class ConcurrentQueue<T>
    {
        protected readonly Queue<T> Internal = new Queue<T>();
        protected readonly ReaderWriterLockSlim Lock = new ReaderWriterLockSlim();
        protected readonly int MaxLength;
        protected readonly ManualResetEvent CanWrite = new ManualResetEvent(true);

        public ConcurrentQueue(int maxLength = short.MaxValue)
        {
            MaxLength = maxLength;
        }

        public virtual void Enqueue(T obj)
        {
            CanWrite.WaitOne();
            Lock.EnterWriteLock();
            try
            {
                Internal.Enqueue(obj);
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
