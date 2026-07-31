# Optical Module and I2C Refactor Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Refactor OpenCMIS into a .NET 10 optical-module communication core with atomic MSA/HCI access, named serial adapters, optional Windows-only Cypress adapters, and simulated test coverage.

**Architecture:** `Module.Core` owns one synchronized optical-module session and exposes MSA and vendor-HCI services over the hardware-independent `II2cRegisterBus` port. Serial and Cypress projects implement that port; Protocol and App depend inward on abstractions and compose devices through providers. Existing `ICmisDevice` workflows remain available through a compatibility facade while concrete TypeA/TypeB connectors become obsolete.

**Tech Stack:** C# 14, .NET 10, `System.IO.Ports`, Microsoft.Extensions.DependencyInjection, xUnit, Microsoft.NET.Test.Sdk, Windows P/Invoke in the isolated Cypress projects.

## Global Constraints

- Every project targets `net10.0` except `OpenCMIS.Cypress`, `OpenCMIS.Transport.I2C.Cypress`, their tests, and WPF, which target `net10.0-windows`.
- `OpenCMIS.Module.Core`, Protocol, App, and CLI must not reference Cypress assemblies.
- No OpenCMIS project may reference a Pulse project or Pulse NuGet package.
- I2C addresses are canonical 7-bit values in core code; legacy `0xA0` is converted explicitly to `0x50` at compatibility boundaries.
- MSA page selection plus transfer and the complete HCI command sequence share one `OpticalModuleSession` gate.
- Tests use fakes/mocks and must not claim real serial, FIC2USB, EUI3, module, or driver verification.
- Cypress source must not be imported or published until its license permits the intended repository use.
- Each task ends with focused tests, a solution build, and a separate commit.

---

## File Map

### Transport contracts

- `src/OpenCMIS.Transport.Abstractions/Interfaces/II2cRegisterBus.cs`: target-address-aware asynchronous I2C port.
- `src/OpenCMIS.Transport.Abstractions/Interfaces/II2cAdapterProvider.cs`: adapter discovery and creation port.
- `src/OpenCMIS.Transport.Abstractions/Models/I2cDeviceAddress.cs`: validated canonical 7-bit address.
- `src/OpenCMIS.Transport.Abstractions/Models/RegisterOffset.cs`: validated register offset.
- `src/OpenCMIS.Transport.Abstractions/Models/I2cTransferCapabilities.cs`: transfer limits.
- `src/OpenCMIS.Transport.Abstractions/Models/I2cRetryOptions.cs`: transient transfer retry limits.
- `src/OpenCMIS.Transport.Abstractions/Models/I2cConnectionProfile.cs`: closed typed-profile base contract and concrete profiles.
- `src/OpenCMIS.Transport.Abstractions/Models/I2cAdapterDescriptor.cs`: provider discovery result.
- `src/OpenCMIS.Transport.Abstractions/Models/I2cProbeFailure.cs`: structured discovery failure.

### Serial infrastructure

- `src/OpenCMIS.Transport.I2C.Serial/Serial/ISerialSession.cs`: mockable serial read/write boundary.
- `src/OpenCMIS.Transport.I2C.Serial/Serial/SerialPortSession.cs`: `System.IO.Ports` implementation with exact-length reads.
- `src/OpenCMIS.Transport.I2C.Serial/Codecs/LinktelI2cCodec.cs`: Linktel frame encoding and response validation.
- `src/OpenCMIS.Transport.I2C.Serial/Codecs/HmI2cCodec.cs`: HM frame encoding and response validation.
- `src/OpenCMIS.Transport.I2C.Serial/Adapters/LinktelSerialI2cAdapter.cs`: Linktel `II2cRegisterBus`.
- `src/OpenCMIS.Transport.I2C.Serial/Adapters/HmSerialI2cAdapter.cs`: HM `II2cRegisterBus`.
- `src/OpenCMIS.Transport.I2C.Serial/Adapters/HmMultiChannelI2cAdapter.cs`: channel-selecting HM adapter.
- `src/OpenCMIS.Transport.I2C.Serial/Providers/LinktelSerialAdapterProvider.cs`: Linktel discovery/profile factory.
- `src/OpenCMIS.Transport.I2C.Serial/Providers/HmSerialAdapterProvider.cs`: HM discovery/profile factory.
- `src/OpenCMIS.Transport.I2C.Serial/Providers/HmMultiChannelAdapterProvider.cs`: HM multichannel discovery/profile factory.
- `src/OpenCMIS.Transport.I2C/Implementations/I2CConnectorTypeA.cs`: obsolete forwarding wrapper.
- `src/OpenCMIS.Transport.I2C/Implementations/I2CConnectorTypeB.cs`: obsolete forwarding wrapper.

### Module domain

- `src/OpenCMIS.Module.Core/OpticalModuleSession.cs`: shared synchronization and bus lifetime.
- `src/OpenCMIS.Module.Core/Msa/IMsaMemoryAccessor.cs`: MSA contract.
- `src/OpenCMIS.Module.Core/Msa/MsaMemoryAccessor.cs`: atomic page-select/read/write.
- `src/OpenCMIS.Module.Core/Hci/IHciMemoryAccessor.cs`: HCI contract.
- `src/OpenCMIS.Module.Core/Hci/HciCommandCodec.cs`: vendor command frames.
- `src/OpenCMIS.Module.Core/Hci/HciMemoryAccessor.cs`: HCI execution, polling, validation.
- `src/OpenCMIS.Module.Core/Hci/HciOptions.cs`: ready bytes, timeout, polling backoff.
- `src/OpenCMIS.Module.Core/Models/ModulePage.cs`: module page value object.
- `src/OpenCMIS.Module.Core/Models/HciTableId.cs`: HCI table value object.

### Protocol and application

- `src/OpenCMIS.Protocol.Core/Implementations/RegisterAccess.cs`: compatibility adapter over `IMsaMemoryAccessor`.
- `src/OpenCMIS.Protocol.Core/Implementations/PageManager.cs`: obsolete compatibility surface without independent page cache.
- `src/OpenCMIS.App.Core/CmisDevice.cs`: facade over CMIS services and module accessors.
- `src/OpenCMIS.App.Core/DeviceManager.cs`: provider-based discovery and opening.
- `src/OpenCMIS.App.Core/ServiceCollectionExtensions.cs`: core-only registration.
- `src/OpenCMIS.Protocol.Abstractions/Models/ModuleIdentity.cs`: public identity result.
- `src/OpenCMIS.Protocol.Abstractions/Models/ModuleMonitors.cs`: public monitor snapshot.
- `src/OpenCMIS.Protocol.Abstractions/Models/MonitorValue.cs`: scaled monitor value.
- `src/OpenCMIS.Protocol.Abstractions/Models/LaneStatus.cs`: public lane result.
- `src/OpenCMIS.Protocol.Abstractions/Models/ModuleDashData.cs`: dashboard snapshot.
- `src/OpenCMIS.Protocol.Abstractions/Models/ModuleInfo.cs`: module summary.
- `src/OpenCMIS.Protocol.Abstractions/Models/ModuleStatus.cs`: module status.

### Cypress infrastructure

- `src/OpenCMIS.Cypress/**`: authorized low-level Cypress source, notices, and Windows-only project.
- `src/OpenCMIS.Transport.I2C.Cypress/ICypressDeviceApi.cs`: mockable wrapper around low-level devices.
- `src/OpenCMIS.Transport.I2C.Cypress/Fic2UsbI2cAdapter.cs`: FIC2USB adapter.
- `src/OpenCMIS.Transport.I2C.Cypress/Eui3I2cAdapter.cs`: EUI3 adapter.
- `src/OpenCMIS.Transport.I2C.Cypress/CypressI2cAdapterProvider.cs`: Windows discovery/opening.

### Tests

- `tests/OpenCMIS.Transport.Abstractions.Tests/I2cDeviceAddressTests.cs`: address validation and conversion.
- `tests/OpenCMIS.Transport.Abstractions.Tests/I2cConnectionProfileTests.cs`: typed-profile validation.
- `tests/OpenCMIS.Transport.I2C.Serial.Tests/LinktelI2cCodecTests.cs`: Linktel frames.
- `tests/OpenCMIS.Transport.I2C.Serial.Tests/HmI2cCodecTests.cs`: HM and multichannel frames.
- `tests/OpenCMIS.Transport.I2C.Serial.Tests/SerialPortSessionTests.cs`: exact reads and cancellation.
- `tests/OpenCMIS.Transport.I2C.Serial.Tests/SerialI2cAdapterTests.cs`: adapter retry and segmentation.
- `tests/OpenCMIS.Module.Core.Tests/MsaMemoryAccessorTests.cs`: MSA sequence and concurrency.
- `tests/OpenCMIS.Module.Core.Tests/HciCommandCodecTests.cs`: HCI frames.
- `tests/OpenCMIS.Module.Core.Tests/HciMemoryAccessorTests.cs`: HCI polling and concurrency.
- `tests/OpenCMIS.App.Core.Tests/CmisDeviceCompatibilityTests.cs`: facade behavior.
- `tests/OpenCMIS.App.Core.Tests/DeviceManagerTests.cs`: provider selection.
- `tests/OpenCMIS.Transport.I2C.Cypress.Tests/CypressI2cAdapterTests.cs`: mocked transfers.
- `tests/OpenCMIS.Transport.I2C.Cypress.Tests/CypressI2cAdapterProviderTests.cs`: mocked discovery.

