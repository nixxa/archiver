namespace Archiver
{
    public class Options
    {
        public string Input { get; set; }
        public string Output { get; set; }
        public int ReadBufferSize { get; set; }
        public int MaxBuffers { get; set; }
        public bool UseMultithreading { get; set; }

        public Options()
        {
            ReadBufferSize = 32 * 1024 * 1024;
            MaxBuffers = short.MaxValue;
        }
    }
}
