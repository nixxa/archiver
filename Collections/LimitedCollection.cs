using System;
using System.Threading;
using CancellationToken = Archiver.Threading.CancellationToken;

namespace Archiver.Collections
{
    public class LimitedCollection
    {
        protected readonly int MaxLength;
        protected readonly ManualResetEvent CanWrite = new ManualResetEvent(true);
        protected readonly CancellationToken CancellationToken;

        public LimitedCollection(CancellationToken cancellationToken, int maxLength)
        {
            MaxLength = maxLength;
            CancellationToken = cancellationToken;
        }

        protected void WaitFor(EventWaitHandle eventHandle)
        {
            while (true)
            {
                if (eventHandle.WaitOne(TimeSpan.FromSeconds(1)))
                {
                    return;
                }
                if (CancellationToken.Canceled)
                {
                    if (CancellationToken.Exception != null)
                    {
                        throw new OperationCanceledException(CancellationToken.Exception.Message);
                    }
                    throw new OperationCanceledException();
                }
            }
        }

        protected bool Block(int count)
        {
            if (count >= MaxLength)
            {
                CanWrite.Reset();
                return true;
            }
            return false;
        }

        protected bool Unblock(int count)
        {
            if (count < MaxLength)
            {
                CanWrite.Set();
                return true;
            }
            return false;
        }
    }
}
