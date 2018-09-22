﻿using Archiver.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Archiver.Collections
{
    public class IndexedConcurrentQueue<T> where T: IIndexedItem
    {
        protected readonly Dictionary<int, T> Internal = new Dictionary<int, T>();
        protected readonly ReaderWriterLockSlim Lock = new ReaderWriterLockSlim();
        protected readonly bool VerboseOutput;

        private int _startIndex;
        private int _nextIndex;

        private ManualResetEvent _waiter = new ManualResetEvent(false);

        private bool _started = false;
        private AutoResetEvent _startedEvent = new AutoResetEvent(false);

        public IndexedConcurrentQueue(int startIndex = 0, bool verboseOutput = false)
        {
            _startIndex = startIndex;
            _nextIndex = _startIndex + 1;
            VerboseOutput = verboseOutput;
        }

        public void WaitForInput()
        {
            _startedEvent.WaitOne();
        }

        public void Enqueue(T obj)
        {
            Lock.EnterWriteLock();
            try
            {
                Internal.Add(obj.Index, obj);
                if (VerboseOutput)
                {
                    Console.WriteLine("IndexedConcurrentQueue: chunk " + obj.Index + " was added");
                }
                if (!_started)
                {
                    _started = true;
                    _startedEvent.Set();
                }
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
                if (VerboseOutput)
                {
                    Console.WriteLine("IndexedConcurrentQueue: chunk " + item.Index + " was removed");
                }
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