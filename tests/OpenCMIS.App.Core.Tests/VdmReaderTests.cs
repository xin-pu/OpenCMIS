using OpenCMIS.Protocol.Abstractions.Models;
using OpenCMIS.Protocol.Abstractions;
using OpenCMIS.App.Core.Services;
using OpenCMIS.App.Core;
using OpenCMIS.Shared;
using OpenCMIS.Transport.Abstractions;
using Xunit;

namespace OpenCMIS.App.Core.Tests;

public sealed class VdmReaderTests
{
    [Fact]
    public async Task Device_reads_descriptor_driven_diagnostics_from_its_register_access()
    {
        var registers = new SimulatedRegisters();
        registers.Set(0x01, 0x8E, [0x40]);
        registers.Set(0x20, 0x80, [0x12, 0x34]);
        registers.Set(0x24, 0x80, [0xAB, 0xCD]);
        var flagPage = new byte[128];
        flagPage[0] = 0x0F;
        registers.Set(0x2C, 0x80, flagPage);
        var device = new CmisDevice(
            new DeviceInfo { Id = "test", Name = "Test module" },
            new ConnectedDeviceConnection(),
            registers);

        var diagnostics = await device.ReadVdmDiagnosticsAsync();

        var observable = Assert.Single(diagnostics.ObservableInstances);
        Assert.Equal(1, observable.Instance);
        Assert.Equal(new byte[] { 0x12, 0x34 }, observable.Descriptor);
        Assert.Equal((ushort)0xABCD, observable.Sample);
        Assert.True(observable.Flags.HighAlarm);
        Assert.True(observable.Flags.HighWarning);
        Assert.True(observable.Flags.LowWarning);
        Assert.True(observable.Flags.LowAlarm);
    }

    [Fact]
    public void DescriptorSampleAndFlagsAreRetainedForInstanceOne()
    {
        var descriptor = new byte[] { 0x12, 0x34 };
        var flags = new VdmObservableFlags
        {
            HighAlarm = true,
            HighWarning = false,
            LowWarning = true,
            LowAlarm = false
        };
        var diagnostics = new VdmDiagnostics
        {
            ObservableInstances =
            [
                new VdmObservable
                {
                    Instance = 1,
                    Descriptor = descriptor,
                    Sample = 0xABCD,
                    Flags = flags
                }
            ]
        };

        var observable = Assert.Single(diagnostics.ObservableInstances);
        descriptor[0] = 0xFF;
        Assert.Equal(1, observable.Instance);
        Assert.Equal(new byte[] { 0x12, 0x34 }, observable.Descriptor);
        Assert.Equal((ushort)0xABCD, observable.Sample);
        Assert.Same(flags, observable.Flags);
    }

    [Fact]
    public async Task Reader_gates_on_capability_and_maps_descriptor_sample_and_flags()
    {
        var registers = new SimulatedRegisters();
        registers.Set(0x01, 0x8E, [0x40]);
        registers.Set(0x20, 0x80, [0x12, 0x34]);
        registers.Set(0x24, 0x80, [0xAB, 0xCD]);
        var flagPage = new byte[128];
        flagPage[0] = 0x0F;
        registers.Set(0x2C, 0x80, flagPage);

        var diagnostics = await new VdmReader(registers).ReadAsync();

        var observable = Assert.Single(diagnostics.ObservableInstances);
        Assert.Equal(1, observable.Instance);
        Assert.Equal(new byte[] { 0x12, 0x34 }, observable.Descriptor);
        Assert.Equal((ushort)0xABCD, observable.Sample);
        Assert.True(observable.Flags.HighAlarm);
        Assert.True(observable.Flags.HighWarning);
        Assert.True(observable.Flags.LowWarning);
        Assert.True(observable.Flags.LowAlarm);
    }

    [Fact]
    public async Task Zero_type_id_is_unused_even_when_even_descriptor_byte_is_nonzero()
    {
        var registers = new SimulatedRegisters();
        registers.Set(0x01, 0x8E, [0x40]);
        registers.Set(0x20, 0x80, [0xF0, 0x00, 0x00, 0x12]);
        var result = await new VdmReader(registers).ReadAsync();
        Assert.Equal(2, Assert.Single(result.ObservableInstances).Instance);
    }

