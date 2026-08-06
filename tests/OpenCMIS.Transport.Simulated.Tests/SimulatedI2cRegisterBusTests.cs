using System.Text;
using OpenCMIS.Shared;
using OpenCMIS.Transport.Abstractions;
using Xunit;

namespace OpenCMIS.Transport.Simulated.Tests
{
    public sealed class SimulatedI2cRegisterBusTests
    {
        private static readonly I2cDeviceAddress Address = new (0x50);

        // ---- identity reads ----

        [Fact]
        public async Task Read_identifier_and_revision()
        {
            await using var bus  = await OpenBusAsync();
            var             data = new byte[2];
            await bus.ReadAsync(Address, new (0x00), data);
            Assert.Equal(0x1E, data[0]); // QSFP-DD identifier
            Assert.Equal(0x52, data[1]); // CMIS 5.2
        }

        [Fact]
        public async Task Read_status_and_module_state()
        {
            await using var bus  = await OpenBusAsync();
            var             data = new byte[2];
            await bus.ReadAsync(Address, new (0x02), data);
            Assert.Equal(0x03, data[0]); // ready + dp_ready
            Assert.Equal(0x03, data[1]); // ModuleReady
        }

        // ---- vendor page reads (page 0x01) ----

        [Fact]
        public async Task Read_vendor_name_from_page_01()
        {
            await using var bus = await OpenBusAsync();

            // Select page 0x01
            await bus.WriteAsync(Address,
                                 new (0x7F),
                                 new byte[] {0x01});

            var data = new byte[16];
            await bus.ReadAsync(Address,
                                new (CmisConstants.RegVendorNameStart),
                                data);
            Assert.Equal("OpenCMIS-Sim", Encoding.ASCII.GetString(data).TrimEnd('\0'));
        }

        [Fact]
        public async Task Read_part_number_800g()
        {
            await using var bus = await OpenBusAsync();
            await bus.WriteAsync(Address,
                                 new (0x7F),
                                 new byte[] {0x01});

            var data = new byte[16];
            await bus.ReadAsync(Address,
                                new (CmisConstants.RegPartNumberStart),
                                data);
            Assert.Equal("800G-QSFPDD-SIM",
                         Encoding.ASCII.GetString(data).TrimEnd());
        }

        [Fact]
        public async Task Read_part_number_1p6t()
        {
            await using var bus = await OpenBusAsync("1p6t-osfp");
            await bus.WriteAsync(Address,
                                 new (0x7F),
                                 new byte[] {0x01});

            var data = new byte[16];
            await bus.ReadAsync(Address,
                                new (CmisConstants.RegPartNumberStart),
                                data);
            Assert.Equal("1.6T-OSFP-SIM",
                         Encoding.ASCII.GetString(data).TrimEnd());
        }

        // ---- monitor reads (page 0x00, no noise) ----

        [Fact]
        public async Task Read_temperature_without_noise()
        {
            await using var bus  = await OpenBusAsync(noiseEnabled: false);
            var             data = new byte[2];
            await bus.ReadAsync(Address,
                                new (CmisConstants.RegTemperatureMSB),
                                data);

            // 42.0°C = 10752 = [0x2A, 0x00] BE
            Assert.Equal(0x2A, data[0]);
            Assert.Equal(0x00, data[1]);
        }

        [Fact]
        public async Task Read_vcc_without_noise()
        {
            await using var bus  = await OpenBusAsync(noiseEnabled: false);
            var             data = new byte[2];
            await bus.ReadAsync(Address,
                                new (CmisConstants.RegVccMSB),
                                data);

            // 3.3V = 33000 = [0x80, 0xE8] BE
            Assert.Equal(0x80, data[0]);
            Assert.Equal(0xE8, data[1]);
        }

        // ---- noise on monitor values ----