---

### Task 1: Introduce the hardware-independent I2C contracts

**Files:**

- Create: `tests/OpenCMIS.Transport.Abstractions.Tests/OpenCMIS.Transport.Abstractions.Tests.csproj`
- Create: `tests/OpenCMIS.Transport.Abstractions.Tests/I2cDeviceAddressTests.cs`
- Create: `src/OpenCMIS.Transport.Abstractions/Interfaces/II2cRegisterBus.cs`
- Create: `src/OpenCMIS.Transport.Abstractions/Interfaces/II2cAdapterProvider.cs`
- Create: `src/OpenCMIS.Transport.Abstractions/Models/I2cDeviceAddress.cs`
- Create: `src/OpenCMIS.Transport.Abstractions/Models/RegisterOffset.cs`
- Create: `src/OpenCMIS.Transport.Abstractions/Models/I2cTransferCapabilities.cs`
- Create: `src/OpenCMIS.Transport.Abstractions/Models/I2cRetryOptions.cs`
- Create: `src/OpenCMIS.Transport.Abstractions/Models/I2cConnectionProfile.cs`
- Create: `src/OpenCMIS.Transport.Abstractions/Models/I2cAdapterDescriptor.cs`
- Create: `src/OpenCMIS.Transport.Abstractions/Models/I2cProbeFailure.cs`
- Modify: `src/OpenCMIS.Shared/Enums/CmisErrorCode.cs`
- Modify: `OpenCMIS.sln`

**Interfaces:**

- Consumes: no new project interfaces.
- Produces: `II2cRegisterBus`, `II2cAdapterProvider`, `I2cDeviceAddress`, `RegisterOffset`, `I2cTransferCapabilities`, `I2cRetryOptions`, `I2cConnectionProfile`, `SerialI2cConnectionProfile`, `HmMultiChannelConnectionProfile`, `CypressI2cConnectionProfile`, and `I2cAdapterDescriptor`.

- [ ] **Step 1: Add the test project and failing address tests**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.14.1" />
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.1.4">
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\OpenCMIS.Transport.Abstractions\OpenCMIS.Transport.Abstractions.csproj" />
  </ItemGroup>
</Project>
```

```csharp
public sealed class I2cDeviceAddressTests
{
    [Theory]
    [InlineData(0x00)]
    [InlineData(0x50)]
    [InlineData(0x7F)]
    public void Constructor_accepts_7_bit_values(byte value) =>
        Assert.Equal(value, new I2cDeviceAddress(value).Value);

    [Fact]
    public void Constructor_rejects_8_bit_value() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new I2cDeviceAddress(0xA0));

    [Fact]
    public void FromLegacy8Bit_converts_A0_to_50() =>
        Assert.Equal(0x50, I2cDeviceAddress.FromLegacy8Bit(0xA0).Value);

    [Fact]
    public void ToWriteAddress8Bit_converts_50_to_A0() =>
        Assert.Equal(0xA0, new I2cDeviceAddress(0x50).ToWriteAddress8Bit());
}
```

- [ ] **Step 2: Run the focused test and verify it fails**

Run:

```powershell
dotnet test tests\OpenCMIS.Transport.Abstractions.Tests\OpenCMIS.Transport.Abstractions.Tests.csproj --no-restore
```

Expected: compilation fails because `I2cDeviceAddress` does not exist.

- [ ] **Step 3: Add validated value objects and transfer capabilities**

```csharp
public readonly record struct I2cDeviceAddress
{
    public I2cDeviceAddress(byte value)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value, (byte)0x7F);
        Value = value;
    }

    public byte Value { get; }

    public static I2cDeviceAddress FromLegacy8Bit(byte value)
    {
        if ((value & 0x01) != 0)
            throw new ArgumentOutOfRangeException(nameof(value), "Expected an 8-bit write address.");
        return new I2cDeviceAddress((byte)(value >> 1));
    }

    public byte ToWriteAddress8Bit() => (byte)(Value << 1);
}

public readonly record struct RegisterOffset(byte Value);

public sealed record I2cTransferCapabilities(int MaxReadLength, int MaxWriteLength)
{
    public static I2cTransferCapabilities Unbounded { get; } = new(int.MaxValue, int.MaxValue);
}

public sealed record I2cRetryOptions(int MaxAttempts, TimeSpan Delay)
{
    public static I2cRetryOptions Default { get; } =
        new(3, TimeSpan.FromMilliseconds(20));
}
```

- [ ] **Step 4: Add the bus, provider, descriptor, and typed profile contracts**

```csharp
public interface II2cRegisterBus : IAsyncDisposable
{
    bool IsOpen { get; }
    I2cTransferCapabilities Capabilities { get; }
    ValueTask OpenAsync(CancellationToken cancellationToken = default);
    ValueTask CloseAsync(CancellationToken cancellationToken = default);
    ValueTask ReadAsync(
        I2cDeviceAddress device,
        RegisterOffset offset,
        Memory<byte> destination,
        CancellationToken cancellationToken = default);
    ValueTask WriteAsync(
        I2cDeviceAddress device,
        RegisterOffset offset,
        ReadOnlyMemory<byte> data,
        CancellationToken cancellationToken = default);
}

public interface II2cAdapterProvider
{
    string AdapterId { get; }
    ValueTask<IReadOnlyList<I2cAdapterDescriptor>> DiscoverAsync(
        CancellationToken cancellationToken = default);
    ValueTask<II2cRegisterBus> OpenAsync(
        I2cConnectionProfile profile,
        CancellationToken cancellationToken = default);
}

public abstract record I2cConnectionProfile(
    string AdapterId, I2cDeviceAddress DeviceAddress);
public sealed record SerialI2cConnectionProfile(
    string AdapterId, string PortName, int BaudRate, I2cDeviceAddress DeviceAddress)
    : I2cConnectionProfile(AdapterId, DeviceAddress);
public sealed record HmMultiChannelConnectionProfile(
    string AdapterId, string PortName, int BaudRate, byte Channel,
    I2cDeviceAddress DeviceAddress)
    : I2cConnectionProfile(AdapterId, DeviceAddress);
public sealed record CypressI2cConnectionProfile(
    string AdapterId, string SerialNumber, int Port, int SpeedKhz,
    I2cDeviceAddress DeviceAddress)
    : I2cConnectionProfile(AdapterId, DeviceAddress);
public sealed record I2cAdapterDescriptor(
    string AdapterId, string DeviceId, string DisplayName, I2cConnectionProfile Profile);
