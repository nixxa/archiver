using Archiver.Interfaces;
using System.IO;
using System.IO.Compression;

namespace Archiver
{
    public class Chunk : IIndexedItem
    {
        public int Index { get; }
        public byte[] Input { get; }
        public int InputLength { get; set; }
        public byte[] Output { get; set; }

        public Chunk(int index, byte[] buffer)
        {
            Index = index;
            Input = buffer;
        }

        public void Compress()
        {
            using (var memoryStream = new MemoryStream())
            {
                using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Compress))
                {
                    gzipStream.Write(Input, 0, InputLength);
                }
                Output = memoryStream.ToArray();
            }
        }

        public void Decompress()
        {
            using (var outStream = new MemoryStream())
            {
                using (var memoryStream = new MemoryStream(Input))
                {
                    var buf = new byte[Input.Length];
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
                Output = outStream.ToArray();
            }
        }
    }
}