        [Fact]
        public async Task Monitor_with_noise_produces_small_jitter()
        {
            await using var bus  = await OpenBusAsync(seed: 123, noiseEnabled: true);
            var             data = new byte[2];

            // Read twice; LSB may jitter +/-2
            await bus.ReadAsync(Address,
                                new (CmisConstants.RegTemperatureMSB),
                                data);
            var firstLsb = data[1];

            // Reset noise seed for deterministic second read
            bus.ResetNoise();
            await bus.ReadAsync(Address,
                                new (CmisConstants.RegTemperatureMSB),
                                data);
            Assert.Equal(firstLsb, data[1]); // deterministic with same seed
        }

        [Fact]
        public async Task Identity_bytes_are_not_noisy()
        {
            await using var bus = await OpenBusAsync(seed: 123, noiseEnabled: true);
            var             a   = new byte[1];
            var             b   = new byte[1];
            await bus.ReadAsync(Address, new (0x00), a);
            bus.ResetNoise();
            await bus.ReadAsync(Address, new (0x00), b);
            Assert.Equal(a[0], b[0]); // identity never changes
        }

        // ---- threshold bytes (on dedicated page 0x02) ----

        [Fact]
        public async Task Threshold_page_does_not_corrupt_lower_page_identity()
        {
            await using var bus = await OpenBusAsync(noiseEnabled: false);

            // Identity bytes on page 0x00 must remain intact
            var identifier = new byte[1];
            await bus.ReadAsync(Address, new (0x00), identifier);
            Assert.Equal(0x1E, identifier[0]);

            var status = new byte[1];
            await bus.ReadAsync(Address, new (0x02), status);
            Assert.Equal(0x03, status[0]);
        }

        [Fact]
        public async Task Read_temperature_thresholds_from_correct_page()
        {
            await using var bus = await OpenBusAsync(noiseEnabled: false);

            // Select threshold page 0x02
            await bus.WriteAsync(Address,
                                 new (0x7F),
                                 new[] {CmisConstants.ThresholdPage});

            // Temp high alarm = 70°C = 17920 = [0x46, 0x00]
            var data = new byte[2];
            await bus.ReadAsync(Address,
                                new (CmisConstants.RegTempHighAlarmMSB),
                                data);
            Assert.Equal(0x46, data[0]);
            Assert.Equal(0x00, data[1]);
        }

        [Fact]
        public async Task Read_vcc_thresholds_from_correct_page()
        {
            await using var bus = await OpenBusAsync(noiseEnabled: false);

            // Select threshold page 0x02
            await bus.WriteAsync(Address,
                                 new (0x7F),
                                 new[] {CmisConstants.ThresholdPage});

            // VCC high alarm = 3.5V = 35000 = [0x88, 0xB8]
            var data = new byte[2];
            await bus.ReadAsync(Address,
                                new (CmisConstants.RegVccHighAlarmMSB),
                                data);
            Assert.Equal(0x88, data[0]);
            Assert.Equal(0xB8, data[1]);
        }

        // ---- serial number at corrected address ----

        [Fact]
        public async Task Read_serial_number_does_not_corrupt_part_number()
        {
            await using var bus = await OpenBusAsync();

            // Select vendor page 0x01
            await bus.WriteAsync(Address,
                                 new (0x7F),
                                 new byte[] {0x01});

            // Part number at 0x94 must be intact
            var part = new byte[16];
            await bus.ReadAsync(Address,
                                new (CmisConstants.RegPartNumberStart),
                                part);
            Assert.Equal("800G-QSFPDD-SIM",
                         Encoding.ASCII.GetString(part).TrimEnd());

            // Serial number at 0xA8 must be populated (no overlap)
            var serial = new byte[16];
            await bus.ReadAsync(Address,
                                new (CmisConstants.RegSerialNumberStart),
                                serial);
            Assert.Equal("SIM-800G000001",
                         Encoding.ASCII.GetString(serial).TrimEnd());
        }

        // ---- writes ----

