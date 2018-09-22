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

        private int _readingMemory = -1;

        private const int MaximumBufferSize = 8 * 1024 * 1024; // 8Mb

        public Options()
        {
            ReadBufferSize = 32 * 1024 * 1024;
            MaxBuffers = short.MaxValue;
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
                MaxBuffers = (int) (bytes / MaximumBufferSize);
                ReadBufferSize = MaximumBufferSize;
            }
            else
            {
                ReadBufferSize = (int) (bytes / Environment.ProcessorCount);
                MaxBuffers = (int) (bytes / ReadBufferSize);
            }
        }
    }
}