public sealed record I2cProbeFailure(string AdapterId, string Candidate, string Message);
```

- [ ] **Step 5: Add transport error codes**

Add the following stable values to `CmisErrorCode`:

```csharp
I2cAdapterNotFound = 600,
I2cConnectionFailed = 610,
I2cTransferFailed = 620,
I2cInvalidResponse = 630,
```

- [ ] **Step 6: Run focused tests and the solution build**

Run:

```powershell
dotnet test tests\OpenCMIS.Transport.Abstractions.Tests\OpenCMIS.Transport.Abstractions.Tests.csproj
dotnet build OpenCMIS.sln --no-restore
```

Expected: all address tests pass; solution build succeeds with zero warnings and errors.

- [ ] **Step 7: Commit the transport contracts**

```powershell
git add OpenCMIS.sln src\OpenCMIS.Transport.Abstractions tests\OpenCMIS.Transport.Abstractions.Tests
git commit -m "feat: add hardware-independent I2C contracts"
```

---

### Task 2: Replace TypeA and TypeB internals with named, testable serial adapters

**Files:**

- Create: `src/OpenCMIS.Transport.I2C.Serial/OpenCMIS.Transport.I2C.Serial.csproj`
- Create: `src/OpenCMIS.Transport.I2C.Serial/Serial/ISerialSession.cs`
- Create: `src/OpenCMIS.Transport.I2C.Serial/Serial/SerialPortSession.cs`
- Create: `src/OpenCMIS.Transport.I2C.Serial/Codecs/LinktelI2cCodec.cs`
- Create: `src/OpenCMIS.Transport.I2C.Serial/Codecs/HmI2cCodec.cs`
- Create: `src/OpenCMIS.Transport.I2C.Serial/Adapters/SerialI2cAdapterBase.cs`
- Create: `src/OpenCMIS.Transport.I2C.Serial/Adapters/LinktelSerialI2cAdapter.cs`
- Create: `src/OpenCMIS.Transport.I2C.Serial/Adapters/HmSerialI2cAdapter.cs`
- Create: `src/OpenCMIS.Transport.I2C.Serial/Adapters/HmMultiChannelI2cAdapter.cs`
- Create: `src/OpenCMIS.Transport.I2C.Serial/Adapters/SerialTransferRetry.cs`
- Create: `tests/OpenCMIS.Transport.I2C.Serial.Tests/OpenCMIS.Transport.I2C.Serial.Tests.csproj`
- Create: `tests/OpenCMIS.Transport.I2C.Serial.Tests/LinktelI2cCodecTests.cs`
- Create: `tests/OpenCMIS.Transport.I2C.Serial.Tests/HmI2cCodecTests.cs`
- Create: `tests/OpenCMIS.Transport.I2C.Serial.Tests/SerialPortSessionTests.cs`
- Create: `tests/OpenCMIS.Transport.I2C.Serial.Tests/SerialI2cAdapterTests.cs`
- Create: `tests/OpenCMIS.Transport.I2C.Serial.Tests/Fakes/ScriptedSerialSession.cs`
- Modify: `src/OpenCMIS.Transport.I2C/Implementations/I2CConnectorTypeA.cs`
- Modify: `src/OpenCMIS.Transport.I2C/Implementations/I2CConnectorTypeB.cs`
- Modify: `OpenCMIS.sln`

**Interfaces:**

- Consumes: `II2cRegisterBus`, `I2cDeviceAddress`, `RegisterOffset`, `I2cTransferCapabilities`.
- Produces: named serial adapters and `ISerialSession`, whose `ReadExactlyAsync` guarantees filling the requested buffer or throwing.

- [ ] **Step 1: Write failing Linktel and HM codec characterization tests**

```csharp
[Fact]
public void Linktel_read_frame_uses_explicit_device_address()
{
    var frame = LinktelI2cCodec.EncodeRead(
        new I2cDeviceAddress(0x50), new RegisterOffset(0x80), 4);
    Assert.Equal(new byte[] { 0x55, 0x38, 0x11, 0x03, 0xA0, 0x80, 0x04, 0x0D, 0x0A }, frame);
}

[Fact]
public void Hm_read_frame_uses_explicit_device_address()
{
    var frame = HmI2cCodec.EncodeRead(
        new I2cDeviceAddress(0x50), new RegisterOffset(0x10), 2);
    Assert.Equal(new byte[] { 0x02, 0xD3, 0x00, 0xAA, 0xA0, 0x10 }, frame);
}

[Fact]
public void Linktel_parser_rejects_invalid_status()
{
    var response = new byte[] { 0xAA, 0x00, 0x01, 0x01, 0x42, 0x0D, 0x0A };
    Assert.Throws<CmisException>(() => LinktelI2cCodec.ParseRead(response, 1));
}
```

- [ ] **Step 2: Run codec tests and verify missing-type failures**

Run:

```powershell
dotnet test tests\OpenCMIS.Transport.I2C.Serial.Tests\OpenCMIS.Transport.I2C.Serial.Tests.csproj --filter "FullyQualifiedName~Codec"
```

Expected: compilation fails because the named codecs do not exist.

- [ ] **Step 3: Implement pure codecs by extracting current wire formats**

```csharp
public static class LinktelI2cCodec
{
    public static byte[] EncodeRead(I2cDeviceAddress device, RegisterOffset offset, int length);
    public static byte[] EncodeWrite(
        I2cDeviceAddress device, RegisterOffset offset, ReadOnlySpan<byte> data);
    public static byte[] ParseRead(ReadOnlySpan<byte> response, int expectedLength);
    public static void ValidateWrite(ReadOnlySpan<byte> response);
}

public static class HmI2cCodec
{
    public static byte[] EncodeRead(I2cDeviceAddress device, RegisterOffset offset, int length);
    public static byte[] EncodeWrite(
        I2cDeviceAddress device, RegisterOffset offset, ReadOnlySpan<byte> data);
    public static byte[] ParseRead(ReadOnlySpan<byte> response, int expectedLength);
    public static void ValidateWrite(ReadOnlySpan<byte> response);
}
```

Preserve the current Linktel constants `0x55/0xAA/0x10/0x11/0x0D/0x0A` and HM constants `0xD2/0xD3/0xAA`; validate length, header, status, terminators, and checksum wherever the wire format carries them.

- [ ] **Step 4: Write failing serial-session partial-read and cancellation tests**

```csharp
[Fact]
public async Task ReadExactlyAsync_combines_partial_reads()
{
    var stream = new ScriptedSerialStream([new byte[] { 1 }, new byte[] { 2, 3 }]);
    var session = new SerialPortSession(stream);
    var buffer = new byte[3];
    await session.ReadExactlyAsync(buffer, CancellationToken.None);
    Assert.Equal(new byte[] { 1, 2, 3 }, buffer);
}

[Fact]
public async Task ReadExactlyAsync_propagates_cancellation()
{
    using var cts = new CancellationTokenSource();
    cts.Cancel();
    var session = new SerialPortSession(new BlockingSerialStream());
    await Assert.ThrowsAnyAsync<OperationCanceledException>(
        () => session.ReadExactlyAsync(new byte[1], cts.Token).AsTask());
}
```

- [ ] **Step 5: Implement the mockable serial boundary and named adapters**

```csharp
public interface ISerialSession : IAsyncDisposable
{
    ValueTask OpenAsync(CancellationToken cancellationToken);
    ValueTask WriteAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken);
    ValueTask ReadExactlyAsync(Memory<byte> destination, CancellationToken cancellationToken);
}

public interface ISerialSessionFactory
{
    ISerialSession Create(SerialI2cConnectionProfile profile);
}
```

`LinktelSerialI2cAdapter` and `HmSerialI2cAdapter` open one session per
complete I2C operation, use their codec, honor the supplied target address,
segment transfers to advertised limits, and let `OperationCanceledException`
escape unwrapped. `HmMultiChannelI2cAdapter` validates channels `1..5` and uses
the characterized command-byte maps below instead of sending a separate
channel-select transfer:

```csharp
private static readonly byte[] ReadCommands =
    [0xE2, 0xE4, 0xD3, 0xE6, 0xE8];
private static readonly byte[] WriteCommands =
    [0xE1, 0xE3, 0xD2, 0xE5, 0xE7];
```

Add a theory covering all five channels and assert that the remaining HM frame
bytes stay `[length, command, 0x00, 0xAA, address8Bit, register]`.

- [ ] **Step 6: Replace old implementations with obsolete forwarding wrappers**

```csharp
[Obsolete("Use LinktelSerialI2cAdapter with a canonical 7-bit I2C address.")]
public sealed class I2CConnectorTypeA : IRegisterTransport
{
    private readonly LinktelSerialI2cAdapter _inner;
    private readonly I2cDeviceAddress _device;

    public I2CConnectorTypeA(string portName, int baudRate = 115200, byte slaveAddress = 0xA0)
    {
        _device = I2cDeviceAddress.FromLegacy8Bit(slaveAddress);
        _inner = LinktelSerialI2cAdapter.CreateDefault(portName, baudRate);
    }

    public bool IsConnected => _inner.IsOpen;
    public async Task<bool> OpenAsync()
    {
        await _inner.OpenAsync();
        return _inner.IsOpen;
    }
    public Task CloseAsync() => _inner.CloseAsync().AsTask();
    public async Task<byte> ReadRegisterAsync(byte registerAddress)
    {
        var data = await ReadRegisterBlockAsync(registerAddress, 1);
        return data[0];
    }
    public async Task<byte[]> ReadRegisterBlockAsync(byte registerAddress, int length)
    {
        var data = new byte[length];
        await _inner.ReadAsync(_device, new RegisterOffset(registerAddress), data);
        return data;
    }
    public Task WriteRegisterAsync(byte registerAddress, byte value) =>
        WriteRegisterBlockAsync(registerAddress, [value]);
    public Task WriteRegisterBlockAsync(byte registerAddress, byte[] data) =>
        _inner.WriteAsync(_device, new RegisterOffset(registerAddress), data).AsTask();
    public Task<byte[]> ReadAsync(int length) =>
        throw new NotSupportedException("Raw serial reads are not part of II2cRegisterBus.");
    public Task WriteAsync(byte[] data) =>
        throw new NotSupportedException("Raw serial writes are not part of II2cRegisterBus.");
    public void Dispose() => _inner.DisposeAsync().AsTask().GetAwaiter().GetResult();
}
```

Add `I2CConnectorTypeB` with the same eight forwarded members and disposal behavior,
but construct `HmSerialI2cAdapter.CreateDefault(portName, baudRate)` and retain its
existing defaults `1500000` and legacy address `0xA0`. Keep both wrappers only
while App/Protocol migration is in progress.

- [ ] **Step 7: Add transient-only retry tests and implementation**

```csharp
[Fact]
public async Task Adapter_retries_transient_IO_failure()
{
    var sessions = new ScriptedSerialSessionFactory(
        ScriptedSerialSession.Throwing(new IOException("temporary")),
        ScriptedSerialSession.Returning(LinktelSuccess([0x42])));
    await using var adapter = new LinktelSerialI2cAdapter(
        sessions, Profile, new I2cRetryOptions(2, TimeSpan.Zero), TimeProvider.System);
    var data = new byte[1];

    await adapter.ReadAsync(Address, Offset, data);

    Assert.Equal(2, sessions.CreateCount);
    Assert.Equal(0x42, data[0]);
}

