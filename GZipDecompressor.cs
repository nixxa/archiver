using System;
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
        private byte[] _remains = new byte[0];

        public GZipDecompressor(Options options) : base(options)
        {
        }

        private static int Match(byte[] source, byte[] pattern, int startIndex)
        {
            int result = -1;
            for (int i = startIndex; i < source.Length - pattern.Length; i++)
            {
                bool match = false;
                for (int k = 0; k < pattern.Length; k++)
                {
                    if (pattern[k] != source[i+k])
                    {
                        match = false;
                        break;
                    }
                    match = true;
                }
                if (match)
                {
                    result = i;
                    break;
                }
            }
            return result;
        }

        private void AssignChunkValue(IChunk chunk, byte[] value)
        {
            chunk.Body = value;
            if (Options.VerboseOutput)
            {
                Console.WriteLine("AbstractProcessor: chunk " + chunk.Index + " was read");
            }
        }

        private bool ParseRemains(IChunk chunk)
        {
            int position = -1;
            if ((position = Locate(_remains, _remains.Length)) != -1)
            {
                var buf = new byte[position];
                Buffer.BlockCopy(_remains, 0, buf, 0, buf.Length);
                AssignChunkValue(chunk, buf);
                buf = new byte[_remains.Length - position];
                Buffer.BlockCopy(_remains, position, buf, 0, buf.Length);
                _remains = buf;
                return true;
            }
            return false;
        }

        private int Locate(byte[] source, int length)
        {
            int position = Match(source, GzipHeader, 0);
            if (position == 0)
            {
                position = Match(source, GzipHeader, GzipHeader.Length);
            }
            if (position != -1)
            {
                return position;
            }
            return -1;
        }

        protected override IChunk ReadChunk(Stream inputStream)
        {
            var chunk = CreateChunk();
            chunk.Index = ++_chunkIndex;
            if (_remains.Length > 0)
            {
                if (ParseRemains(chunk))
                {
                    return chunk;
                }
            }

            var bytes = new byte[Options.ReadBufferSize];
            using (var memStream = new MemoryStream())
            {
                if (_remains.Length > 0)
                {
                    memStream.Write(_remains, 0, _remains.Length);
                    _remains = new byte[0];
                }
                int readCount = -1;
                do
                {
                    readCount = inputStream.Read(bytes, 0, bytes.Length);
                    int position = -1;
                    if ((position = Locate(bytes, readCount)) != -1)
                    {
                        memStream.Write(bytes, 0, position - 1);
                        _remains = new byte[readCount - position];
                        Buffer.BlockCopy(bytes, position, _remains, 0, _remains.Length);
                        break;
                    }
                    memStream.Write(bytes, 0, readCount);
                }
                while (readCount > 0);

                AssignChunkValue(chunk, memStream.ToArray());
                return chunk;
            }
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
