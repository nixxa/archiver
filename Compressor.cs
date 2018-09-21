using Archiver.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace Archiver
{
    public class Compressor : ICompressor
    {
        protected delegate void AsyncMethodCaller();

        protected readonly TimeSpan DefaultWriteTimeout = TimeSpan.FromSeconds(10);
        protected Options Options { get; private set; }
        protected ConcurrentQueue<Chunk> ChunksQueue { get; set; }
        protected bool IsReading { get; private set; }

        private int _chunkIndex = 0;
        private const int BatchSize = 10;
        private const int DefaultOutputBufferSize = 4096;

        public Compressor(Options options)
        {
            Options = options;
        }

        public virtual void Compress()
        {
            using (var inputStream = new FileStream(Options.Input, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                ChunksQueue = new ConcurrentQueue<Chunk>(Options.MaxBuffers);
                var caller = new AsyncMethodCaller(Write);
                IAsyncResult writeAsyncResult = caller.BeginInvoke(null, null);

                ReadStream(inputStream);

                caller.EndInvoke(writeAsyncResult);
                ChunksQueue = null;
            }
        }

        protected virtual void ReadStream(FileStream inputStream)
        {
            IsReading = true;
            while (true)
            {
                Chunk chunk = ReadChunk(inputStream);
                if (chunk.Count == 0)
                {
                    break;
                }
                ChunksQueue.Enqueue(chunk);
            }
            IsReading = false;
        }

        protected virtual Chunk ReadChunk(FileStream inputStream)
        {
            var chunk = new Chunk(++_chunkIndex, new byte[Options.ReadBufferSize], DefaultWriteTimeout);
            chunk.Count = inputStream.Read(chunk.Buffer, 0, chunk.Buffer.Length);
            return chunk;
        }

        internal void Decompress()
        {
            throw new NotImplementedException();
        }

        protected virtual void Write()
        {
            if (!File.Exists(Options.Output))
            {
                File.Create(Options.Output);
            }
            using (var outputStream = new FileStream(Options.Output, FileMode.Create))
            {
                while (IsReading || ChunksQueue.Count > 0)
                {
                    if (ChunksQueue.Count == 0)
                    {
                        Thread.Sleep(10);
                        continue;
                    }
                    var batch = ChunksQueue.Dequeue(BatchSize);
                    CompressBatch(batch);
                    WriteBatch(batch, outputStream);
                }
            }
        }

        private void CompressBatch(IEnumerable<Chunk> enumerable)
        {
            var waits = new List<EventWaitHandle>();
            foreach (var item in enumerable)
            {
                var asyncState = new AsyncChunkState();
                waits.Add(asyncState.Completed);
                ThreadPool.QueueUserWorkItem(state => CompressChunk(item, state as IAsyncChunkState), asyncState);
            }
            WaitHandle.WaitAll(waits.ToArray());
        }

        private void CompressChunk(Chunk chunk, IAsyncChunkState asyncState)
        {
            chunk.Compress();
            asyncState.Completed.Set();
        }

        private void WriteBatch(IEnumerable<Chunk> batch, Stream stream)
        {
            var orderedCollection = batch.OrderBy(a => a.Index).ToList();
            for(int i = 0; i < orderedCollection.Count; i++)
            {
                var chunk = orderedCollection[i];
                stream.Write(chunk.Compressed, 0, chunk.Compressed.Length);
            }
        }
    }
}
