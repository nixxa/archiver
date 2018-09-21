using System.Threading;

namespace Archiver
{
    public class AsyncChunkState : IAsyncChunkState
    {
        public EventWaitHandle Completed { get; set; }

        public AsyncChunkState()
        {
            Completed = new AutoResetEvent(false);
        }
    }
}
