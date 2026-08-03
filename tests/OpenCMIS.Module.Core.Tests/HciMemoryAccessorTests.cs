using Microsoft.Extensions.Time.Testing;
using OpenCMIS.Module.Core.Hci;
using OpenCMIS.Module.Core.Msa;
using OpenCMIS.Module.Core.Tests.Fakes;
using OpenCMIS.Shared;
using OpenCMIS.Transport.Abstractions;
using Xunit;

namespace OpenCMIS.Module.Core.Tests
{
    public sealed class HciMemoryAccessorTests
    {
        private static readonly I2cDeviceAddress Address = new (0x50);
        private static readonly HciTableId       Table   = new (0xAE);
        private static readonly RegisterOffset   Offset  = new (0x0A);

        [Fact]
        public async Task Read_executes_complete_sequence_and_extracts_payload()
        {
            var bus = new ScriptedI2cRegisterBus();
            bus.QueueRead([0x01]);
            bus.QueueRead([0x00]);
            bus.QueueRead([0x00]);
            bus.QueueRead([0, 0, 0, 0, 0, 0, 0, 0, 0x12, 0x34]);
            var             time    = new FakeTimeProvider();
            await using var session = new OpticalModuleSession(bus);
            await session.OpenAsync();
            var accessor = new HciMemoryAccessor(
                    session,
                    new()
                    {InitialPollDelay = TimeSpan.Zero},
                    time);

            var data = await accessor.ReadAsync(Address, Table, Offset, 2);

            Assert.Equal(new byte[] {0x12, 0x34}, data);
            Assert.Equal(
                    new[]
                    {
                        "W 50:7F 7F",
                        "R 50:80 1",
                        "R 50:80 1",
                        "W 50:81 000000AE0A8002",
                        "W 50:80 7E",
                        "R 50:80 1",
                        "R 50:80 10"
                    },
                    bus.Operations);
        }

        [Fact]
        public async Task Write_executes_command_without_response_payload_read()
        {
            var bus = new ScriptedI2cRegisterBus();
            bus.QueueRead([0x00]);
            bus.QueueRead([0x00]);
            await using var session = new OpticalModuleSession(bus);
            await session.OpenAsync();
            var accessor = new HciMemoryAccessor(
                    session,
                    new (),
                    TimeProvider.System);

            await accessor.WriteAsync(
                    Address,
                    Table,
                    Offset,
                    new byte[] {0x12, 0x34});

            Assert.Equal(
                    new[]
                    {
                        "W 50:7F 7F",
                        "R 50:80 1",
                        "W 50:81 010000AE0A80021234",
                        "W 50:80 7E",
                        "R 50:80 1"
                    },
                    bus.Operations);
        }

        [Fact]
        public async Task Busy_status_times_out_using_virtual_time()
        {
            var bus = new ScriptedI2cRegisterBus();
            bus.QueueRead([0x01]);
            var             time    = new FakeTimeProvider();
            await using var session = new OpticalModuleSession(bus);
            await session.OpenAsync();
            var accessor = new HciMemoryAccessor(
                    session,
                    new()
                    {
                        Timeout          = TimeSpan.FromMilliseconds(10),
                        InitialPollDelay = TimeSpan.FromMilliseconds(10)
                    },
                    time);

            var operation = accessor.ReadAsync(Address, Table, Offset, 1).AsTask();
            await bus.ReadObserved;
            time.Advance(TimeSpan.FromMilliseconds(10));

            var error = await Assert.ThrowsAsync<CmisException>(() => operation);
            Assert.Equal(CmisErrorCode.HciCommandTimeout, error.ErrorCode);
        }

        [Fact]
        public async Task Busy_status_propagates_caller_cancellation()
        {
            var bus = new ScriptedI2cRegisterBus();
            bus.QueueRead([0x01]);
            await using var session = new OpticalModuleSession(bus);
            await session.OpenAsync();
            var accessor = new HciMemoryAccessor(
                    session,
                    new (),
                    TimeProvider.System);
            using var cancellation = new CancellationTokenSource();

            var operation = accessor.ReadAsync(
                    Address,
                    Table,
                    Offset,
                    1,
                    cancellation.Token).AsTask();
            await bus.ReadObserved;
            cancellation.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation);
        }

        [Fact]
        public async Task Msa_cannot_interleave_with_Hci()
        {
            var bus = new ScriptedI2cRegisterBus();
            bus.QueueRead([0x00]);
            bus.QueueRead([0x00]);
            bus.QueueRead([0, 0, 0, 0, 0, 0, 0, 0, 0x42]);
            bus.QueueRead([0x24]);
            bus.PauseAfterOperation("W 50:7F 7F");
            await using var session = new OpticalModuleSession(bus);
            await session.OpenAsync();
            var hci = new HciMemoryAccessor(
                    session,
                    new (),
                    TimeProvider.System);
            var msa = new MsaMemoryAccessor(session);

            var hciTask = hci.ReadAsync(Address, Table, Offset, 1).AsTask();
            await bus.PauseObserved;
            var msaTask = msa.ReadAsync(
                    Address,
                    new (0x11),
                    new (0x80),
                    1).AsTask();

            Assert.Equal(new[] {"W 50:7F 7F"}, bus.Operations);
            bus.Resume();
            await Task.WhenAll(hciTask, msaTask);
            Assert.Equal("W 50:7F 11", bus.Operations[^2]);
            Assert.Equal("R 50:80 1",  bus.Operations[^1]);
        }
    }
}
