using Mono.Options;
using System;
using System.IO;

namespace Archiver
{
    class Program
    {
        [MTAThread]
        static void Main(string[] args)
        {
            try
            {
                var options = new Options();

                var optionSet = new OptionSet()
                {
                    { "in=|input=", "input filename", v => options.Input = v },
                    { "out=|output=", "output filename", v => options.Output = v },
                    { "mem:", "maximum memory for read buffers in megabytes", v => options.ReadingMemory = (v != null ? int.Parse(v) : -1) },
                    { "v|verbose", "show detailed information during execution", v => options.VerboseOutput = (v != null) }
                };
                var commandSet = new CommandSet("archiver")
                {
                    "usage: archiver.exe COMMAND [arguments] [options]",
                    new Command("compress", "Compress input file to output file")
                    {
                        Options = optionSet,
                        Run = argv => new GZipCompressor(options).Run()
                    },
                    new Command("decompress", "Decompress input file to output file")
                    {
                        Options = optionSet,
                        Run = argv => new GZipDecompressor(options).Run()
                    }
                };
                commandSet.Run(args);
            }
            catch (IOException ioe)
            {
                Console.WriteLine(ioe.Message);
            }
        }
    }
}
