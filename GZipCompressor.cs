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

        public override CompressionType Type => CompressionType.GZip;
        public override CompressionMode Mode => CompressionMode.Compress;

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
                    gzipStream.Flush();
                }
                chunk.Body = memoryStream.ToArray();
            }
        }

        //public override void Run()
        //{
        //    using (var inputStream = File.OpenRead(Options.Input))
        //    {
        //        using (var outputStream = new FileStream(Options.Output, FileMode.Create))
        //        {
        //            using (var gzipStream = new GZipStream(outputStream, CompressionMode.Compress, false))
        //            {
        //                int count = -1;
        //                byte[] buffer = new byte[Options.ReadBufferSize];
        //                do
        //                {
        //                    count = inputStream.Read(buffer, 0, buffer.Length);
        //                    gzipStream.Write(buffer, 0, count);
        //                }
        //                while (count > 0);
        //            }
        //        }
        //    }
        //}
    }
}