[Fact]
public async Task Adapter_does_not_retry_invalid_response()
{
    var sessions = new ScriptedSerialSessionFactory(
        ScriptedSerialSession.Returning([0x00]));
    await using var adapter = new LinktelSerialI2cAdapter(
        sessions, Profile, new I2cRetryOptions(3, TimeSpan.Zero), TimeProvider.System);

    await Assert.ThrowsAsync<CmisException>(
        () => adapter.ReadAsync(Address, Offset, new byte[1]).AsTask());

    Assert.Equal(1, sessions.CreateCount);
}
```

`SerialTransferRetry.ExecuteAsync` retries only `IOException` and
`TimeoutException`, uses
`Task.Delay(options.Delay, timeProvider, cancellationToken)`, never retries
`CmisException` frame-validation failures, and propagates
`OperationCanceledException`.

- [ ] **Step 8: Run serial tests and solution build**

Run:

```powershell
dotnet test tests\OpenCMIS.Transport.I2C.Serial.Tests\OpenCMIS.Transport.I2C.Serial.Tests.csproj
dotnet build OpenCMIS.sln --no-restore
```

Expected: codec, partial-read, cancellation, error-conversion, segmentation, and explicit-address tests pass; build succeeds.

- [ ] **Step 9: Commit named serial adapters**

```powershell
git add OpenCMIS.sln src\OpenCMIS.Transport.I2C.Serial src\OpenCMIS.Transport.I2C tests\OpenCMIS.Transport.I2C.Serial.Tests
git commit -m "refactor: add named serial I2C adapters"
```

---

### Task 3: Add the synchronized optical-module session and atomic MSA access

**Files:**

- Create: `src/OpenCMIS.Module.Core/OpenCMIS.Module.Core.csproj`
- Create: `src/OpenCMIS.Module.Core/OpticalModuleSession.cs`
- Create: `src/OpenCMIS.Module.Core/Models/ModulePage.cs`
- Create: `src/OpenCMIS.Module.Core/Msa/IMsaMemoryAccessor.cs`
- Create: `src/OpenCMIS.Module.Core/Msa/MsaMemoryAccessor.cs`
- Create: `tests/OpenCMIS.Module.Core.Tests/OpenCMIS.Module.Core.Tests.csproj`
- Create: `tests/OpenCMIS.Module.Core.Tests/Fakes/ScriptedI2cRegisterBus.cs`
- Create: `tests/OpenCMIS.Module.Core.Tests/MsaMemoryAccessorTests.cs`
- Modify: `OpenCMIS.sln`
- Modify: `src/OpenCMIS.Shared/Enums/CmisErrorCode.cs`

**Interfaces:**

- Consumes: `II2cRegisterBus`.
- Produces: `OpticalModuleSession`, `ModulePage`, and `IMsaMemoryAccessor`.

- [ ] **Step 1: Add a scripted bus and failing MSA sequence test**

Use this test project definition so Task 4 can advance virtual time without
real delays:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.TimeProvider.Testing" Version="10.0.0" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.14.1" />
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.1.4">
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\OpenCMIS.Module.Core\OpenCMIS.Module.Core.csproj" />
  </ItemGroup>
</Project>
```

```csharp
[Fact]
public async Task Read_selects_page_and_reads_under_one_session_operation()
{
    var bus = new ScriptedI2cRegisterBus();
    bus.QueueRead(new byte[] { 0x11, 0x22 });
    await using var session = new OpticalModuleSession(bus);
    var accessor = new MsaMemoryAccessor(session);

    var result = await accessor.ReadAsync(
        new I2cDeviceAddress(0x50), new ModulePage(0x11),
        new RegisterOffset(0x80), 2);

    Assert.Equal(new byte[] { 0x11, 0x22 }, result);
    Assert.Equal(
        new[] { "W 50:7F 11", "R 50:80 2" },
        bus.Operations);
}
```

- [ ] **Step 2: Run the MSA test and verify it fails**

Run:

```powershell
dotnet test tests\OpenCMIS.Module.Core.Tests\OpenCMIS.Module.Core.Tests.csproj --filter "FullyQualifiedName~Msa"
```

Expected: compilation fails because `OpticalModuleSession` and `MsaMemoryAccessor` do not exist.

- [ ] **Step 3: Implement the session and MSA contract**

```csharp
public sealed class OpticalModuleSession : IAsyncDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    internal II2cRegisterBus Bus { get; }

    public OpticalModuleSession(II2cRegisterBus bus) =>
        Bus = bus ?? throw new ArgumentNullException(nameof(bus));

    internal async ValueTask<T> ExecuteAsync<T>(
        Func<II2cRegisterBus, CancellationToken, ValueTask<T>> operation,
        CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try { return await operation(Bus, cancellationToken).ConfigureAwait(false); }
        finally { _gate.Release(); }
    }

    public async ValueTask DisposeAsync()
    {
        await Bus.DisposeAsync().ConfigureAwait(false);
        _gate.Dispose();
    }
}

public interface IMsaMemoryAccessor
{
    ValueTask<byte[]> ReadAsync(
        I2cDeviceAddress device, ModulePage page, RegisterOffset offset, int length,
        CancellationToken cancellationToken = default);
    ValueTask WriteAsync(
        I2cDeviceAddress device, ModulePage page, RegisterOffset offset,
        ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default);
}
```

`MsaMemoryAccessor` validates length/range, enters `session.ExecuteAsync`, writes the page byte to register `0x7F`, then performs the read or write before releasing the gate. The first implementation always selects the requested page and keeps no independent cache.

Add `MsaPageSelectionFailed = 640` and use it only when the page-select write
fails; transfer failures after successful selection retain their transport code.

- [ ] **Step 4: Add a deterministic interleaving test**

```csharp
[Fact]
public async Task Concurrent_page_reads_cannot_interleave()
{
    var bus = new ScriptedI2cRegisterBus { PauseAfterFirstWrite = true };
    await using var session = new OpticalModuleSession(bus);
    var accessor = new MsaMemoryAccessor(session);

    var first = accessor.ReadAsync(Address, new ModulePage(1), Offset, 1).AsTask();
    await bus.FirstWriteObserved;
    var second = accessor.ReadAsync(Address, new ModulePage(2), Offset, 1).AsTask();

    Assert.Equal(new[] { "W 50:7F 01" }, bus.Operations);
    bus.Resume();
    await Task.WhenAll(first, second);
    Assert.Equal(
        new[] { "W 50:7F 01", "R 50:80 1", "W 50:7F 02", "R 50:80 1" },
        bus.Operations);
}
```

- [ ] **Step 5: Run module tests and solution build**

Run:

```powershell
dotnet test tests\OpenCMIS.Module.Core.Tests\OpenCMIS.Module.Core.Tests.csproj
dotnet build OpenCMIS.sln --no-restore
```

Expected: MSA sequencing, validation, cancellation, and non-interleaving tests pass.

- [ ] **Step 6: Commit the module session and MSA access**

```powershell
git add OpenCMIS.sln src\OpenCMIS.Module.Core tests\OpenCMIS.Module.Core.Tests
git commit -m "feat: add atomic optical module MSA access"
```

---

### Task 4: Add vendor HCI encoding, polling, and shared-session access

**Files:**

- Create: `src/OpenCMIS.Module.Core/Hci/IHciMemoryAccessor.cs`
- Create: `src/OpenCMIS.Module.Core/Hci/HciCommandCodec.cs`
- Create: `src/OpenCMIS.Module.Core/Hci/HciMemoryAccessor.cs`
- Create: `src/OpenCMIS.Module.Core/Hci/HciOptions.cs`
- Create: `src/OpenCMIS.Module.Core/Models/HciTableId.cs`
- Create: `tests/OpenCMIS.Module.Core.Tests/HciCommandCodecTests.cs`
- Create: `tests/OpenCMIS.Module.Core.Tests/HciMemoryAccessorTests.cs`
- Modify: `src/OpenCMIS.Shared/Enums/CmisErrorCode.cs`

**Interfaces:**

