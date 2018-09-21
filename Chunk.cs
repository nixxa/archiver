using System;
using System.IO;
using System.IO.Compression;

namespace Archiver
{
    public class Chunk
    {
        public int Index { get; }
        public byte[] Buffer { get; }
        public int Count { get; set; }
        public TimeSpan WriteTimeout { get; }
        public byte[] Compressed { get; set; }

        public Chunk(int index, byte[] buffer, TimeSpan timeout)
        {
            Index = index;
            Buffer = buffer;
            WriteTimeout = timeout;
        }

        public void Compress()
        {
            using (var memoryStream = new MemoryStream())
            {
                using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Compress))
                {
                    gzipStream.Write(Buffer, 0, Count);
                }
                Compressed = memoryStream.ToArray();
            }
        }
    }
}
