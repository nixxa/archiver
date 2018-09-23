using Archiver.Collections;
using Archiver.Interfaces;
using Archiver.Threading;
using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using CancellationToken = Archiver.Threading.CancellationToken;

namespace Archiver
{
    /// <summary>
    /// Base class for all compressors/decompressors.
    /// </summary>
    public abstract class AbstractProcessor
    {
        protected Options Options { get; private set; }
        private ConcurrentQueue<IChunk> ReadChunksQueue { get; set; }
        private IndexedConcurrentQueue<IChunk> ExecutedChunksQueue { get; set; }

        private int _chunkIndex = 0;
        private bool _reading = false;
        private bool _executing = false;

        private Exception _exception;

        protected bool ErrorOccured => _exception != null;
        protected CancellationToken CancellationToken;

        public abstract CompressionType Type { get; }
        public abstract CompressionMode Mode { get; }

        public AbstractProcessor(Options options)
        {
            Options = options;
        }

        public virtual void Run()
        {
            CancellationToken = new CancellationToken();
            ReadChunksQueue = new ConcurrentQueue<IChunk>(CancellationToken, Options.MaxBuffers, Options.VerboseOutput);
            ExecutedChunksQueue = new IndexedConcurrentQueue<IChunk>(CancellationToken, Options.MaxBuffers, Options.VerboseOutput);

            var execThread = new ManagedThread(Execute, OnException);
            execThread.Start();
            var writeThread = new ManagedThread(Write, OnException);
            writeThread.Start();

            try
            {
                using (var inputStream = new FileStream(Options.Input, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    using (var bufferedStream = new BufferedStream(inputStream, Options.ReadBufferSize))
                    {
                        ReadStream(inputStream);
                    }
                }
            }
            catch (Exception e)
            {
                OnException(e);
            }

            execThread.Join();
            writeThread.Join();

            if (ErrorOccured)
            {
                throw _exception;
            }

            ReadChunksQueue = null;
            ExecutedChunksQueue = null;
        }

        protected void OnException(Exception e)
        {
            _exception = _exception != null ? _exception : e;
            if (_exception != null)
            {
                CancellationToken.Cancel(e);
            }
        }

        protected virtual void ReadStream(Stream inputStream)
        {
            _reading = true;
            while (true && !ErrorOccured)
            {
                IChunk chunk = ReadChunk(inputStream);
                if (chunk.Body.Length == 0)
                {
                    break;
                }
                ReadChunksQueue.Enqueue(chunk);
            }
            _reading = false;
        }

        protected virtual IChunk CreateChunk()
        {
            return new Chunk();
        }

        protected virtual IChunk ReadChunk(Stream inputStream)
        {
            var buffer = new byte[Options.ReadBufferSize];
            var count = inputStream.Read(buffer, 0, buffer.Length);
            if (count != buffer.Length)
            {
                Array.Resize(ref buffer, count);
            }
            var chunk = CreateChunk();
            chunk.Index = ++_chunkIndex;
            chunk.Body = buffer;
            if (Options.VerboseOutput)
            {
                Console.WriteLine("AbstractProcessor: chunk " + chunk.Index + " was read");
            }
            return chunk;
        }

        protected virtual void Execute()
        {
            ReadChunksQueue.WaitForInput();
            _executing = true;
            using (var threadPool = new GeneralThreadPool(CancellationToken, Options.MaxBuffers))
            {
                while ((_reading || ReadChunksQueue.Count > 0) && !ErrorOccured)
                {
                    IChunk chunk = null;
                    if (ReadChunksQueue.TryDequeue(out chunk))
                    {
                        threadPool.Enqueue(() =>
                        {
                            ExecuteChunk(chunk);
                            ExecutedChunksQueue.Enqueue(chunk);
                        });
                    }
                    else
                    {
                        Thread.Sleep(10);
                    }
                    
                }
                threadPool.StopAdding();
                threadPool.WaitForCompletion();
            }
            _executing = false;
        }

        protected abstract void ExecuteChunk(IChunk chunk);

        protected virtual void Write()
        {
            ExecutedChunksQueue.WaitForInput();
            using (var outputStream = new FileStream(Options.Output, FileMode.Create))
            {
                using (var bufferedSTream = new BufferedStream(outputStream, Options.ReadBufferSize))
                {
                    while ((_executing || ExecutedChunksQueue.Count > 0) && !ErrorOccured)
                    {
                        IChunk chunk = null;
                        if (ExecutedChunksQueue.TryDequeue(out chunk))
                        {
                            bufferedSTream.Write(chunk.Body, 0, chunk.Body.Length);
                            if (Options.VerboseOutput)
                            {
                                Console.WriteLine("AbstractProcessor: chunk " + chunk.Index + " was written");
                            }
                        }
                        else
                        {
                            Thread.Sleep(10);
                        }
                    }
                }
            }
        }
    }
}
