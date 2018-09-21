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
                var options = ParseArguments(args);
                var compressor = new Compressor(options);
                compressor.Compress();
            }
            catch (ParseException pe)
            {
                Console.WriteLine(pe.Message);
            }
        }

        static Options ParseArguments(string[] args)
        {
            if (args.Length < 2)
            {
                throw new ParseException("Using: archiver.exe <inputFilename> <outputFilename> [options]");
            }

            string inputFilename = args[0];
            string outputFilename = args[1];
            if (!File.Exists(inputFilename))
            {
                throw new ParseException("Input '" + inputFilename + "' not exists");
            }
            if (string.IsNullOrEmpty(outputFilename))
            {
                throw new ParseException("Output filename must be specified");
            }
            var options = new Options
            {
                Input = inputFilename,
                Output = outputFilename
            };

            var optionSet = new OptionSet()
            {
                { "b=", v => options.ReadBufferSize = (v != null ? int.Parse(v) : 1024 * 1024) },
                { "mb=", v => options.MaxBuffers = (v != null ? int.Parse(v) : 1000) },
            };
            optionSet.Parse(args);
            return options;
        }
    }
}