- Consumes: `OpticalModuleSession`, `II2cRegisterBus`.
- Produces: `IHciMemoryAccessor`, `HciCommandCodec`, `HciOptions`, and HCI-specific `CmisErrorCode` values.

- [ ] **Step 1: Write failing HCI packet characterization tests**

```csharp
[Fact]
public void EncodeRead_preserves_vendor_wire_format()
{
    Assert.Equal(
        new byte[] { 0x00, 0x00, 0x00, 0xAE, 0x0A, 0x80, 0x02 },
        HciCommandCodec.EncodeRead(new HciTableId(0xAE), new RegisterOffset(0x0A), 2));
}

[Fact]
public void EncodeWrite_appends_payload()
{
    Assert.Equal(
        new byte[] { 0x01, 0x00, 0x00, 0xA4, 0x08, 0x80, 0x02, 0x12, 0x34 },
        HciCommandCodec.EncodeWrite(
            new HciTableId(0xA4), new RegisterOffset(0x08), new byte[] { 0x12, 0x34 }));
}
```

- [ ] **Step 2: Run codec tests and verify they fail**

Run:

```powershell
dotnet test tests\OpenCMIS.Module.Core.Tests\OpenCMIS.Module.Core.Tests.csproj --filter "FullyQualifiedName~HciCommandCodec"
```

Expected: compilation fails because `HciCommandCodec` does not exist.

- [ ] **Step 3: Implement the HCI codec and options**

```csharp
public sealed record HciOptions
{
    public IReadOnlySet<byte> ReadyValues { get; init; } = new HashSet<byte> { 0x00 };
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(2);
    public TimeSpan InitialPollDelay { get; init; } = TimeSpan.FromMilliseconds(10);
    public TimeSpan MaximumPollDelay { get; init; } = TimeSpan.FromSeconds(1);
}

public static class HciCommandCodec
{
    public static byte[] EncodeRead(HciTableId table, RegisterOffset offset, int length);
    public static byte[] EncodeWrite(
        HciTableId table, RegisterOffset offset, ReadOnlySpan<byte> data);
    public static byte[] ExtractReadPayload(
        ReadOnlySpan<byte> response, int requestedLength);
}
```

Use the characterized seven-byte header, enforce payload lengths `1..255`, require an eight-byte response prefix before payload extraction, and throw `CmisException(HciInvalidResponse)` for malformed responses.

Add stable error values:

```csharp
HciCommandTimeout = 650,
HciInvalidResponse = 660,
HciCommandRejected = 670,
```

- [ ] **Step 4: Write failing HCI sequence, timeout, and cancellation tests**

```csharp
[Fact]
public async Task Read_executes_complete_sequence_and_extracts_payload()
{
    var bus = new ScriptedI2cRegisterBus();
    bus.QueueRead([0x01]); // first busy
    bus.QueueRead([0x00]); // first ready
    bus.QueueRead([0x00]); // second ready
    bus.QueueRead([0, 0, 0, 0, 0, 0, 0, 0, 0x12, 0x34]);
    var time = new FakeTimeProvider();
    await using var session = new OpticalModuleSession(bus);
    var accessor = new HciMemoryAccessor(session, new HciOptions(), time);

    var data = await accessor.ReadAsync(Address, new HciTableId(0xAE), Offset, 2);

    Assert.Equal(new byte[] { 0x12, 0x34 }, data);
    Assert.Contains("W 50:7F 7F", bus.Operations);
    Assert.Contains("W 50:81 00-00-00-AE-80-80-02", bus.Operations);
    Assert.Contains("W 50:80 7E", bus.Operations);
}

[Fact]
public async Task Busy_status_uses_TimeProvider_and_times_out()
{
    var bus = ScriptedI2cRegisterBus.AlwaysRead(0x01);
    var time = new FakeTimeProvider();
    await using var session = new OpticalModuleSession(bus);
    var accessor = new HciMemoryAccessor(
        session, new HciOptions { Timeout = TimeSpan.FromMilliseconds(20) }, time);
    var operation = accessor.ReadAsync(Address, Table, Offset, 1).AsTask();
    time.Advance(TimeSpan.FromMilliseconds(21));
    var error = await Assert.ThrowsAsync<CmisException>(() => operation);
    Assert.Equal(CmisErrorCode.HciCommandTimeout, error.ErrorCode);
}
```

- [ ] **Step 5: Implement HCI access inside the shared session gate**

```csharp
public interface IHciMemoryAccessor
{
    ValueTask<byte[]> ReadAsync(
        I2cDeviceAddress device, HciTableId table, RegisterOffset offset, int length,
        CancellationToken cancellationToken = default);
    ValueTask WriteAsync(
        I2cDeviceAddress device, HciTableId table, RegisterOffset offset,
        ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default);
}
```

Within one `session.ExecuteAsync`: write `0x7F` to register `0x7F`; poll
register `0x80`; write the encoded command to `0x81`; write `0x7E` to `0x80`;
poll again; for reads, fetch `length + 8` bytes from `0x80` and extract the
payload. Use `TimeProvider.GetTimestamp`,
`Task.Delay(delay, timeProvider, cancellationToken)`, capped exponential
delays, and the caller token. Propagate caller cancellation and convert only
elapsed timeout to `HciCommandTimeout`.

- [ ] **Step 6: Prove MSA and HCI cannot interleave**

```csharp
[Fact]
public async Task Msa_cannot_interleave_with_Hci()
{
    var bus = ScriptedI2cRegisterBus.ReadyHciResponse([0x42]);
    bus.PauseAfterOperation("W 50:7F 7F");
    await using var session = new OpticalModuleSession(bus);
    var hci = new HciMemoryAccessor(session, new HciOptions(), TimeProvider.System);
    var msa = new MsaMemoryAccessor(session);

    var hciTask = hci.ReadAsync(Address, Table, Offset, 1).AsTask();
    await bus.PauseObserved;
    var msaTask = msa.ReadAsync(Address, new ModulePage(0x11), Offset, 1).AsTask();

    Assert.DoesNotContain("W 50:7F 11", bus.Operations);
    bus.Resume();
    await Task.WhenAll(hciTask, msaTask);
    Assert.True(
        bus.Operations.IndexOf("W 50:7F 11") >
        bus.Operations.IndexOf("R 50:80 9"));
}
```

- [ ] **Step 7: Run HCI/module tests and build**

Run:

```powershell
dotnet test tests\OpenCMIS.Module.Core.Tests\OpenCMIS.Module.Core.Tests.csproj
dotnet build OpenCMIS.sln --no-restore
```

Expected: packet, busy-to-ready, timeout, cancellation, invalid response, and MSA/HCI serialization tests pass.

- [ ] **Step 8: Commit HCI support**

```powershell
git add src\OpenCMIS.Module.Core src\OpenCMIS.Shared\Enums\CmisErrorCode.cs tests\OpenCMIS.Module.Core.Tests
git commit -m "feat: add synchronized vendor HCI access"
```

---

### Task 5: Move CMIS semantics onto Module.Core and preserve the public facade

**Files:**

- Modify: `src/OpenCMIS.Protocol.Core/OpenCMIS.Protocol.Core.csproj`
- Modify: `src/OpenCMIS.Protocol.Core/Implementations/RegisterAccess.cs`
- Modify: `src/OpenCMIS.Protocol.Core/Implementations/PageManager.cs`
- Modify: `src/OpenCMIS.App.Core/OpenCMIS.App.Core.csproj`
- Modify: `src/OpenCMIS.App.Core/CmisDevice.cs`
- Move: `src/OpenCMIS.Shared/Models/ModuleIdentity.cs` to `src/OpenCMIS.Protocol.Abstractions/Models/ModuleIdentity.cs`
- Move: `src/OpenCMIS.Shared/Models/ModuleMonitors.cs` to `src/OpenCMIS.Protocol.Abstractions/Models/ModuleMonitors.cs`
- Move: `src/OpenCMIS.Shared/Models/MonitorValue.cs` to `src/OpenCMIS.Protocol.Abstractions/Models/MonitorValue.cs`
- Move: `src/OpenCMIS.Shared/Models/LaneStatus.cs` to `src/OpenCMIS.Protocol.Abstractions/Models/LaneStatus.cs`
- Move: `src/OpenCMIS.Shared/Models/ModuleDashData.cs` to `src/OpenCMIS.Protocol.Abstractions/Models/ModuleDashData.cs`
- Move: `src/OpenCMIS.Transport.Abstractions/Models/ModuleInfo.cs` to `src/OpenCMIS.Protocol.Abstractions/Models/ModuleInfo.cs`
- Move: `src/OpenCMIS.Transport.Abstractions/Models/ModuleStatus.cs` to `src/OpenCMIS.Protocol.Abstractions/Models/ModuleStatus.cs`
- Modify: UI files that import the old namespaces
- Create: `tests/OpenCMIS.App.Core.Tests/OpenCMIS.App.Core.Tests.csproj`
- Create: `tests/OpenCMIS.App.Core.Tests/CmisDeviceCompatibilityTests.cs`

