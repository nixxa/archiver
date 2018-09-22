using System.IO;
using System.IO.Compression;
using Archiver.Interfaces;

namespace Archiver
{
    public class GZipCompressor : AbstractProcessor
    {
        public GZipCompressor(Options options) : base(options)
        {
        }

        protected override IChunk CreateChunk()
        {
            return new Chunk();
        }

        protected override void ExecuteChunk(IChunk chunk)
        {
            using (var memoryStream = new MemoryStream())
            {
                using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Compress))
                {
                    gzipStream.Write(chunk.Body, 0, chunk.Body.Length);
                }
                chunk.Body = memoryStream.ToArray();
            }
        }
    }
}
