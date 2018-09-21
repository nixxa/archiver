using Archiver.Exceptions;
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
                    { "b:", "read buffer size in bytes", v => options.ReadBufferSize = (v != null ? int.Parse(v) : 1024 * 1024) },
                    { "mb:", "maximum read buffers in memory", v => options.MaxBuffers = (v != null ? int.Parse(v) : 1000) },
                };
                var commandSet = new CommandSet("archiver")
                {
                    "usage: archiver.exe COMMAND [arguments] [options]",
                    new Command("compress", "Compress input file to output file")
                    {
                        Options = optionSet,
                        Run = argv => new Compressor(options).Run()
                    },
                    new Command("decompress", "Decompress input file to output file")
                    {
                        Options = optionSet,
                        Run = argv => new Decompressor(options).Run()
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
