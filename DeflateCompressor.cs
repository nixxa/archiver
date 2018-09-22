using System.IO;
using System.IO.Compression;
using Archiver.Interfaces;

namespace Archiver
{
    public class DeflateCompressor : AbstractProcessor
    {
        public override CompressionType Type => CompressionType.Deflate;
        public override CompressionMode Mode => CompressionMode.Compress;

        public DeflateCompressor(Options options) : base(options)
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
                using (var gzipStream = new DeflateStream(memoryStream, CompressionMode.Compress))
                {
                    gzipStream.Write(chunk.Body, 0, chunk.Body.Length);
                }
                chunk.Body = memoryStream.ToArray();
            }
        }
    }
}
