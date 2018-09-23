using Archiver.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using CancellationToken = Archiver.Threading.CancellationToken;

namespace Archiver.Collections
{
    /// <summary>
    /// Synchronized queue with ordered output strategy.
    /// All items in the queue are sorted by IIndexedItem.Index field in an ascending order. Sequential call of Dequeue method will get these items in that order.
    /// </summary>
    /// <typeparam name="T">An indexable queue item</typeparam>
    public class IndexedConcurrentQueue<T> : DelayedLimitedCollection where T: IIndexedItem
    {
        protected readonly Dictionary<int, T> Internal = new Dictionary<int, T>();
        protected readonly ReaderWriterLockSlim Lock = new ReaderWriterLockSlim();
        protected readonly bool VerboseOutput;

        private int _startIndex;
        private int _nextIndex;

        public IndexedConcurrentQueue(CancellationToken cancellationToken, int maxLength = short.MaxValue, bool verboseOutput = false)
            : base (cancellationToken, maxLength)
        {
            _startIndex = 0;
            _nextIndex = _startIndex + 1;
            VerboseOutput = verboseOutput;
        }

        ~IndexedConcurrentQueue()
        {
            if (Lock != null)
            {
                Lock.Dispose();
            }
        }

        public virtual void Enqueue(T obj)
        {
            if (obj.Index != _nextIndex)
            {
                WaitFor(CanWrite);
            }
            Lock.EnterWriteLock();
            try
            {
                Internal.Add(obj.Index, obj);
            }
            finally
            {
                Lock.ExitWriteLock();
            }
            if (VerboseOutput)
            {
                Console.WriteLine("IndexedConcurrentQueue: chunk " + obj.Index + " was added");
            }
            SetChanged();
            Block(Internal.Count);
        }

        public virtual bool TryDequeue(out T item)
        {
            item = default(T);
            Lock.EnterUpgradeableReadLock();
            try
            {
                if (Internal.Count != 0 && Internal.ContainsKey(_nextIndex))
                {
                    Lock.EnterWriteLock();
                    try
                    {
                        item = Internal[_nextIndex];
                        Internal.Remove(_nextIndex);
                    }
                    finally
                    {
                        Lock.ExitWriteLock();
                    }
                    Unblock(Internal.Count);
                    if (VerboseOutput)
                    {
                        Console.WriteLine("IndexedConcurrentQueue: chunk " + item.Index + " was removed");
                    }
                    _nextIndex += 1;
                    return true;
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
