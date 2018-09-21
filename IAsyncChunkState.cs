using System.Threading;

namespace Archiver
{
    public interface IAsyncChunkState
    {
        EventWaitHandle Completed { get; }
    }
}
