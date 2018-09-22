using System;
using System.IO;
using System.IO.Compression;
using Archiver.Interfaces;

namespace Archiver
{
    public class DeflateDecompressor : AbstractProcessor
    {
        public override CompressionType Type => CompressionType.Deflate;
        public override CompressionMode Mode => CompressionMode.Decompress;

        public DeflateDecompressor(Options options) : base(options)
        {
        }

        protected override IChunk CreateChunk()
        {
            throw new NotImplementedException();
        }

        protected override void ExecuteChunk(IChunk chunk)
        {
            using (var outStream = new MemoryStream())
            {
                using (var memoryStream = new MemoryStream(chunk.Body))
                {
                    var buf = new byte[chunk.Body.Length];
                    using (var gzipStream = new DeflateStream(memoryStream, CompressionMode.Decompress))
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
