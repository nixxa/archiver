using Archiver.Collections;
using System;
using System.Threading;

namespace Archiver.Threading
{
    public class GeneralThreadPool : IDisposable
    {
        private readonly ConcurrentQueue<Action> _queue;
        private readonly Thread[] _pool;
        private volatile bool _active;
        private bool _adding = true;
        private AutoResetEvent _completed = new AutoResetEvent(false);

        public Exception Exception { get; set; }

        public GeneralThreadPool(CancellationToken cancellationToken, int maxLength = short.MaxValue)
        {
            _queue = new ConcurrentQueue<Action>(cancellationToken, maxLength);
            int maxThreads = Environment.ProcessorCount;
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
            _queue.Enqueue(action);
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
                if (_queue.TryDequeue(out action))
                {
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
                else
                {
                    if (!_adding)
                    {
                        _completed.Set();
                    }
                    Thread.Sleep(10);
                    continue;
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
