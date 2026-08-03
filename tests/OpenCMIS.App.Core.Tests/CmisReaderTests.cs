using OpenCMIS.App.Core.Services;
using OpenCMIS.Protocol.Abstractions;
using OpenCMIS.Shared;
using Xunit;

namespace OpenCMIS.App.Core.Tests
{
    public sealed class CmisReaderTests
    {
        [Fact]
        public async Task Status_reader_maps_state_ready_and_alert_flags()
        {
            var registers = new StubRegisterAccess()
                           .Returns(0x00, CmisConstants.RegModuleState,    [0x03])
                           .Returns(0x00, CmisConstants.RegStatus,         [0x01])
                           .Returns(0x00, CmisConstants.RegInterruptFlags, [0x01, 0x04])
                           .Returns(0x00, CmisConstants.RegModuleFlags,    [0x03, 0x00]);
            var reader = new CmisStatusService(registers, TimeProvider.System);

            var status = await reader.ReadAsync();

            Assert.Equal(ModuleState.Ready, status.CurrentState);
            Assert.True(status.IsReady);
            Assert.True(status.HasAlerts);
            Assert.Contains("Temperature high alarm", status.ActiveAlerts);
            Assert.Contains("TX fault",               status.ActiveAlerts);
        }

        [Fact]
        public async Task Lane_reader_maps_flags_and_scaled_values()
        {
            var registers = new StubRegisterAccess()
                           .Returns(0x10, CmisConstants.RegLaneStatusFlags, [0x03])
                           .Returns(0x10, CmisConstants.RegLaneTxPowerMSB,  [0x27, 0x10])
                           .Returns(0x10, CmisConstants.RegLaneRxPowerMSB,  [0x4E, 0x20])
                           .Returns(0x10, CmisConstants.RegLaneTxBiasMSB,   [0x01, 0xF4]);
            var reader = new CmisLaneReader(registers);

            var lanes = await reader.ReadAsync(1);

            Assert.Collection(lanes,
                              lane =>
                                  {
                                      Assert.Equal(1, lane.LaneNumber);
                                      Assert.True(lane.IsEnabled);
                                      Assert.True(lane.HasFault);
                                      Assert.Equal(1.0000, lane.TxPower);
                                      Assert.Equal(2.0000, lane.RxPower);
                                      Assert.Equal(1.000,  lane.TxBias);
                                  });
        }

        private sealed class StubRegisterAccess : IRegisterAccess
        {
            private readonly Dictionary<(byte Page, byte Offset, int Length), byte[]>
                    _reads = [];

            public async Task<byte> ReadByteAsync(byte page, byte address)
            {
                return (await ReadBlockAsync(page, address, 1))[0];
            }

            public Task WriteByteAsync(byte page, byte address, byte value)
            {
                return Task.CompletedTask;
            }

            public Task<byte[]> ReadBlockAsync(byte page,
                                               byte startAddress,
                                               int  length)
            {
                return Task.FromResult(
                        _reads[(page, startAddress, length)].ToArray());
            }

            public Task WriteBlockAsync(byte   page,
                                        byte   startAddress,
                                        byte[] data)
            {
                return Task.CompletedTask;
            }

            public StubRegisterAccess Returns(byte   page,
                                              byte   offset,
                                              byte[] data)
            {
                _reads[(page, offset, data.Length)] = data;
                return this;
            }
        }

        #region Byte-order verification (big-endian)
        [Fact]
        public void ParseTemperature_uses_big_endian_byte_order()
        {
            // CMIS 5.2: signed int16, LSB=1/256°C, MSB at lower address.
            // [0x01, 0x00] BE = 0x0100 = 256 → 256/256 = 1.00°C
            Assert.Equal(1.00, CmisMonitorReader.ParseTemperature([0x01, 0x00]));

            // [0x00, 0x01] BE = 0x0001 = 1 → 1/256 ≈ 0.00°C
            Assert.Equal(0.00, CmisMonitorReader.ParseTemperature([0x00, 0x01]));

            // [0xFF, 0xEC] BE = 0xFFEC = -20 → -20/256 ≈ -0.08°C
            Assert.Equal(-0.08, CmisMonitorReader.ParseTemperature([0xFF, 0xEC]));
        }

        [Fact]
        public void ParseVcc_uses_big_endian_byte_order()
        {
            // CMIS 5.2: unsigned int16, LSB=100µV.
            // [0x27, 0x10] BE = 0x2710 = 10000 → 10000*100/1M = 1.0000V
            Assert.Equal(1.0000, CmisMonitorReader.ParseVcc([0x27, 0x10]));

            // [0x80, 0xE8] BE = 0x80E8 = 33000 → 33000*100/1M = 3.3000V
            Assert.Equal(3.3000, CmisMonitorReader.ParseVcc([0x80, 0xE8]));
        }

        [Fact]
        public void ParseCurrent_uses_big_endian_byte_order()
        {
            // CMIS 5.2: unsigned int16, LSB=2µA.
            // [0x01, 0xF4] BE = 0x01F4 = 500 → 500*2/1000 = 1.000mA
            Assert.Equal(1.000, CmisMonitorReader.ParseCurrent([0x01, 0xF4]));

            // [0x03, 0xE8] BE = 0x03E8 = 1000 → 1000*2/1000 = 2.000mA
            Assert.Equal(2.000, CmisMonitorReader.ParseCurrent([0x03, 0xE8]));
        }

        [Fact]
        public void ParsePower_uses_big_endian_byte_order()
        {
            // CMIS 5.2: unsigned int16, LSB=0.1µW.
            // [0x27, 0x10] BE = 0x2710 = 10000 → 10000/10000 = 1.0000mW
            Assert.Equal(1.0000, CmisMonitorReader.ParsePower([0x27, 0x10]));

            // [0x4E, 0x20] BE = 0x4E20 = 20000 → 20000/10000 = 2.0000mW
            Assert.Equal(2.0000, CmisMonitorReader.ParsePower([0x4E, 0x20]));
        }
        #endregion
    }
}
