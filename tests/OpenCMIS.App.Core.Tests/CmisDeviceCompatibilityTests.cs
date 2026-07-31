using System.Text;
using OpenCMIS.Module.Core;
using OpenCMIS.Module.Core.Hci;
using OpenCMIS.Module.Core.Msa;
using OpenCMIS.Protocol.Abstractions;
using OpenCMIS.Shared;
using OpenCMIS.Transport.Abstractions;
using Xunit;

namespace OpenCMIS.App.Core.Tests;

public sealed class CmisDeviceCompatibilityTests
{
    private static readonly I2cDeviceAddress Address = new(0x50);

    [Fact]
    public async Task ReadModuleIdentity_preserves_existing_mapping()
    {
        var msa = new StubMsaMemoryAccessor()
            .Returns(0x01, CmisConstants.RegVendorNameStart, Ascii("VENDOR", 16))
            .Returns(0x01, CmisConstants.RegVendorOUI, [0x00, 0x11, 0x22])
            .Returns(0x01, CmisConstants.RegPartNumberStart, Ascii("PART-NUMBER", 16))
            .Returns(0x01, CmisConstants.RegSerialNumberStart, Ascii("SERIAL", 16))
            .Returns(0x01, CmisConstants.RegHardwareRevision, [0x12, 0x34])
            .Returns(0x01, CmisConstants.RegFirmwareRevision, [0x12, 0x34])
            .Returns(0x01, CmisConstants.RegDateCode, Ascii("260731", 8))
            .Returns(0x01, CmisConstants.RegCLEICode, Ascii("CLEI", 10))
            .Returns(0x00, CmisConstants.RegIdentifier, [0x18])
            .Returns(0x00, CmisConstants.RegRevision, [0x52]);
        var (device, session, _) = await CreateDeviceAsync(msa);
        await using var ownedSession = session;

        var identity = await device.ReadModuleIdentityAsync();

        Assert.Equal("VENDOR", identity.VendorName);
        Assert.Equal("00-11-22", identity.VendorOUI);
        Assert.Equal("PART-NUMBER", identity.PartNumber);
        Assert.Equal("SERIAL", identity.SerialNumber);
        Assert.Equal("1234", identity.HardwareRevision);
        Assert.Equal("123.4", identity.FirmwareRevision);
        Assert.Equal("QSFP28", identity.ModuleType);
        Assert.Equal("QSFP28 (38-pin)", identity.ConnectorType);
        Assert.Equal("5.2", identity.CmisVersion);
    }

    [Fact]
    public async Task ReadModuleMonitors_preserves_existing_scaling()
    {
        var msa = new StubMsaMemoryAccessor()
            .Returns(0x00, CmisConstants.RegTemperatureMSB, [0x00, 0x01])
            .Returns(0x00, CmisConstants.RegVccMSB, [0x10, 0x27])
            .Returns(0x10, CmisConstants.RegLaneTxBiasMSB, [0xF4, 0x01])
            .Returns(0x10, CmisConstants.RegLaneTxPowerMSB, [0x10, 0x27])
            .Returns(0x10, CmisConstants.RegLaneRxPowerMSB, [0x20, 0x4E]);
        var (device, session, _) = await CreateDeviceAsync(msa);
        await using var ownedSession = session;

        var monitors = await device.ReadModuleMonitorsAsync(1);

        Assert.Equal(1.00, monitors.Temperature.Value);
        Assert.Equal(1.0000, monitors.VCC.Value);
        Assert.Equal(1.000, monitors.TxBiasPerLane[0].Value);
        Assert.Equal(1.0000, monitors.TxPowerPerLane[0].Value);
        Assert.Equal(2.0000, monitors.RxPowerPerLane[0].Value);
    }

    [Fact]
    public async Task CloseAsync_disposes_the_module_session()
    {
        var (device, session, bus) = await CreateDeviceAsync(
            new StubMsaMemoryAccessor());

        await device.CloseAsync();

        Assert.True(bus.IsDisposed);
        Assert.False(session.IsOpen);
    }

    [Fact]
    public async Task Interface_exposes_vendor_Hci_accessor()
    {
        var bus = new LifecycleI2cBus();
        var session = new OpticalModuleSession(bus);
        await session.OpenAsync();
        await using var ownedSession = session;
        var hci = new StubHciMemoryAccessor();
        ICmisDevice device = new CmisDevice(
            new DeviceInfo { Id = "test", Name = "Test module" },
            session,
            Address,
            new StubMsaMemoryAccessor(),
            hci);

        Assert.Same(hci, device.HciAccess);
    }

    private static async Task<(
        CmisDevice Device,
        OpticalModuleSession Session,
        LifecycleI2cBus Bus)> CreateDeviceAsync(IMsaMemoryAccessor msa)
    {
        var bus = new LifecycleI2cBus();
        var session = new OpticalModuleSession(bus);
        await session.OpenAsync();
        var device = new CmisDevice(
            new DeviceInfo { Id = "test", Name = "Test module" },
            session,
            Address,
            msa,
            new StubHciMemoryAccessor());
        return (device, session, bus);
    }

    private static byte[] Ascii(string value, int length)
    {
        var result = new byte[length];
        Encoding.ASCII.GetBytes(value).CopyTo(result, 0);
        Array.Fill(result, (byte)' ', value.Length, length - value.Length);
        return result;
    }

    private sealed class StubMsaMemoryAccessor : IMsaMemoryAccessor
    {
        private readonly Dictionary<(byte Page, byte Offset, int Length), byte[]>
            _reads = [];

        public StubMsaMemoryAccessor Returns(
            byte page,
            byte offset,
            byte[] data)
        {
            _reads[(page, offset, data.Length)] = data;
            return this;
        }

        public ValueTask<byte[]> ReadAsync(
            I2cDeviceAddress device,
            ModulePage page,
            RegisterOffset offset,
            int length,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(
                _reads[(page.Value, offset.Value, length)].ToArray());
        }

        public ValueTask WriteAsync(
            I2cDeviceAddress device,
            ModulePage page,
            RegisterOffset offset,
            ReadOnlyMemory<byte> data,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.CompletedTask;
        }
    }

    private sealed class StubHciMemoryAccessor : IHciMemoryAccessor
    {
        public ValueTask<byte[]> ReadAsync(
            I2cDeviceAddress device,
            HciTableId table,
            RegisterOffset offset,
            int length,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(new byte[length]);

        public ValueTask WriteAsync(
            I2cDeviceAddress device,
            HciTableId table,
            RegisterOffset offset,
            ReadOnlyMemory<byte> data,
            CancellationToken cancellationToken = default) =>
            ValueTask.CompletedTask;
    }

    private sealed class LifecycleI2cBus : II2cRegisterBus
    {
        public bool IsOpen { get; private set; }
        public bool IsDisposed { get; private set; }
        public I2cTransferCapabilities Capabilities =>
            I2cTransferCapabilities.Unbounded;

        public ValueTask OpenAsync(CancellationToken cancellationToken = default)
        {
            IsOpen = true;
            return ValueTask.CompletedTask;
        }

        public ValueTask CloseAsync(CancellationToken cancellationToken = default)
        {
            IsOpen = false;
            return ValueTask.CompletedTask;
        }

        public ValueTask ReadAsync(
            I2cDeviceAddress device,
            RegisterOffset offset,
            Memory<byte> destination,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public ValueTask WriteAsync(
            I2cDeviceAddress device,
            RegisterOffset offset,
            ReadOnlyMemory<byte> data,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public ValueTask DisposeAsync()
        {
            IsOpen = false;
            IsDisposed = true;
            return ValueTask.CompletedTask;
        }
    }
}
