using OpenCMIS.Module.Core.Msa;
using OpenCMIS.Module.Core.Tests.Fakes;
using OpenCMIS.Shared;
using OpenCMIS.Transport.Abstractions;
using Xunit;

namespace OpenCMIS.Module.Core.Tests;

public sealed class MsaMemoryAccessorTests
{
    private static readonly I2cDeviceAddress Address = new(0x50);
    private static readonly RegisterOffset Offset = new(0x80);

    [Fact]
    public async Task Read_selects_page_then_reads()
    {
        var bus = new ScriptedI2cRegisterBus();
        bus.QueueRead([0x11, 0x22]);
        await using var session = new OpticalModuleSession(bus);
        await session.OpenAsync();
        var accessor = new MsaMemoryAccessor(session);

        var result = await accessor.ReadAsync(
            Address,
            new ModulePage(0x11),
            Offset,
            2);

        Assert.Equal(new byte[] { 0x11, 0x22 }, result);
        Assert.Equal(
            new[] { "W 50:7F 11", "R 50:80 2" },
            bus.Operations);
    }

    [Fact]
    public async Task Write_selects_page_then_writes()
    {
        var bus = new ScriptedI2cRegisterBus();
        await using var session = new OpticalModuleSession(bus);
        await session.OpenAsync();
        var accessor = new MsaMemoryAccessor(session);

        await accessor.WriteAsync(
            Address,
            new ModulePage(0x01),
            Offset,
            new byte[] { 0x12, 0x34 });

        Assert.Equal(
            new[] { "W 50:7F 01", "W 50:80 1234" },
            bus.Operations);
    }

    [Fact]
    public async Task Concurrent_page_reads_cannot_interleave()
    {
        var bus = new ScriptedI2cRegisterBus();
        bus.QueueRead([0x01]);
        bus.QueueRead([0x02]);
        bus.PauseAfterFirstWrite();
        await using var session = new OpticalModuleSession(bus);
        await session.OpenAsync();
        var accessor = new MsaMemoryAccessor(session);

        var first = accessor.ReadAsync(
            Address,
            new ModulePage(0x01),
            Offset,
            1).AsTask();
        await bus.PauseObserved;
        var second = accessor.ReadAsync(
            Address,
            new ModulePage(0x02),
            Offset,
            1).AsTask();

        Assert.Equal(new[] { "W 50:7F 01" }, bus.Operations);
        bus.Resume();
        await Task.WhenAll(first, second);
        Assert.Equal(
            new[]
            {
                "W 50:7F 01",
                "R 50:80 1",
                "W 50:7F 02",
                "R 50:80 1"
            },
            bus.Operations);
    }

    [Fact]
    public async Task Read_rejects_range_past_end_of_page()
    {
        var bus = new ScriptedI2cRegisterBus();
        await using var session = new OpticalModuleSession(bus);
        await session.OpenAsync();
        var accessor = new MsaMemoryAccessor(session);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => accessor.ReadAsync(
                Address,
                new ModulePage(0),
                new RegisterOffset(0xFF),
                2).AsTask());

        Assert.Empty(bus.Operations);
    }

    [Fact]
    public async Task Read_reports_page_selection_failure()
    {
        var bus = new ScriptedI2cRegisterBus
        {
            NextWriteException = new CmisException(CmisErrorCode.I2cTransferFailed)
        };
        await using var session = new OpticalModuleSession(bus);
        await session.OpenAsync();
        var accessor = new MsaMemoryAccessor(session);

        var error = await Assert.ThrowsAsync<CmisException>(
            () => accessor.ReadAsync(
                Address,
                new ModulePage(0x11),
                Offset,
                1).AsTask());

        Assert.Equal(CmisErrorCode.MsaPageSelectionFailed, error.ErrorCode);
    }

    [Fact]
    public async Task Read_propagates_caller_cancellation()
    {
        var bus = new ScriptedI2cRegisterBus();
        await using var session = new OpticalModuleSession(bus);
        await session.OpenAsync();
        var accessor = new MsaMemoryAccessor(session);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => accessor.ReadAsync(
                Address,
                new ModulePage(0x11),
                Offset,
                1,
                cancellation.Token).AsTask());

        Assert.Empty(bus.Operations);
    }

    [Fact]
    public async Task Disposing_session_disposes_bus()
    {
        var bus = new ScriptedI2cRegisterBus();
        var session = new OpticalModuleSession(bus);

        await session.DisposeAsync();

        Assert.True(bus.IsDisposed);
    }
}
