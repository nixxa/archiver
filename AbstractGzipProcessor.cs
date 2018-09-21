using Archiver.Interfaces;
using Archiver.Threading;
using System;
using System.IO;
using System.Threading;

namespace Archiver
{
    public abstract class AbstractGzipProcessor
    {
        protected Options Options { get; private set; }
        protected ConcurrentQueue<Chunk> ReadChunksQueue { get; set; }
        protected SortedConcurrentQueue<Chunk> ExecutedChunksQueue { get; set; }

        private int _chunkIndex = 0;
        private const int DefaultOutputBufferSize = 4096;
        private bool _reading = false;
        private bool _executing = false;

        public AbstractGzipProcessor(Options options)
        {
            Options = options;
        }

        public virtual void Run()
        {
            ReadChunksQueue = new ConcurrentQueue<Chunk>(Options.MaxBuffers);
            ExecutedChunksQueue = new SortedConcurrentQueue<Chunk>();

            var execThread = new Thread(Execute);
            execThread.Start();
            var writeThread = new Thread(Write);
            writeThread.Start();

            using (var inputStream = new FileStream(Options.Input, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                ReadStream(inputStream);
            }

            execThread.Join();
            writeThread.Join();
            ReadChunksQueue = null;
            ExecutedChunksQueue = null;
        }

        protected virtual void ReadStream(FileStream inputStream)
        {
            _reading = true;
            while (true)
            {
                Chunk chunk = ReadChunk(inputStream);
                if (chunk.InputLength == 0)
                {
                    break;
                }
                ReadChunksQueue.Enqueue(chunk);
            }
            _reading = false;
        }

        protected virtual Chunk ReadChunk(FileStream inputStream)
        {
            var chunk = new Chunk(++_chunkIndex, new byte[Options.ReadBufferSize]);
            chunk.InputLength = inputStream.Read(chunk.Input, 0, chunk.Input.Length);
            return chunk;
        }

        protected virtual void Execute()
        {
            _executing = true;
            using (var threadPool = new GeneralThreadPool())
            {
                while (_reading || ReadChunksQueue.Count > 0)
                {
                    if (ReadChunksQueue.Count == 0)
                    {
                        Thread.Sleep(10);
                        continue;
                    }
                    var chunk = ReadChunksQueue.Dequeue();
                    threadPool.Enqueue(() =>
                    {
                        ExecuteChunk(chunk);
                        ExecutedChunksQueue.Enqueue(chunk);
                    });
                }
                threadPool.StopAdding();
                threadPool.WaitForCompletion();
            }
            _executing = false;
        }

        protected abstract void ExecuteChunk(Chunk chunk);

        protected virtual void Write()
        {
            using (var outputStream = new FileStream(Options.Output, FileMode.Create))
            {
                while (_executing || ExecutedChunksQueue.Count > 0)
                {
                    if (ExecutedChunksQueue.Count == 0)
                    {
                        Thread.Sleep(10);
                        continue;
                    }
                    var chunk = ExecutedChunksQueue.Dequeue();
                    outputStream.Write(chunk.Output, 0, chunk.Output.Length);
                }
            }
        }
    }
}