**Interfaces:**

- Consumes: `IMsaMemoryAccessor`, `IHciMemoryAccessor`, existing CMIS addressing/parsing code.
- Produces: the existing `ICmisDevice` behavior plus a separately injectable `IHciMemoryAccessor`.

- [ ] **Step 1: Write failing compatibility tests around current high-level behavior**

```csharp
[Fact]
public async Task ReadModuleIdentity_uses_MSA_and_preserves_existing_mapping()
{
    var msa = new StubMsaMemoryAccessor()
        .Returns(0x00, 0x00, [0x18])
        .Returns(0x00, 0x81, Ascii("VENDOR          "))
        .Returns(0x00, 0x94, Ascii("PART-NUMBER     "));
    var device = CmisDeviceTestFactory.Create(msa);

    var identity = await device.ReadModuleIdentityAsync();

    Assert.Equal(0x18, identity.Identifier);
    Assert.Equal("VENDOR", identity.VendorName);
    Assert.Equal("PART-NUMBER", identity.PartNumber);
}
```

```csharp
[Fact]
public async Task ReadModuleMonitors_scales_temperature_Vcc_power_and_bias()
{
    var msa = new StubMsaMemoryAccessor()
        .Returns(0x00, CmisConstants.RegTemperatureMSB, [0x00, 0x01])
        .Returns(0x00, CmisConstants.RegVccMSB, [0x10, 0x27])
        .Returns(CmisConstants.FirstLanePage, CmisConstants.RegLaneTxBiasMSB, [0xF4, 0x01])
        .Returns(CmisConstants.FirstLanePage, CmisConstants.RegLaneTxPowerMSB, [0x10, 0x27])
        .Returns(CmisConstants.FirstLanePage, CmisConstants.RegLaneRxPowerMSB, [0x20, 0x4E]);
    var device = CmisDeviceTestFactory.Create(msa);

    var monitors = await device.ReadModuleMonitorsAsync(1);

    Assert.Equal(1.00, monitors.Temperature.Value);
    Assert.Equal(1.0000, monitors.VCC.Value);
    Assert.Equal(1.000, monitors.TxBiasPerLane[0].Value);
    Assert.Equal(1.0000, monitors.TxPowerPerLane[0].Value);
    Assert.Equal(2.0000, monitors.RxPowerPerLane[0].Value);
}

[Fact]
public async Task ReadLaneStatus_maps_each_requested_lane()
{
    var msa = new StubMsaMemoryAccessor()
        .Returns(CmisConstants.FirstLanePage, CmisConstants.RegLaneStatusFlags, [0x03])
        .Returns(CmisConstants.FirstLanePage, CmisConstants.RegLaneTxPowerMSB, [0x10, 0x27])
        .Returns(CmisConstants.FirstLanePage, CmisConstants.RegLaneRxPowerMSB, [0x20, 0x4E])
        .Returns(CmisConstants.FirstLanePage, CmisConstants.RegLaneTxBiasMSB, [0xF4, 0x01]);
    var device = CmisDeviceTestFactory.Create(msa);

    var lanes = await device.ReadLaneStatusAsync(1);

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

[Fact]
public async Task CloseAsync_disposes_the_single_optical_module_session()
{
    var bus = new ScriptedI2cRegisterBus();
    var session = new OpticalModuleSession(bus);
    var device = CmisDeviceTestFactory.Create(session);

    await device.CloseAsync();

    Assert.True(bus.IsDisposed);
}
```

Also add `ReadModuleDashData_composes_identity_monitors_lanes_and_status` using
the union of the identity/monitor/lane bytes above plus state byte `0x03`, status
byte `0x01`, and interrupt bytes `[0x00, 0x00]`. Assert that the returned object
contains vendor `VENDOR`, one lane, state `(ModuleState)0x03`, `IsReady == true`,
and a non-default `StatusTimestamp`. These are characterization values from the
current implementation, not newly simplified formulas.

- [ ] **Step 2: Run compatibility tests and verify they fail against the new factory**

Run:

```powershell
dotnet test tests\OpenCMIS.App.Core.Tests\OpenCMIS.App.Core.Tests.csproj --filter "FullyQualifiedName~Compatibility"
```

Expected: compilation fails because the new `CmisDevice` constructor/factory is not implemented.

- [ ] **Step 3: Make `RegisterAccess` a compatibility adapter over MSA**

```csharp
public sealed class RegisterAccess : IRegisterAccess
{
    private readonly IMsaMemoryAccessor _msa;
    private readonly I2cDeviceAddress _device;

    public RegisterAccess(IMsaMemoryAccessor msa, I2cDeviceAddress device)
    {
        _msa = msa;
        _device = device;
    }

    public async Task<byte[]> ReadBlockAsync(byte page, byte startAddress, int length) =>
        await _msa.ReadAsync(
            _device, new ModulePage(page), new RegisterOffset(startAddress), length);
    public async Task<byte> ReadByteAsync(byte page, byte address) =>
        (await ReadBlockAsync(page, address, 1))[0];
    public Task WriteByteAsync(byte page, byte address, byte value) =>
        WriteBlockAsync(page, address, [value]);
    public Task WriteBlockAsync(byte page, byte startAddress, byte[] data) =>
        _msa.WriteAsync(
            _device, new ModulePage(page), new RegisterOffset(startAddress), data).AsTask();
}
```

Mark `IPageManager`/`PageManager` obsolete; remove their independent state from the active path.

- [ ] **Step 4: Split `CmisDevice` into focused services while retaining its facade**

Create these focused internal types:

```csharp
internal sealed class CmisIdentityReader(IMsaMemoryAccessor msa, I2cDeviceAddress device)
{
    public Task<ModuleIdentity> ReadAsync(CancellationToken cancellationToken = default);
}

internal sealed class CmisMonitorReader(IMsaMemoryAccessor msa, I2cDeviceAddress device)
{
    public Task<ModuleMonitors> ReadAsync(
        int laneCount, CancellationToken cancellationToken = default);
}

internal sealed class CmisLaneReader(IMsaMemoryAccessor msa, I2cDeviceAddress device)
{
    public Task<List<LaneStatus>> ReadAsync(
        int laneCount, CancellationToken cancellationToken = default);
}

internal sealed class CmisApplicationService(
    IMsaMemoryAccessor msa, I2cDeviceAddress device)
{
    public Task<IReadOnlyList<CmisApplication>> ReadAsync(
        CancellationToken cancellationToken = default);
    public Task SelectAsync(
        byte applicationCode, CancellationToken cancellationToken = default);
}
```

`CmisDevice` delegates each `ICmisDevice` member to one service, exposes the
compatibility `IRegisterAccess`, and disposes the single `OpticalModuleSession`.
Copy the existing conversion formulas and register constants into the focused
service that owns them; do not change their values in this task.

- [ ] **Step 5: Move public models and repair namespaces**

Set public model namespaces to `OpenCMIS.Protocol.Abstractions.Models`. Update `ICmisDevice`, CDB, App, CLI, WPF, and tests. Do not leave duplicate model definitions in Shared or Transport.

- [ ] **Step 6: Run compatibility tests and build**

Run:

```powershell
dotnet test tests\OpenCMIS.App.Core.Tests\OpenCMIS.App.Core.Tests.csproj
dotnet test tests\OpenCMIS.Module.Core.Tests\OpenCMIS.Module.Core.Tests.csproj
dotnet build OpenCMIS.sln --no-restore
```

Expected: high-level CMIS workflows remain green and the active Protocol/App path no longer references `IRegisterTransport`.

- [ ] **Step 7: Commit the CMIS facade migration**

```powershell
git add src\OpenCMIS.Protocol.Abstractions src\OpenCMIS.Protocol.Core src\OpenCMIS.App.Core src\OpenCMIS.Shared src\OpenCMIS.Transport.Abstractions src\OpenCMIS.UI.CLI src\OpenCMIS.UI.WPF tests\OpenCMIS.App.Core.Tests
git commit -m "refactor: move CMIS workflows onto module core"
```

---

### Task 6: Replace direct connector construction with adapter providers

**Files:**

- Create: `src/OpenCMIS.App.Core/IOpticalModuleFactory.cs`
- Create: `src/OpenCMIS.App.Core/OpticalModuleFactory.cs`
- Modify: `src/OpenCMIS.App.Core/DeviceManager.cs`
- Modify: `src/OpenCMIS.App.Core/ServiceCollectionExtensions.cs`
- Create: `src/OpenCMIS.Transport.I2C.Serial/Providers/LinktelSerialAdapterProvider.cs`
- Create: `src/OpenCMIS.Transport.I2C.Serial/Providers/HmSerialAdapterProvider.cs`
- Create: `src/OpenCMIS.Transport.I2C.Serial/Providers/HmMultiChannelAdapterProvider.cs`
- Create: `tests/OpenCMIS.App.Core.Tests/DeviceManagerTests.cs`