    [Theory]
    [InlineData(0x01, true, false, false, false)]
    [InlineData(0x02, false, true, false, false)]
    [InlineData(0x04, false, false, true, false)]
    [InlineData(0x08, false, false, false, true)]
    public async Task First_instance_uses_low_nibble_in_HA_LA_HW_LW_order(
        byte flag, bool ha, bool la, bool hw, bool lw)
    {
        var registers = new SimulatedRegisters();
        registers.Set(0x01, 0x8E, [0x40]);
        registers.Set(0x20, 0x80, [0x00, 0x01, 0x00, 0x02]);
        registers.Set(0x2C, 0x80, [flag]);
        var result = await new VdmReader(registers).ReadAsync();
        var first = result.ObservableInstances[0].Flags;
        Assert.Equal(ha, first.HighAlarm);
        Assert.Equal(la, first.LowAlarm);
        Assert.Equal(hw, first.HighWarning);
        Assert.Equal(lw, first.LowWarning);
        var second = result.ObservableInstances[1].Flags;
        Assert.False(second.HighAlarm);
        Assert.False(second.LowAlarm);
        Assert.False(second.HighWarning);
        Assert.False(second.LowWarning);
    }

    [Fact]
    public async Task Adjacent_instances_use_independent_nibbles()
    {
        var registers = new SimulatedRegisters();
        registers.Set(0x01, 0x8E, [0x40]);
        registers.Set(0x20, 0x80, [0x00, 0x01, 0x00, 0x02]);
        registers.Set(0x2C, 0x80, [0x81]);
        var result = await new VdmReader(registers).ReadAsync();
        Assert.True(result.ObservableInstances[0].Flags.HighAlarm);
        Assert.False(result.ObservableInstances[0].Flags.LowWarning);
        Assert.False(result.ObservableInstances[1].Flags.HighAlarm);
        Assert.True(result.ObservableInstances[1].Flags.LowWarning);
    }

    [Fact]
    public async Task Only_advertised_group_is_read_and_reserved_advertisement_bits_are_ignored()
    {
        var registers = new SimulatedRegisters();
        registers.Set(0x01, 0x8E, [0x40]);
        registers.Set(0x2F, 0x80, [0xFC]);
        await new VdmReader(registers).ReadAsync();
        Assert.Contains((0x2F, 0x80), registers.Reads);
        Assert.Contains((0x20, 0x80), registers.Reads);
        Assert.Contains((0x24, 0x80), registers.Reads);
        Assert.DoesNotContain(registers.Reads, r => r.Page is 0x21 or 0x22 or 0x23 or 0x25 or 0x26 or 0x27);
    }

    [Theory]
    [InlineData(0x01)]
    [InlineData(0x2F)]
    public async Task Failed_capability_or_advertisement_is_unavailable_without_further_reads(byte page)
    {
        var registers = new SimulatedRegisters();
        registers.Set(0x01, 0x8E, [0x40]);
        registers.FailedPages.Add(page);
        var result = await new VdmReader(registers).ReadAsync();
        Assert.Equal(VdmReadStatus.Unavailable, result.ReadStatus);
        Assert.Empty(result.ObservableInstances);
        Assert.DoesNotContain(registers.Reads, r => r.Page is >= 0x20 and <= 0x2C);
    }

    [Theory]
    [InlineData(0x20, false)]
    [InlineData(0x20, true)]
    [InlineData(0x24, false)]
    [InlineData(0x24, true)]
    [InlineData(0x2C, false)]
    [InlineData(0x2C, true)]
    public async Task Short_or_failed_page_preserves_partial_status_and_unknown_values(byte page, bool fails)
    {
        var registers = new SimulatedRegisters();
        registers.Set(0x01, 0x8E, [0x40]);
        var descriptors = new byte[128];
        descriptors[1] = 1;
        descriptors[3] = 2;
        descriptors[5] = 3;
        registers.Set(0x20, 0x80, descriptors);
        if (fails) registers.FailedPages.Add(page);
        else registers.Set(page, 0x80, page == 0x20 ? [0xF0, 1, 0x00] : [0]);

        var result = await new VdmReader(registers).ReadAsync();

        Assert.True(result.IsSupported);
        Assert.Equal(VdmReadStatus.Partial, result.ReadStatus);
        if (page == 0x20)
        {
            Assert.Equal(fails ? 0 : 1, result.ObservableInstances.Count);
        }
        else if (page == 0x24)
        {
            Assert.Equal(3, result.ObservableInstances.Count);
            Assert.All(result.ObservableInstances, o => Assert.Null(o.Sample));
        }
        else
        {
            var missing = result.ObservableInstances[2].Flags;
            Assert.Null(missing.HighAlarm);
            Assert.Null(missing.LowAlarm);
            Assert.Null(missing.HighWarning);
            Assert.Null(missing.LowWarning);
            if (!fails) Assert.False(result.ObservableInstances[0].Flags.HighAlarm);
        }
    }

