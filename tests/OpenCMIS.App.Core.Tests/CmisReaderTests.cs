using OpenCMIS.App.Core.Services;
using OpenCMIS.Protocol.Abstractions;
using OpenCMIS.Shared;
using Xunit;

namespace OpenCMIS.App.Core.Tests;

public sealed class CmisReaderTests
{
    [Fact]
    public async Task Status_reader_maps_state_ready_and_alert_flags()
    {
        var registers = new StubRegisterAccess()
            .Returns(0x00, CmisConstants.RegModuleState, [0x03])
            .Returns(0x00, CmisConstants.RegStatus, [0x01])
            .Returns(0x00, CmisConstants.RegInterruptFlags, [0x01, 0x04]);
        var reader = new CmisStatusService(registers, TimeProvider.System);

        var status = await reader.ReadAsync();

        Assert.Equal(ModuleState.Ready, status.CurrentState);
        Assert.True(status.IsReady);
        Assert.True(status.HasAlerts);
        Assert.Contains("Temperature high alarm", status.ActiveAlerts);
        Assert.Contains("TX fault", status.ActiveAlerts);
    }

    [Fact]
    public async Task Lane_reader_maps_flags_and_scaled_values()
    {
        var registers = new StubRegisterAccess()
            .Returns(0x10, CmisConstants.RegLaneStatusFlags, [0x03])
            .Returns(0x10, CmisConstants.RegLaneTxPowerMSB, [0x10, 0x27])
            .Returns(0x10, CmisConstants.RegLaneRxPowerMSB, [0x20, 0x4E])
            .Returns(0x10, CmisConstants.RegLaneTxBiasMSB, [0xF4, 0x01]);
        var reader = new CmisLaneReader(registers);

        var lanes = await reader.ReadAsync(1);

        Assert.Collection(lanes, lane =>
        {
            Assert.Equal(1, lane.LaneNumber);
            Assert.True(lane.IsEnabled);
            Assert.True(lane.HasFault);
            Assert.Equal(1.0000, lane.TxPower);
            Assert.Equal(2.0000, lane.RxPower);
            Assert.Equal(1.000, lane.TxBias);
        });
    }

    private sealed class StubRegisterAccess : IRegisterAccess
    {
        private readonly Dictionary<(byte Page, byte Offset, int Length), byte[]>
            _reads = [];

        public StubRegisterAccess Returns(
            byte page,
            byte offset,
            byte[] data)
        {
            _reads[(page, offset, data.Length)] = data;
            return this;
        }

        public async Task<byte> ReadByteAsync(byte page, byte address)
        {
            return (await ReadBlockAsync(page, address, 1))[0];
        }

        public Task WriteByteAsync(byte page, byte address, byte value)
        {
            return Task.CompletedTask;
        }

        public Task<byte[]> ReadBlockAsync(
            byte page,
            byte startAddress,
            int length)
        {
            return Task.FromResult(
                _reads[(page, startAddress, length)].ToArray());
        }

        public Task WriteBlockAsync(
            byte page,
            byte startAddress,
            byte[] data)
        {
            return Task.CompletedTask;
        }
    }
}
