using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;

namespace Archiver
{
    public class ProcessorFactory
    {
        private readonly IEnumerable<AbstractProcessor> _processors;

        public ProcessorFactory(Options options)
        {
            _processors = Scan(options);
        }

        public AbstractProcessor CreateCompressor(Options options)
        {
            var compressor = _processors.FirstOrDefault(t => t.Type == options.Type && t.Mode == CompressionMode.Compress);
            if (compressor == null)
            {
                throw new NotSupportedException("Compression for type " + options.Type + " is not supported");
            }
            return compressor;
        }

        public AbstractProcessor CreateDecompressor(Options options)
        {
            var compressor = _processors.FirstOrDefault(t => t.Type == options.Type && t.Mode == CompressionMode.Decompress);
            if (compressor == null)
            {
                throw new NotSupportedException("Decompression for type " + options.Type + " is not supported");
            }
            return compressor;
        }

        private IEnumerable<AbstractProcessor> Scan(Options options)
        {
            var result = new List<AbstractProcessor>();
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                var types = assembly.GetTypes().Where(type => type.IsSubclassOf(typeof(AbstractProcessor)));
                result.AddRange(types.Select(type => Activator.CreateInstance(type, options)).Cast<AbstractProcessor>());
            }
            return result;
        }
    }
}
