using OpenCMIS.Module.Core;
using OpenCMIS.Module.Core.Msa;
using OpenCMIS.Protocol.Abstractions;
using OpenCMIS.Protocol.Core;
using OpenCMIS.Transport.Abstractions;
using Xunit;

namespace OpenCMIS.App.Core.Tests;

public sealed class BankedRegisterAccessTests
{
    [Fact]
    public async Task Read_block_forwards_bank_and_page()
    {
        var memory = new RecordingMsaMemoryAccessor([0xAA, 0xBB]);
        IRegisterAccess access = new RegisterAccess(
            memory,
            new I2cDeviceAddress(0x50));

        var result = await access.ReadBlockAsync(0x02, 0x11, 0x80, 2);

        Assert.Equal(new byte[] { 0xAA, 0xBB }, result);
        Assert.Equal(new ModulePage(0x02, 0x11), memory.LastPage);
        Assert.Equal(new RegisterOffset(0x80), memory.LastOffset);
    }

    [Fact]
    public async Task Write_block_forwards_bank_and_page()
    {
        var memory = new RecordingMsaMemoryAccessor([]);
        IRegisterAccess access = new RegisterAccess(
            memory,
            new I2cDeviceAddress(0x50));

        await access.WriteBlockAsync(0x03, 0x12, 0x82, [0x10, 0x20]);

        Assert.Equal(new ModulePage(0x03, 0x12), memory.LastPage);
        Assert.Equal(new RegisterOffset(0x82), memory.LastOffset);
        Assert.Equal(new byte[] { 0x10, 0x20 }, memory.LastWrite);
    }

    private sealed class RecordingMsaMemoryAccessor(byte[] readResult)
        : IMsaMemoryAccessor
    {
        public ModulePage? LastPage { get; private set; }

        public RegisterOffset? LastOffset { get; private set; }

        public byte[] LastWrite { get; private set; } = [];

        public ValueTask<byte[]> ReadAsync(
            I2cDeviceAddress device,
            ModulePage page,
            RegisterOffset offset,
            int length,
            CancellationToken cancellationToken = default)
        {
            LastPage = page;
            LastOffset = offset;
            return ValueTask.FromResult(readResult);
        }

        public ValueTask WriteAsync(
            I2cDeviceAddress device,
            ModulePage page,
            RegisterOffset offset,
            ReadOnlyMemory<byte> data,
            CancellationToken cancellationToken = default)
        {
            LastPage = page;
            LastOffset = offset;
            LastWrite = data.ToArray();
            return ValueTask.CompletedTask;
        }
    }
}
