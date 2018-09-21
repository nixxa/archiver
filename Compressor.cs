namespace Archiver
{
    public class Compressor : AbstractGzipProcessor
    {
        public Compressor(Options options) : base(options)
        {
        }

        protected override void ExecuteChunk(Chunk chunk)
        {
            chunk.Compress();
        }
    }
}
