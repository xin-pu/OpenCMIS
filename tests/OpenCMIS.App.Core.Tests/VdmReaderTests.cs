using OpenCMIS.Protocol.Abstractions.Models;
using OpenCMIS.Protocol.Abstractions;
using OpenCMIS.App.Core.Services;
using Xunit;

namespace OpenCMIS.App.Core.Tests;

public sealed class VdmReaderTests
{
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
        flagPage[0] = 0xA0;
        registers.Set(0x2C, 0x80, flagPage);

        var diagnostics = await new VdmReader(registers).ReadAsync();

        var observable = Assert.Single(diagnostics.ObservableInstances);
        Assert.Equal(1, observable.Instance);
        Assert.Equal(new byte[] { 0x12, 0x34 }, observable.Descriptor);
        Assert.Equal((ushort)0xABCD, observable.Sample);
        Assert.True(observable.Flags.HighAlarm);
        Assert.True(observable.Flags.LowAlarm);
    }

    private sealed class SimulatedRegisters : IRegisterAccess
    {
        private readonly Dictionary<(byte Page, byte Address), byte[]> _values = [];

        public void Set(byte page, byte address, byte[] value) => _values[(page, address)] = value;
        public Task<byte> ReadByteAsync(byte page, byte address) =>
            Task.FromResult(_values.TryGetValue((page, address), out var value) ? value[0] : (byte)0);
        public Task<byte[]> ReadBlockAsync(byte page, byte startAddress, int length) =>
            Task.FromResult(_values.TryGetValue((page, startAddress), out var value)
                ? value
                : new byte[length]);
        public Task<byte[]> ReadBlockAsync(byte bank, byte page, byte startAddress, int length) =>
            ReadBlockAsync(page, startAddress, length);
        public Task WriteByteAsync(byte page, byte address, byte value) => Task.CompletedTask;
        public Task WriteBlockAsync(byte page, byte startAddress, byte[] data) => Task.CompletedTask;
        public Task WriteBlockAsync(byte bank, byte page, byte startAddress, byte[] data) => Task.CompletedTask;
    }
}
