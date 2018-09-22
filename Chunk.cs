using Archiver.Interfaces;

namespace Archiver
{
    public class Chunk : IChunk
    {
        public int Index { get; set; }
        public byte[] Body { get; set; }
    }
}