**Interfaces:**

- Consumes: `II2cAdapterProvider`, typed profiles, `OpticalModuleSession`.
- Produces: provider-based `DeviceManager` with legacy dictionary conversion only at its compatibility boundary.

- [ ] **Step 1: Write failing provider-selection and discovery tests**

```csharp
[Fact]
public async Task OpenDevice_selects_provider_from_typed_profile()
{
    var expectedBus = new ScriptedI2cRegisterBus();
    var provider = new StubProvider("linktel", expectedBus);
    var manager = DeviceManagerTestFactory.Create(provider);
    var info = DeviceInfoTestFactory.For(
        new SerialI2cConnectionProfile("linktel", "COM7", 115200, Address));

    await manager.OpenDeviceAsync(info);

    Assert.Same(info.Profile, provider.OpenedProfile);
}

[Fact]
public async Task Enumeration_combines_all_providers_and_keeps_probe_failures()
{
    var manager = DeviceManagerTestFactory.Create(
        StubProvider.Returning("linktel", LinktelDescriptor),
        StubProvider.Failing("hm", "COM8", "Access denied"));
    var devices = await manager.EnumerateDevicesAsync();
    Assert.Single(devices);
    Assert.Single(manager.LastProbeFailures);
}
```

- [ ] **Step 2: Run provider tests and verify they fail**

Run:

```powershell
dotnet test tests\OpenCMIS.App.Core.Tests\OpenCMIS.App.Core.Tests.csproj --filter "FullyQualifiedName~DeviceManager"
```

Expected: tests fail because `DeviceManager` still constructs `I2CConnectorTypeA`.

- [ ] **Step 3: Implement provider-based discovery/opening**

```csharp
public sealed class DeviceManager : IDeviceManager
{
    private readonly IReadOnlyDictionary<string, II2cAdapterProvider> _providers;
    private readonly IOpticalModuleFactory _moduleFactory;

    public DeviceManager(
        IEnumerable<II2cAdapterProvider> providers,
        IOpticalModuleFactory moduleFactory);

    public IReadOnlyList<I2cProbeFailure> LastProbeFailures { get; private set; }

    // Enumerate providers independently; OpenDevice selects by Profile.AdapterId.
}
```

Add `I2cConnectionProfile? Profile` to `DeviceInfo`. If only `ConnectionParameters` exists, convert `ConnectorType=TypeA` to adapter ID `linktel`, `TypeB` to `hm`, parse the legacy address with `FromLegacy8Bit`, and reject missing/invalid values with `InvalidParameterValue`.

- [ ] **Step 4: Move composition to hosts**

`AddOpenCmisCore` registers only core factories/services. Add `AddOpenCmisSerialAdapters` in the serial project to register its providers. CLI and WPF call both methods; App.Core removes its project reference to the concrete serial project.

- [ ] **Step 5: Run app tests and verify dependency direction**

Run:

```powershell
dotnet test tests\OpenCMIS.App.Core.Tests\OpenCMIS.App.Core.Tests.csproj
dotnet build OpenCMIS.sln --no-restore
dotnet list src\OpenCMIS.App.Core\OpenCMIS.App.Core.csproj reference
```

Expected: tests pass; the reference list contains no `OpenCMIS.Transport.I2C.Serial`, legacy `OpenCMIS.Transport.I2C`, or Cypress project.

- [ ] **Step 6: Commit provider-based composition**

```powershell
git add src\OpenCMIS.App.Core src\OpenCMIS.Transport.Abstractions src\OpenCMIS.Transport.I2C.Serial src\OpenCMIS.UI.CLI src\OpenCMIS.UI.WPF tests\OpenCMIS.App.Core.Tests
git commit -m "refactor: compose I2C devices through providers"
```

---

### Task 7: Import Cypress source behind a license gate

**Files:**

- Create: `docs/third-party/cypress-license-review.md`
- Create: `src/OpenCMIS.Cypress/OpenCMIS.Cypress.csproj`
- Create: `src/OpenCMIS.Cypress/THIRD-PARTY-NOTICES.md`
- Copy after authorization: `E:\Code Pulse\pulse.instruments.cypress\src\Pulse.Instruments.Cypress\Cypress\**`
- Copy after authorization: `E:\Code Pulse\pulse.instruments.cypress\src\Pulse.Instruments.Cypress\CyPresses\**`
- Copy after authorization: signing key or replace signing configuration only as explicitly permitted by the license/build policy
- Modify: namespaces and assembly metadata only where required for standalone compilation
- Modify: `OpenCMIS.sln`

**Interfaces:**

- Consumes: authorized Cypress/CyUSB source.
- Produces: a Windows-only low-level `OpenCMIS.Cypress` assembly with no Pulse dependency.

- [ ] **Step 1: Record the license decision before copying source**

Create `docs/third-party/cypress-license-review.md` containing the source repository/path, copyright holder, applicable license file, allowed internal/repository redistribution scope, required notices, signing-key handling, reviewer, and review date. If redistribution is not authorized, stop this task and leave Tasks 1–6 usable without Cypress.

- [ ] **Step 2: Create the Windows-only project**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0-windows</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
    <UseWindowsForms>true</UseWindowsForms>
    <RootNamespace>OpenCMIS.Cypress</RootNamespace>
    <AssemblyName>OpenCMIS.Cypress</AssemblyName>
  </PropertyGroup>
</Project>
```

- [ ] **Step 3: Copy the authorized source and preserve notices**

Copy only the source files required by `CyUSBDevices`, `DeviceFIC2USB`, and `DeviceEUI3`, plus their transitive types. Preserve original copyright headers. Replace `Pulse.Instruments.Cypress` namespaces consistently with `OpenCMIS.Cypress`; do not alter P/Invoke signatures, USB constants, or binary protocol behavior during the import commit.

- [ ] **Step 4: Build the imported project on Windows**

Run:

```powershell
dotnet build src\OpenCMIS.Cypress\OpenCMIS.Cypress.csproj --no-restore
```

Expected: Windows-targeted source compiles with zero errors; this is build-only verification and does not load `CyUSB3.sys` or open hardware.

- [ ] **Step 5: Commit the authorized source import**

```powershell
git add OpenCMIS.sln docs\third-party src\OpenCMIS.Cypress
git commit -m "build: import authorized Cypress source"
```

---

### Task 8: Add mocked FIC2USB and EUI3 I2C adapters

**Files:**

- Create: `src/OpenCMIS.Transport.I2C.Cypress/OpenCMIS.Transport.I2C.Cypress.csproj`
- Create: `src/OpenCMIS.Transport.I2C.Cypress/ICypressDeviceApi.cs`
- Create: `src/OpenCMIS.Transport.I2C.Cypress/CypressDeviceApi.cs`
- Create: `src/OpenCMIS.Transport.I2C.Cypress/Fic2UsbI2cAdapter.cs`
- Create: `src/OpenCMIS.Transport.I2C.Cypress/Eui3I2cAdapter.cs`
- Create: `src/OpenCMIS.Transport.I2C.Cypress/CypressI2cAdapterProvider.cs`
- Create: `tests/OpenCMIS.Transport.I2C.Cypress.Tests/OpenCMIS.Transport.I2C.Cypress.Tests.csproj`
- Create: `tests/OpenCMIS.Transport.I2C.Cypress.Tests/CypressI2cAdapterTests.cs`
- Create: `tests/OpenCMIS.Transport.I2C.Cypress.Tests/CypressI2cAdapterProviderTests.cs`
- Create: `tests/OpenCMIS.Transport.I2C.Cypress.Tests/Fakes/MockCypressDeviceApi.cs`
- Modify: `OpenCMIS.sln`

**Interfaces:**

- Consumes: `II2cRegisterBus`, `CypressI2cConnectionProfile`, and low-level `OpenCMIS.Cypress`.
- Produces: `Fic2UsbI2cAdapter`, `Eui3I2cAdapter`, and `CypressI2cAdapterProvider`.

- [ ] **Step 1: Write failing mocked adapter tests**

```csharp
[Fact]
public async Task Fic2Usb_maps_address_port_speed_and_register_prefix()
{
    var api = new MockCypressDeviceApi();
    await using var adapter = new Fic2UsbI2cAdapter(api, port: 1, speedKhz: 100);
    await adapter.WriteAsync(
        new I2cDeviceAddress(0x50), new RegisterOffset(0x80),
        new byte[] { 0x12, 0x34 });

    Assert.Equal(1, api.LastPort);
    Assert.Equal(100, api.LastSpeedKhz);
    Assert.Equal(0xA0, api.LastDeviceAddress8Bit);
    Assert.Equal(new byte[] { 0x80, 0x12, 0x34 }, api.LastWrite);
}