    [Fact]
    public async Task Complete_empty_descriptor_page_and_unsupported_module_are_distinguishable_from_failed_reads()
    {
        var registers = new SimulatedRegisters();
        var unsupported = await new VdmReader(registers).ReadAsync();
        Assert.False(unsupported.IsSupported);
        Assert.Equal(VdmReadStatus.Complete, unsupported.ReadStatus);
        Assert.Single(registers.Reads);
        registers.Set(0x01, 0x8E, [0x40]);
        var empty = await new VdmReader(registers).ReadAsync();
        Assert.True(empty.IsSupported);
        Assert.Empty(empty.ObservableInstances);
        Assert.Equal(VdmReadStatus.Complete, empty.ReadStatus);
    }

    [Fact]
    public async Task Group_boundary_maps_instances_64_and_65_to_separate_flag_bytes_and_samples()
    {
        var registers = new SimulatedRegisters();
        registers.Set(0x01, 0x8E, [0x40]);
        registers.Set(0x2F, 0x80, [0x01]);
        var firstDescriptors = new byte[128];
        firstDescriptors[127] = 1;
        var secondDescriptors = new byte[128];
        secondDescriptors[1] = 2;
        var firstSamples = new byte[128];
        firstSamples[126] = 0xAB;
        firstSamples[127] = 0xCD;
        var secondSamples = new byte[128];
        secondSamples[0] = 0x12;
        secondSamples[1] = 0x34;
        var flags = new byte[64];
        flags[31] = 0x20;
        flags[32] = 0x04;
        registers.Set(0x20, 0x80, firstDescriptors);
        registers.Set(0x21, 0x80, secondDescriptors);
        registers.Set(0x24, 0x80, firstSamples);
        registers.Set(0x25, 0x80, secondSamples);
        registers.Set(0x2C, 0x80, flags);

        var result = await new VdmReader(registers).ReadAsync();

        Assert.Equal(VdmReadStatus.Complete, result.ReadStatus);
        Assert.Equal(2, result.ObservableInstances.Count);
        Assert.Equal(64, result.ObservableInstances[0].Instance);
        Assert.Equal((ushort)0xABCD, result.ObservableInstances[0].Sample);
        Assert.True(result.ObservableInstances[0].Flags.LowAlarm);
        Assert.False(result.ObservableInstances[0].Flags.HighWarning);
        Assert.Equal(65, result.ObservableInstances[1].Instance);
        Assert.Equal((ushort)0x1234, result.ObservableInstances[1].Sample);
        Assert.False(result.ObservableInstances[1].Flags.LowAlarm);
        Assert.True(result.ObservableInstances[1].Flags.HighWarning);
        Assert.DoesNotContain(registers.Reads, r => r.Page is 0x22 or 0x23 or 0x26 or 0x27);
    }

    private sealed class SimulatedRegisters : IRegisterAccess
    {
        private readonly Dictionary<(byte Page, byte Address), byte[]> _values = [];
        public List<(int Page, int Address)> Reads { get; } = [];
        public HashSet<byte> FailedPages { get; } = [];

        public void Set(byte page, byte address, byte[] value) => _values[(page, address)] = value;
        public Task<byte> ReadByteAsync(byte page, byte address)
        {
            Reads.Add((page, address));
            if (FailedPages.Contains(page)) throw new IOException("Read failed");
            return Task.FromResult(_values.TryGetValue((page, address), out var value) ? value[0] : (byte)0);
        }
        public Task<byte[]> ReadBlockAsync(byte page, byte startAddress, int length)
        {
            Reads.Add((page, startAddress));
            if (FailedPages.Contains(page)) throw new IOException("Read failed");
            return Task.FromResult(_values.TryGetValue((page, startAddress), out var value)
                ? value
                : new byte[length]);
        }
        public Task<byte[]> ReadBlockAsync(byte bank, byte page, byte startAddress, int length) =>
            ReadBlockAsync(page, startAddress, length);
        public Task WriteByteAsync(byte page, byte address, byte value) => Task.CompletedTask;
        public Task WriteBlockAsync(byte page, byte startAddress, byte[] data) => Task.CompletedTask;
        public Task WriteBlockAsync(byte bank, byte page, byte startAddress, byte[] data) => Task.CompletedTask;
    }

    private sealed class ConnectedDeviceConnection : IDeviceConnection
    {
        public bool IsConnected => true;
        public Task<bool> OpenAsync() => Task.FromResult(true);
        public Task CloseAsync() => Task.CompletedTask;
        public Task<byte[]> ReadAsync(int length) => Task.FromResult(new byte[length]);
        public Task WriteAsync(byte[] data) => Task.CompletedTask;
        public void Dispose() { }
    }
}
