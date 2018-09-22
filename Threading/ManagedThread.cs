using System;
using System.Threading;

namespace Archiver.Threading
{
    public class ManagedThread
    {
        private Thread _worker;

        public ManagedThread(Action action, Action<Exception> errorCallback)
        {
            _worker = new Thread(new ThreadStart(() =>
            {
                try
                {
                    action();
                }
                catch (Exception e)
                {
                    errorCallback(e);
                }
            }));
        }

        public void Start()
        {
            _worker.Start();
        }

        public void Join()
        {
            _worker.Join();
        }
    }
}
