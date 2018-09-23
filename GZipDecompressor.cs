using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using Archiver.Interfaces;

namespace Archiver
{
    public class GZipDecompressor : AbstractProcessor
    {
        public override CompressionType Type => CompressionType.GZip;
        public override CompressionMode Mode => CompressionMode.Decompress;

        private byte[] _magicNumber = new byte[10];
        private int _chunkIndex = 0;

        private static readonly byte[] GzipHeader = new byte[] { 0x1f, 0x8b, 0x08 };

        public GZipDecompressor(Options options) : base(options)
        {
        }

        protected override IChunk CreateChunk()
        {
            return new Chunk();
        }

        private static bool HasHeader(IList<byte> bytes)
        {
            return bytes.Count >= 3 && bytes[0] == 0x1f && bytes[1] == 0x8b && bytes[2] == 0x08;
        }

        private static byte[] AddByte(byte[] source, byte val)
        {
            Array.Copy(source, 1, source, 0, source.Length - 1);
            source[source.Length - 1] = val;
            return source;
        }

        protected override IChunk ReadChunk(FileStream inputStream)
        {
            var chunk = CreateChunk();
            chunk.Index = ++_chunkIndex;
            var bytes = new byte[3];
            int count = 0;
            using (var memStream = new MemoryStream())
            {
                int result = -1;
                byte nextByte = 0;
                do
                {
                    result = inputStream.ReadByte();
                    if (result == -1) break;
                    nextByte = (byte)result;
                    count += 1;

                    memStream.WriteByte(nextByte);

                    bytes = AddByte(bytes, nextByte);
                    if (HasHeader(bytes))
                    {
                        // next header found
                        if (count == 3) continue; // skip first header in file
                        break;
                    }
                }
                while (true);

                var buf = memStream.ToArray();
                if (buf.Length > 0)
                {
                    if (result != -1)
                    {
                        Array.Resize(ref buf, buf.Length - GzipHeader.Length);
                    }
                    if (HasHeader(buf))
                    {
                        chunk.Body = new byte[buf.Length];
                        Array.Copy(buf, 0, chunk.Body, 0, buf.Length);
                    }
                    else
                    {
                        chunk.Body = new byte[buf.Length + GzipHeader.Length];
                        Array.Copy(GzipHeader, chunk.Body, GzipHeader.Length);
                        Array.Copy(buf, 0, chunk.Body, GzipHeader.Length, buf.Length);
                    }
                }
                else
                {
                    chunk.Body = buf;
                }
            }
            if (Options.VerboseOutput)
            {
                Console.WriteLine("AbstractProcessor: chunk " + chunk.Index + " was read");
            }
            return chunk;
        }

        protected override void ExecuteChunk(IChunk chunk)
        {
            var buffer = chunk.Body;
            using (var outStream = new MemoryStream())
            {
                using (var memoryStream = new MemoryStream(buffer))
                {
                    using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Decompress))
                    {
                        int count = -1;
                        do
                        {
                            byte[] buf = new byte[buffer.Length];
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
