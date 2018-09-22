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
            var options = new Options();

            var optionSet = new OptionSet()
            {
                { "in=|input=", "input filename", v => options.Input = v },
                { "out=|output=", "output filename", v => options.Output = v },
                { "t|type:", "compression type", v => options.Type = ParseType(v) },
                { "mem:", "maximum memory for read buffers in megabytes", v => options.ReadingMemory = (v != null ? int.Parse(v) : -1) },
                { "v|verbose", "show detailed information during execution", v => options.VerboseOutput = (v != null) }
            };
            var commandSet = new CommandSet("archiver")
            {
                "usage: archiver.exe COMMAND [arguments] [options]",
                new Command("compress", "Compress input file to output file")
                {
                    Options = optionSet,
                    Run = argv => RunCommand(() =>
                    {
                        var factory = new ProcessorFactory(options);
                        var compressor = factory.CreateCompressor(options);
                        compressor.Run();
                    })
                },
                new Command("decompress", "Decompress input file to output file")
                {
                    Options = optionSet,
                    Run = argv => RunCommand(() =>
                    {
                        var factory = new ProcessorFactory(options);
                        var compressor = factory.CreateDecompressor(options);
                        compressor.Run();
                    })
                }
            };
            commandSet.Run(args);
        }

        static void RunCommand(Action action)
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        static CompressionType ParseType(string val)
        {
            var enumType = typeof(CompressionType);
            try
            {
                var result = (CompressionType)Enum.Parse(enumType, val, true);
                return result;
            }
            catch (Exception e)
            {
                Console.WriteLine("Type '" + val + "' is unknown. Using 'gzip' type instead");
                return CompressionType.GZip;
            }
        }
    }
}
