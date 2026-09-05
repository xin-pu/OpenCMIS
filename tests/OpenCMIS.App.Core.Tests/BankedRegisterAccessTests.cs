using OpenCMIS.Module.Core;
using OpenCMIS.Module.Core.Msa;
using OpenCMIS.Protocol.Abstractions;
using OpenCMIS.Protocol.Core;
using OpenCMIS.Transport.Abstractions;
using Xunit;

namespace OpenCMIS.App.Core.Tests
{
    public sealed class BankedRegisterAccessTests
    {
        [Theory]
        [InlineData(0x20)]
        [InlineData(0x21)]
        [InlineData(0x22)]
        [InlineData(0x23)]
        public async Task Descriptor_pages_reject_byte_block_and_banked_writes_before_memory_access(byte page)
        {
            var memory = new RecordingMsaMemoryAccessor([]);
            IRegisterAccess access = new RegisterAccess(memory, new(0x50));
            await Assert.ThrowsAsync<NotSupportedException>(() => access.WriteByteAsync(page, 0x80, 1));
            await Assert.ThrowsAsync<NotSupportedException>(() => access.WriteBlockAsync(page, 0x80, [1, 2]));
            await Assert.ThrowsAsync<NotSupportedException>(() => access.WriteBlockAsync(1, page, 0x80, [1, 2]));
            Assert.Null(memory.LastPage);
            Assert.Empty(memory.LastWrite);
        }

        [Theory]
        [InlineData(0x1F)]
        [InlineData(0x24)]
        public async Task Neighboring_pages_allow_byte_and_block_writes(byte page)
        {
            var memory = new RecordingMsaMemoryAccessor([]);
            IRegisterAccess access = new RegisterAccess(memory, new(0x50));
            await access.WriteByteAsync(page, 0x80, 0xAB);
            Assert.Equal(new byte[] { 0xAB }, memory.LastWrite);
            await access.WriteBlockAsync(page, 0x80, [0xCD, 0xEF]);
            Assert.Equal(new byte[] { 0xCD, 0xEF }, memory.LastWrite);
        }

        [Theory]
        [InlineData(0x20)]
        [InlineData(0x21)]
        [InlineData(0x22)]
        [InlineData(0x23)]
        public async Task Legacy_descriptor_writes_do_not_switch_pages_or_write_transport(byte page)
        {
            var transport = new RecordingLegacyAccess();
#pragma warning disable CS0618
            IRegisterAccess access = new RegisterAccess(transport, transport);
#pragma warning restore CS0618
            await Assert.ThrowsAsync<NotSupportedException>(() => access.WriteByteAsync(page, 0x80, 1));
            await Assert.ThrowsAsync<NotSupportedException>(() => access.WriteBlockAsync(page, 0x80, [1]));
            await Assert.ThrowsAsync<NotSupportedException>(() => access.WriteBlockAsync(1, page, 0x80, [1]));
            Assert.Equal(0, transport.Writes);
            Assert.Equal(0, transport.PageSwitches);
            await access.WriteByteAsync(0x24, 0x80, 1);
            await access.WriteBlockAsync(0x24, 0x80, [1]);
            Assert.Equal(2, transport.Writes);
        }

        private sealed class RecordingLegacyAccess : IRegisterTransport, IPageManager
        {
            public int Writes { get; private set; }
            public int PageSwitches { get; private set; }
            public bool IsConnected => true;
            public byte CurrentPage { get; private set; }
            public Task SwitchPageAsync(byte page) { CurrentPage = page; PageSwitches++; return Task.CompletedTask; }
            public Task ResetAsync() => Task.CompletedTask;
            public Task<bool> OpenAsync() => Task.FromResult(true);
            public Task CloseAsync() => Task.CompletedTask;
            public Task<byte[]> ReadAsync(int length) => Task.FromResult(new byte[length]);
            public Task WriteAsync(byte[] data) { Writes++; return Task.CompletedTask; }
            public Task<byte> ReadRegisterAsync(byte address) => Task.FromResult((byte)0);
            public Task<byte[]> ReadRegisterBlockAsync(byte address, int length) => ReadAsync(length);
            public Task WriteRegisterAsync(byte address, byte value) => WriteAsync([value]);
            public Task WriteRegisterBlockAsync(byte address, byte[] data) => WriteAsync(data);
            public void Dispose() { }
        }

        [Fact]
        public async Task Read_block_forwards_bank_and_page()
        {
            var memory = new RecordingMsaMemoryAccessor([0xAA, 0xBB]);
            IRegisterAccess access = new RegisterAccess(
                    memory,
                    new (0x50));

            var result = await access.ReadBlockAsync(0x02, 0x11, 0x80, 2);

            Assert.Equal(new byte[] {0xAA, 0xBB},    result);
            Assert.Equal(new ModulePage(0x02, 0x11), memory.LastPage);
            Assert.Equal(new RegisterOffset(0x80),   memory.LastOffset);
        }

        [Fact]
        public async Task Write_block_forwards_bank_and_page()
        {
            var memory = new RecordingMsaMemoryAccessor([]);
            IRegisterAccess access = new RegisterAccess(
                    memory,
                    new (0x50));

            await access.WriteBlockAsync(0x03, 0x12, 0x82, [0x10, 0x20]);

            Assert.Equal(new ModulePage(0x03, 0x12), memory.LastPage);
            Assert.Equal(new RegisterOffset(0x82),   memory.LastOffset);
            Assert.Equal(new byte[] {0x10, 0x20},    memory.LastWrite);
        }

        private sealed class RecordingMsaMemoryAccessor(byte[] readResult)
                : IMsaMemoryAccessor
        {
            public ModulePage? LastPage { get; private set; }

            public RegisterOffset? LastOffset { get; private set; }

            public byte[] LastWrite { get; private set; } = [];

            public ValueTask<byte[]> ReadAsync(I2cDeviceAddress  device,
                                               ModulePage        page,
                                               RegisterOffset    offset,
                                               int               length,
                                               CancellationToken cancellationToken = default)
            {
                LastPage   = page;
                LastOffset = offset;
                return ValueTask.FromResult(readResult);
            }

            public ValueTask WriteAsync(I2cDeviceAddress     device,
                                        ModulePage           page,
                                        RegisterOffset       offset,
                                        ReadOnlyMemory<byte> data,
                                        CancellationToken    cancellationToken = default)
            {
                LastPage   = page;
                LastOffset = offset;
                LastWrite  = data.ToArray();
                return ValueTask.CompletedTask;
            }
        }
    }
}
