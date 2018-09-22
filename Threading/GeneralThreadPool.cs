using Archiver.Collections;
using System;
using System.Threading;

namespace Archiver.Threading
{
    public class GeneralThreadPool : IDisposable
    {
        private readonly ConcurrentQueue<Action> _queue = new ConcurrentQueue<Action>();
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
        private readonly Thread[] _pool;
        private volatile bool _active;
        private bool _adding = true;
        private readonly int _maxLength;
        private ManualResetEvent _canWrite = new ManualResetEvent(true);
        private AutoResetEvent _completed = new AutoResetEvent(false);

        public Exception Exception { get; set; }

        public GeneralThreadPool(int maxLength = short.MaxValue)
        {
            int maxThreads = Environment.ProcessorCount;
            _maxLength = maxLength;
            _active = true;
            _pool = new Thread[maxThreads];
            for (int i = 0; i < _pool.Length; i++)
            {
                _pool[i] = new Thread(RunThread);
                _pool[i].Start();
            }
        }

        public void Enqueue(Action action)
        {
            _canWrite.WaitOne();
            _lock.EnterWriteLock();
            try
            {
                _queue.Enqueue(action);
                if (_queue.Count >= _maxLength)
                {
                    _canWrite.Reset();
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public void StartAdding()
        {
            _adding = true;
        }

        public void StopAdding()
        {
            _adding = false;
        }

        public void WaitForCompletion()
        {
            _completed.WaitOne();
        }

        private void RunThread()
        {
            while (_active)
            {
                Action action = null;
                _lock.EnterUpgradeableReadLock();
                try
                {
                    if (_queue.Count == 0)
                    {
                        if (!_adding)
                        {
                            _completed.Set();
                        }
                        Thread.Sleep(10);
                        continue;
                    }
                    _lock.EnterWriteLock();
                    try
                    {
                        action = _queue.Dequeue();
                        if (_queue.Count < _maxLength)
                        {
                            _canWrite.Set();
                        }
                    }
                    finally
                    {
                        _lock.ExitWriteLock();
                    }
                }
                finally
                {
                    _lock.ExitUpgradeableReadLock();
                }

                try
                {
                    action();
                }
                catch (Exception e)
                {
                    Exception = e;
                    throw;
                }
            }
        }

        public void Dispose()
        {
            _active = false;
            for (int i = 0; i < _pool.Length; i++)
            {
                _pool[i].Join();
            }
        }
    }
}
