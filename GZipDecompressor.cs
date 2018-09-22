using System.IO;
using System.IO.Compression;
using Archiver.Interfaces;

namespace Archiver
{
    public class GZipDecompressor : AbstractProcessor
    {
        public GZipDecompressor(Options options) : base(options)
        {
        }

        protected override IChunk CreateChunk()
        {
            return new Chunk();
        }

        protected override void ExecuteChunk(IChunk chunk)
        {
            using (var outStream = new MemoryStream())
            {
                using (var memoryStream = new MemoryStream(chunk.Body))
                {
                    var buf = new byte[chunk.Body.Length];
                    using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Decompress))
                    {
                        int count = -1;
                        do
                        {
                            count = gzipStream.Read(buf, 0, buf.Length);
                            outStream.Write(buf, 0, count);
                        }
                        while (count > 0);
                    }
                }
                chunk.Body = outStream.ToArray();
            }
        }
    }
}
