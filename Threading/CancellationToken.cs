using System;

namespace Archiver.Threading
{
    public class CancellationToken
    {
        public bool Canceled { get; set; }
        public Exception Exception { get; set; }

        public CancellationToken()
        {
            Canceled = false;
        }

        public void Cancel(Exception e)
        {
            Canceled = true;
            Exception = e;
        }
    }
}
