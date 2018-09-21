namespace Archiver
{
    public class Decompressor : AbstractGzipProcessor
    {
        public Decompressor(Options options) : base(options)
        {
        }

        protected override void ExecuteChunk(Chunk chunk)
        {
            chunk.Decompress();
        }
    }
}
