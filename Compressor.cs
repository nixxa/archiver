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
        private const int WriteThreadsCount = 4;

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

        protected virtual void Write()
        {
            using (var outputStream = File.Exists(Options.Output)
                ? new FileStream(Options.Output, FileMode.Create)
                : File.Create(Options.Output))
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

        private void WriteBatch(IEnumerable<Chunk> batch, FileStream stream)
        {
            byte[] buffer = new byte[batch.Sum(b => b.Compressed.Length)];
            int position = 0;
            foreach (var item in batch.OrderBy(a => a.Index))
            {
                Array.Copy(item.Compressed, 0, buffer, position, item.Compressed.Length);
                position += item.Compressed.Length;
            }
            var waits = new List<EventWaitHandle>();
            var blocks = new int[WriteThreadsCount];
            for (int i = 0; i < WriteThreadsCount; i++)
            {
                blocks[i] = (i + 1) * (buffer.Length / WriteThreadsCount);
            }
            //blocks[blocks.Length - 1] = buffer.Length - ((WriteThreadsCount - 1) * (buffer.Length / WriteThreadsCount));

            for (int i = 0; i < WriteThreadsCount; i++)
            {
                var asyncState = new AsyncChunkState();
                waits.Add(asyncState.Completed);
                var n = i;
                ThreadPool.QueueUserWorkItem(
                    state =>
                    {
                        stream.Write(buffer, n > 0 ? blocks[n - 1] + 1 : 0,  n > 0 ? blocks[n] - blocks[n-1] : blocks[n]);
                        (state as IAsyncChunkState).Completed.Set();
                    }, asyncState);
                ;
            }
            WaitHandle.WaitAll(waits.ToArray());
        }
    }
}
