using Archiver.Threading;
using System.Threading;
using CancellationToken = Archiver.Threading.CancellationToken;

namespace Archiver.Collections
{
    public class DelayedLimitedCollection : LimitedCollection
    {
        private bool _started = false;
        private AutoResetEvent _startedEvent = new AutoResetEvent(false);

        public DelayedLimitedCollection(CancellationToken cancellationToken, int maxLength) : base(cancellationToken, maxLength)
        {
        }

        public void WaitForInput()
        {
            WaitFor(_startedEvent);
        }

        protected void SetChanged()
        {
            if (!_started)
            {
                _started = true;
                _startedEvent.Set();
            }
        }
    }
}
