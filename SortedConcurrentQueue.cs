using Archiver.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Archiver
{
    public class SortedConcurrentQueue<T> where T: IIndexedItem
    {
        protected readonly Dictionary<int, T> Internal = new Dictionary<int, T>();
        protected readonly ReaderWriterLockSlim Lock = new ReaderWriterLockSlim();

        private int _startIndex;
        private int _nextIndex;

        private ManualResetEvent _waiter = new ManualResetEvent(false);

        public SortedConcurrentQueue(int startIndex = 0)
        {
            _startIndex = startIndex;
            _nextIndex = _startIndex + 1;
        }

        public void Enqueue(T obj)
        {
            Console.WriteLine("Adding index " + obj.Index);
            Lock.EnterWriteLock();
            try
            {
                Internal.Add(obj.Index, obj);
                Console.WriteLine("Added index " + obj.Index);
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

        public T Dequeue()
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
                Console.WriteLine("Removed index " + _nextIndex);
                _nextIndex += 1;
                if (!Internal.ContainsKey(_nextIndex))
                {
                    _waiter.Reset();
                }
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
