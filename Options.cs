using System;

namespace Archiver
{
    public class Options
    {
        public bool VerboseOutput { get; set; }
        public string Input { get; set; }
        public string Output { get; set; }
        public int ReadBufferSize { get; private set; }
        public int MaxBuffers { get; private set; }
        public int ReadingMemory
        {
            get
            {
                return _readingMemory;
            }
            set
            {
                _readingMemory = value;
                CalculateBuffers();
            }
        }
        public CompressionType Type { get; set; }


        private int _readingMemory = -1;

        private const int MaximumBufferSize = 1024 * 1024; // 8Mb

        public Options()
        {
            ReadBufferSize = 32 * 1024 * 1024;
            MaxBuffers = short.MaxValue;
            Type = CompressionType.GZip;
        }

        private void CalculateBuffers()
        {
            if (ReadingMemory == -1)
            {
                ReadBufferSize = MaximumBufferSize;
                MaxBuffers = short.MaxValue;
            }
            long bytes = ReadingMemory * 1024 * 1024;
            if (bytes > MaximumBufferSize)
            {
                MaxBuffers = (int) (bytes / MaximumBufferSize) / 2;
                ReadBufferSize = MaximumBufferSize;
            }
            else
            {
                ReadBufferSize = (int) (bytes / Environment.ProcessorCount);
                MaxBuffers = (int) (bytes / ReadBufferSize) / 2;
            }
        }
    }
}