[Fact]
public async Task Eui3_failure_is_converted_to_I2cTransferFailed()
{
    var api = new MockCypressDeviceApi { TransferResult = false };
    await using var adapter = new Eui3I2cAdapter(api, port: 0, speedKhz: 400);
    var error = await Assert.ThrowsAsync<CmisException>(() =>
        adapter.ReadAsync(Address, Offset, new byte[1]).AsTask());
    Assert.Equal(CmisErrorCode.I2cTransferFailed, error.ErrorCode);
}
```

- [ ] **Step 2: Run Cypress adapter tests and verify missing-type failures**

Run:

```powershell
dotnet test tests\OpenCMIS.Transport.I2C.Cypress.Tests\OpenCMIS.Transport.I2C.Cypress.Tests.csproj --no-restore
```

Expected: compilation fails because the Cypress adapter project does not exist.

- [ ] **Step 3: Add the mockable low-level boundary**

```csharp
public interface ICypressDeviceApi : IAsyncDisposable
{
    IReadOnlyList<CypressDeviceDescriptor> Discover();
    bool Open(string serialNumber);
    bool Read(int port, int speedKhz, byte address8Bit, int length, out byte[] data);
    bool Write(int port, int speedKhz, byte address8Bit, ReadOnlySpan<byte> data);
    void Close();
}
```

`CypressDeviceApi` is the only class that references `CyUSBDevices`, `DeviceFIC2USB`, or `DeviceEUI3`. Keep all low-level blocking calls behind this boundary.

- [ ] **Step 4: Implement both adapters and provider**

Both adapters implement `II2cRegisterBus`, convert the canonical address with `ToWriteAddress8Bit`, prefix writes with the register offset, perform the low-level transfer on a worker thread with cancellation observed before and after the call, segment according to device limits, and convert `false`/malformed results to `CmisException(I2cTransferFailed)`. The provider filters discovery by device type and maps serial number, port, and speed into `CypressI2cConnectionProfile`.

- [ ] **Step 5: Run mocked tests and Windows builds**

Run:

```powershell
dotnet test tests\OpenCMIS.Transport.I2C.Cypress.Tests\OpenCMIS.Transport.I2C.Cypress.Tests.csproj
dotnet build src\OpenCMIS.Transport.I2C.Cypress\OpenCMIS.Transport.I2C.Cypress.csproj --no-restore
dotnet build OpenCMIS.sln --no-restore
```

Expected: discovery, mapping, segmentation, failures, cancellation boundaries, and disposal tests pass; no hardware is opened.

- [ ] **Step 6: Commit Cypress adapters**

```powershell
git add OpenCMIS.sln src\OpenCMIS.Transport.I2C.Cypress tests\OpenCMIS.Transport.I2C.Cypress.Tests
git commit -m "feat: add Windows Cypress I2C adapters"
```

---

### Task 9: Compose platform-specific providers and remove obsolete active dependencies

**Files:**

- Modify: `src/OpenCMIS.UI.CLI/OpenCMIS.UI.CLI.csproj`
- Modify: `src/OpenCMIS.UI.CLI/Program.cs`
- Modify: `src/OpenCMIS.UI.WPF/OpenCMIS.UI.WPF.csproj`
- Modify: `src/OpenCMIS.UI.WPF/App.xaml.cs`
- Modify: `src/OpenCMIS.App.Core/OpenCMIS.App.Core.csproj`
- Delete after reference scan: old active `OpenCMIS.Transport.I2C` implementation files not needed by compatibility wrappers
- Modify: `README.md`
- Create: `docs/architecture/optical-module-i2c.md`

**Interfaces:**

- Consumes: serial and Cypress DI registration methods.
- Produces: cross-platform CLI composition and Windows WPF composition.

- [ ] **Step 1: Add host composition assertions**

Add a CLI composition test resolving only `LinktelSerialAdapterProvider`, `HmSerialAdapterProvider`, and `HmMultiChannelAdapterProvider`. Add a WPF composition test resolving those providers plus `CypressI2cAdapterProvider`; guard the WPF test with Windows targeting, not a runtime skip.

- [ ] **Step 2: Register providers at the correct host boundary**

```csharp
// CLI
services.AddOpenCmisCore();
services.AddOpenCmisSerialAdapters();

// WPF
services.AddOpenCmisCore();
services.AddOpenCmisSerialAdapters();
services.AddOpenCmisCypressAdapters();
```

CLI must not reference either Cypress project. WPF references `OpenCMIS.Transport.I2C.Cypress`, which transitively owns the low-level Windows assembly.

- [ ] **Step 3: Remove obsolete active references**

Run `rg -n "I2CConnectorTypeA|I2CConnectorTypeB|IRegisterTransport|OpenCMIS\\.Transport\\.I2C"` over `src`. Keep obsolete wrappers only if an external compatibility surface still needs them; otherwise remove the legacy project from the solution. Confirm `App.Core`, Protocol, Module, and CLI have no concrete adapter references.

- [ ] **Step 4: Document usage and platform limits**

Document:

- canonical `0x50` address usage and legacy `0xA0` conversion
- provider IDs and typed profiles
- MSA and HCI APIs
- CLI serial-only behavior
- WPF Cypress availability on Windows
- simulated-only verification status
- future hardware validation matrix

- [ ] **Step 5: Run host builds and reference audits**

Run:

```powershell
dotnet build src\OpenCMIS.UI.CLI\OpenCMIS.UI.CLI.csproj --no-restore
dotnet build src\OpenCMIS.UI.WPF\OpenCMIS.UI.WPF.csproj --no-restore
dotnet list src\OpenCMIS.UI.CLI\OpenCMIS.UI.CLI.csproj reference
dotnet list src\OpenCMIS.App.Core\OpenCMIS.App.Core.csproj reference
```

Expected: both hosts build; CLI/App reference output contains no Cypress project; App contains no concrete serial project.

- [ ] **Step 6: Commit host composition and documentation**

```powershell
git add OpenCMIS.sln src README.md docs\architecture
git commit -m "refactor: isolate platform-specific I2C composition"
```

---

### Task 10: Run the full simulated verification and capture remaining hardware work

**Files:**

- Modify: `README.md`
- Modify: `docs/architecture/optical-module-i2c.md`
- Create: `docs/verification/2026-07-31-simulated-i2c-verification.md`

**Interfaces:**

- Consumes: all preceding projects and tests.
- Produces: evidence-backed completion report and explicit real-hardware follow-up matrix.

- [ ] **Step 1: Verify target frameworks and dependency boundaries**

Run:

```powershell
rg -n "<TargetFramework>" src tests -g "*.csproj"
rg -n "Pulse\\.|Pulse.Instruments.Cypress|PackageReference.*Cypress" src tests -g "*.csproj" -g "*.cs"
dotnet list src\OpenCMIS.Module.Core\OpenCMIS.Module.Core.csproj reference
dotnet list src\OpenCMIS.Protocol.Core\OpenCMIS.Protocol.Core.csproj reference
dotnet list src\OpenCMIS.App.Core\OpenCMIS.App.Core.csproj reference
dotnet list src\OpenCMIS.UI.CLI\OpenCMIS.UI.CLI.csproj reference
```

Expected: frameworks match Global Constraints; Pulse search returns no dependency; inward projects have no concrete serial/Cypress references.

- [ ] **Step 2: Run the complete simulated suite**

Run:

```powershell
dotnet test OpenCMIS.sln --no-restore --configuration Release
dotnet build OpenCMIS.sln --no-restore --configuration Release
```

Expected: every test and project passes with zero warnings and errors.

- [ ] **Step 3: Record verification scope honestly**

Create the verification document with command outputs summarized by project and test count. State explicitly:

```text
Verified: value validation, wire-frame encoding/parsing, partial-read handling,
transfer segmentation, provider selection, MSA/HCI operation ordering,
concurrency serialization, timeout behavior, cancellation, and error conversion.

Not verified: communication with a physical optical module, Linktel bridge,
HM bridge, HM multichannel bridge, FIC2USB, EUI3, CyUSB3.sys installation,
USB hot-plug behavior, or electrical/timing compatibility.
```

Include a future hardware matrix with one row each for Linktel, HM, HM multichannel, FIC2USB, and EUI3, covering discovery, open/close, Page 0 read, Page 17 read, HCI read, HCI write, timeout, cancellation, and repeated reconnect.

- [ ] **Step 4: Review the diff and commit final verification**

Run:

```powershell
git diff --check
git status --short
git diff --stat master...HEAD
```

Then:

```powershell
git add README.md docs\architecture docs\verification
git commit -m "docs: record simulated I2C verification"
```

- [ ] **Step 5: Perform the completion review**

Use `superpowers:verification-before-completion`, then `superpowers:requesting-code-review`. Do not merge or push until the user selects the integration action.
