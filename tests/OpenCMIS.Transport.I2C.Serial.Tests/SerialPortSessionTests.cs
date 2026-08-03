using OpenCMIS.Transport.I2C.Serial.Serial;
using Xunit;

namespace OpenCMIS.Transport.I2C.Serial.Tests
{
    public sealed class SerialPortSessionTests
    {
        [Fact]
        public async Task ReadExactlyAsync_combines_partial_reads()
        {
            await using var stream = new ChunkedReadStream(
                    [new byte[] {0x01}, new byte[] {0x02, 0x03}]);
            await using var session     = new SerialPortSession(stream);
            var             destination = new byte[3];

            await session.ReadExactlyAsync(destination, CancellationToken.None);

            Assert.Equal(new byte[] {0x01, 0x02, 0x03}, destination);
        }

        [Fact]
        public async Task ReadExactlyAsync_throws_when_stream_ends_early()
        {
            await using var stream  = new ChunkedReadStream([new byte[] {0x01}]);
            await using var session = new SerialPortSession(stream);

            await Assert.ThrowsAsync<EndOfStreamException>(() => session.ReadExactlyAsync(new byte[2], CancellationToken.None).AsTask());
        }

        [Fact]
        public async Task ReadExactlyAsync_propagates_cancellation()
        {
            await using var stream       = new BlockingReadStream();
            await using var session      = new SerialPortSession(stream);
            using var       cancellation = new CancellationTokenSource();
            cancellation.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => session.ReadExactlyAsync(new byte[1], cancellation.Token).AsTask());
        }

        private sealed class ChunkedReadStream(IReadOnlyList<byte[]> chunks) : Stream
        {
            private int _chunkIndex;

            public override bool CanRead  => true;
            public override bool CanSeek  => false;
            public override bool CanWrite => false;
            public override long Length   => throw new NotSupportedException();

            public override long Position
            {
                get => throw new NotSupportedException();
                set => throw new NotSupportedException();
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                return ReadAsync(buffer.AsMemory(offset, count)).AsTask().GetAwaiter().GetResult();
            }

            public override ValueTask<int> ReadAsync(Memory<byte>      buffer,
                                                     CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (_chunkIndex >= chunks.Count)
                    return ValueTask.FromResult(0);

                var chunk = chunks[_chunkIndex++];
                chunk.AsSpan().CopyTo(buffer.Span);
                return ValueTask.FromResult(chunk.Length);
            }

            public override void Flush() { }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotSupportedException();
            }

            public override void SetLength(long value)
            {
                throw new NotSupportedException();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new NotSupportedException();
            }
        }

        private sealed class BlockingReadStream : Stream
        {
            public override bool CanRead  => true;
            public override bool CanSeek  => false;
            public override bool CanWrite => false;
            public override long Length   => throw new NotSupportedException();

            public override long Position
            {
                get => throw new NotSupportedException();
                set => throw new NotSupportedException();
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                throw new NotSupportedException();
            }

            public override async ValueTask<int> ReadAsync(Memory<byte>      buffer,
                                                           CancellationToken cancellationToken = default)
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                return 0;
            }

            public override void Flush() { }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotSupportedException();
            }

            public override void SetLength(long value)
            {
                throw new NotSupportedException();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new NotSupportedException();
            }
        }
    }
}
