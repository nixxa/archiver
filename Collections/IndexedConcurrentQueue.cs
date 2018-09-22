using Archiver.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using CancellationToken = Archiver.Threading.CancellationToken;

namespace Archiver.Collections
{
    public class IndexedConcurrentQueue<T> : DelayedLimitedCollection where T: IIndexedItem
    {
        protected readonly Dictionary<int, T> Internal = new Dictionary<int, T>();
        protected readonly ReaderWriterLockSlim Lock = new ReaderWriterLockSlim();
        protected readonly bool VerboseOutput;

        private int _startIndex;
        private int _nextIndex;

        private ManualResetEvent _waiter = new ManualResetEvent(false);

        public IndexedConcurrentQueue(CancellationToken cancellationToken, int maxLength = short.MaxValue, bool verboseOutput = false)
            : base (cancellationToken, maxLength)
        {
            _startIndex = 0;
            _nextIndex = _startIndex + 1;
            VerboseOutput = verboseOutput;
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
                if (VerboseOutput)
                {
                    Console.WriteLine("IndexedConcurrentQueue: chunk " + obj.Index + " was added");
                }
                SetChanged();
                Block(Internal.Count);
                if (obj.Index == _nextIndex)
                {
                    _waiter.Set();
                }
            }
            finally
            {
                Lock.ExitWriteLock();
            }
        }

        public virtual T Dequeue()
        {
            T item = default(T);
            if (!Internal.TryGetValue(_nextIndex, out item))
            {
                _waiter.WaitOne();
                item = Internal[_nextIndex];
            }
            Lock.EnterWriteLock();
            try
            {
                Internal.Remove(_nextIndex);
                if (VerboseOutput)
                {
                    Console.WriteLine("IndexedConcurrentQueue: chunk " + item.Index + " was removed");
                }
                _nextIndex += 1;
                if (!Internal.ContainsKey(_nextIndex))
                {
                    _waiter.Reset();
                }
                Unblock(Internal.Count);
                return item;
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
