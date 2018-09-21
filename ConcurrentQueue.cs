using System.Collections.Generic;
using System.Threading;

namespace Archiver
{
    public class ConcurrentQueue<T>
    {
        private readonly Queue<T> _internal = new Queue<T>();
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
        private readonly int _maxLength = 10;
        private readonly ManualResetEvent _canWrite = new ManualResetEvent(true);

        public ConcurrentQueue(int maxLength = short.MaxValue)
        {
            _maxLength = maxLength;
        }

        public void Enqueue(T obj)
        {
            _canWrite.WaitOne();
            try
            {
                _lock.EnterWriteLock();
                _internal.Enqueue(obj);
                if (_internal.Count == _maxLength)
                {
                    _canWrite.Reset();
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public T Dequeue()
        {
            try
            {
                _lock.EnterWriteLock();
                var result = _internal.Dequeue();
                if (_internal.Count < _maxLength)
                {
                    _canWrite.Set();
                }
                return result;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public IEnumerable<T> Dequeue(int count)
        {
            try
            {
                _lock.EnterWriteLock();
                var result = new List<T>();
                for (int i = 0; i < count && _internal.Count > 0; i++)
                {
                    result.Add(_internal.Dequeue());
                }
                if (_internal.Count < _maxLength)
                {
                    _canWrite.Set();
                }
                return result;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public int Count
        {
            get
            {
                try
                {
                    _lock.EnterReadLock();
                    return _internal.Count;
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }
        }
    }
}