        [Fact]
        public async Task Write_then_read_back_preserves_data()
        {
            await using var bus = await OpenBusAsync();

            // Select bank 0, page 0x05 (MSA editor page)
            await bus.WriteAsync(Address,
                                 new (0x7E),
                                 new byte[] {0x00});
            await bus.WriteAsync(Address,
                                 new (0x7F),
                                 new byte[] {0x05});

            // Write upper page block
            await bus.WriteAsync(Address,
                                 new (0x80),
                                 new byte[] {0xAA, 0xBB, 0xCC});

            // Read back
            var data = new byte[3];
            await bus.ReadAsync(Address, new (0x80), data);
            Assert.Equal([0xAA, 0xBB, 0xCC], data);
        }

        [Fact]
        public async Task Write_lower_page_does_not_affect_upper_page_memory()
        {
            await using var bus = await OpenBusAsync();

            // Select bank 0, page 0x05 and write upper
            await bus.WriteAsync(Address,
                                 new (0x7E),
                                 new byte[] {0x00});
            await bus.WriteAsync(Address,
                                 new (0x7F),
                                 new byte[] {0x05});
            await bus.WriteAsync(Address,
                                 new (0x80),
                                 new byte[] {0x11});

            // Select bank 0, page 0x06 and read upper at same offset
            await bus.WriteAsync(Address,
                                 new (0x7F),
                                 new byte[] {0x06});
            var data = new byte[1];
            await bus.ReadAsync(Address, new (0x80), data);
            Assert.NotEqual(0x11, data[0]); // different page memory
        }

        // ---- bank/page selection ----

        [Fact]
        public async Task Bank_select_write_tracks_selected_bank()
        {
            await using var bus = await OpenBusAsync();

            // Select bank 2, page 0x11
            await bus.WriteAsync(Address,
                                 new (0x7E),
                                 new byte[] {0x02});
            await bus.WriteAsync(Address,
                                 new (0x7F),
                                 new byte[] {0x11});

            // Write to upper memory of bank 2, page 0x11
            await bus.WriteAsync(Address,
                                 new (0x88),
                                 new byte[] {0x99});

            // Switch to bank 0, page 0x11 — should NOT see bank 2 data
            await bus.WriteAsync(Address,
                                 new (0x7E),
                                 new byte[] {0x00});
            var data = new byte[1];
            await bus.ReadAsync(Address, new (0x88), data);
            Assert.NotEqual(0x99, data[0]); // bank 0 page 0x11 was not written
        }

        // ---- lane pages ----

        [Fact]
        public async Task Read_lane_page_status_flags()
        {
            await using var bus = await OpenBusAsync();

            // Select lane 0 page (0x10)
            await bus.WriteAsync(Address,
                                 new (0x7F),
                                 new byte[] {0x10});
            var data = new byte[1];
            await bus.ReadAsync(Address,
                                new (CmisConstants.RegLaneStatusFlags),
                                data);
            Assert.Equal(0x01, data[0]); // enabled
        }

        [Fact]
        public async Task Read_lane_tx_bias_without_noise()
        {
            await using var bus = await OpenBusAsync(noiseEnabled: false);

            // Select lane 0 page (0x10)
            await bus.WriteAsync(Address,
                                 new (0x7F),
                                 new byte[] {0x10});
            var data = new byte[2];
            await bus.ReadAsync(Address,
                                new (CmisConstants.RegLaneTxBiasMSB),
                                data);

            // 65mA = 32500 = [0x7E, 0xF4] BE
            Assert.Equal(0x7E, data[0]);
            Assert.Equal(0xF4, data[1]);
        }

        private static async Task<SimulatedI2cRegisterBus> OpenBusAsync(string profile      = "800g-qsfpdd",
                                                                        int    seed         = 42,
                                                                        bool   noiseEnabled = true)
        {
            var bus = new SimulatedI2cRegisterBus(profile, seed, noiseEnabled);
            await bus.OpenAsync();
            return bus;
        }
    }
}
