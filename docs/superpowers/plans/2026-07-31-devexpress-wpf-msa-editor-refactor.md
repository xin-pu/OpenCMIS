# DevExpress WPF MSA Editor Refactor Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the OpenCMIS WPF presentation with DevExpress MVVM and deliver a compact adapter/device selector plus a raw-byte-first, directly writable MSA page editor with automatic read-back verification.

**Architecture:** Keep DevExpress confined to `OpenCMIS.UI.WPF`. Reuse `IDeviceManager` for discovery and connection, add one shared `DeviceSession`, and keep MSA edit state in `MsaPageViewModel` plus small immutable edit models. Extend the existing MSA core page value with a bank while preserving the one-argument page constructor.

**Tech Stack:** .NET 10, WPF, DevExpress WPF 25.2.4, DevExpress MVVM 25.2.4, DevExpress.Mvvm.CodeGenerators 22.1.1, Microsoft.Extensions.Hosting, xUnit 2.9.3

## Global Constraints

- Target framework remains `net10.0-windows` for WPF and WPF tests.
- All DevExpress runtime packages use exactly `25.2.4`.
- `DevExpress.Mvvm.CodeGenerators` uses exactly `22.1.1`, the latest published official DevExpress analyzer.
- Remove `MaterialDesignThemes` and `CommunityToolkit.Mvvm` from `OpenCMIS.UI.WPF` after all views and ViewModels migrate.
- Do not add DevExpress references outside `OpenCMIS.UI.WPF` and `OpenCMIS.UI.WPF.Tests`.
- Preserve complete `DeviceInfo.Profile` instances during discovery and connection.
- Default writes contain only modified writable bytes; full-page write remains an explicit advanced command.
- Every successful write is followed by a read of the same Bank/Page and byte-for-byte verification.
- Failed writes and read-back mismatches preserve the user's editable snapshot.
- Tests remain simulated; do not claim physical hardware verification.
- Keep `.superpowers/` visual-companion artifacts untracked.

---

## File Structure

### New production files

- `src/OpenCMIS.UI.WPF/Services/DeviceSession.cs` — one shared active-device state.
- `src/OpenCMIS.UI.WPF/Models/AdapterChoice.cs` — display/filter projection for an adapter ID.
- `src/OpenCMIS.UI.WPF/Models/MsaByteChange.cs` — immutable old/new byte difference.
- `src/OpenCMIS.UI.WPF/Models/MsaWriteSegment.cs` — one contiguous write operation.
- `src/OpenCMIS.UI.WPF/Models/MsaPageBuffer.cs` — original/edit/read-back buffers and dirty tracking.
- `src/OpenCMIS.UI.WPF/ViewModels/MsaPageViewModel.cs` — Bank/Page read, edit, write, and verify workflow.
- `src/OpenCMIS.UI.WPF/ViewModels/MsaHexRowViewModel.cs` — 16-byte row projection for the grid.
- `src/OpenCMIS.UI.WPF/ViewModels/MsaWriteConfirmationViewModel.cs` — difference-dialog content.
- `src/OpenCMIS.UI.WPF/Services/IMsaWriteConfirmation.cs` — testable boundary for the DevExpress dialog.
- `src/OpenCMIS.UI.WPF/Services/DevExpressMsaWriteConfirmation.cs` — DevExpress dialog implementation.
- `src/OpenCMIS.UI.WPF/Resources/Colors.xaml` — OpenCMIS color tokens used with Win11Light.
- `src/OpenCMIS.UI.WPF/Resources/CompactStyles.xaml` — shared compact spacing and editor styles.

### New test project

- `tests/OpenCMIS.UI.WPF.Tests/OpenCMIS.UI.WPF.Tests.csproj`
- `tests/OpenCMIS.UI.WPF.Tests/DevExpressGenerationTests.cs`
- `tests/OpenCMIS.UI.WPF.Tests/DeviceSessionTests.cs`
- `tests/OpenCMIS.UI.WPF.Tests/DeviceConnectionViewModelTests.cs`
- `tests/OpenCMIS.UI.WPF.Tests/MsaPageBufferTests.cs`
- `tests/OpenCMIS.UI.WPF.Tests/MsaPageViewModelTests.cs`
- `tests/OpenCMIS.UI.WPF.Tests/Fakes/FakeCmisDevice.cs`
- `tests/OpenCMIS.UI.WPF.Tests/Fakes/FakeDeviceManager.cs`
- `tests/OpenCMIS.UI.WPF.Tests/Fakes/FakeRegisterAccess.cs`
- `tests/OpenCMIS.UI.WPF.Tests/Fakes/AcceptWriteConfirmation.cs`

### Replaced or modified WPF files

- `src/OpenCMIS.UI.WPF/OpenCMIS.UI.WPF.csproj`
- `src/OpenCMIS.UI.WPF/App.xaml`
- `src/OpenCMIS.UI.WPF/App.xaml.cs`
- all files under `src/OpenCMIS.UI.WPF/ViewModels`
- all XAML files under `src/OpenCMIS.UI.WPF/Views`
- `src/OpenCMIS.UI.WPF/Views/MainWindow.xaml.cs`

### Modified core files

- `src/OpenCMIS.Module.Core/Models/ModulePage.cs`
- `src/OpenCMIS.Module.Core/Msa/MsaMemoryAccessor.cs`
- `src/OpenCMIS.Protocol.Abstractions/Interfaces/IRegisterAccess.cs`
- `src/OpenCMIS.Protocol.Core/Implementations/RegisterAccess.cs`
- `tests/OpenCMIS.Module.Core.Tests/MsaMemoryAccessorTests.cs`
- `tests/OpenCMIS.App.Core.Tests/BankedRegisterAccessTests.cs`

---

### Task 1: Add Bank-Aware Atomic MSA Access

**Files:**
- Modify: `src/OpenCMIS.Module.Core/Models/ModulePage.cs`
- Modify: `src/OpenCMIS.Module.Core/Msa/MsaMemoryAccessor.cs`
- Modify: `src/OpenCMIS.Protocol.Abstractions/Interfaces/IRegisterAccess.cs`
- Modify: `src/OpenCMIS.Protocol.Core/Implementations/RegisterAccess.cs`
- Modify: `tests/OpenCMIS.Module.Core.Tests/MsaMemoryAccessorTests.cs`
- Create: `tests/OpenCMIS.App.Core.Tests/BankedRegisterAccessTests.cs`

**Interfaces:**
- Produces: `ModulePage(byte value)`, `ModulePage(byte bank, byte value)`, `byte Bank`, and `byte Value`.
- Produces:
  `ReadBlockAsync(byte bank, byte page, byte startAddress, int length)` and
  `WriteBlockAsync(byte bank, byte page, byte startAddress, byte[] data)`
  while preserving the existing bank-zero overloads.
- Consumes: existing `IMsaMemoryAccessor` signatures remain unchanged.

- [ ] **Step 1: Write failing bank-selection tests**

Add tests that preserve the old constructor and require register `0x7E` to be
selected before register `0x7F`:

```csharp
[Fact]
public void Page_without_bank_defaults_to_zero()
{
    var page = new ModulePage(0x11);
    Assert.Equal(0, page.Bank);
    Assert.Equal(0x11, page.Value);
}

[Fact]
public async Task Read_selects_bank_then_page_then_reads()
{
    var bus = new ScriptedI2cRegisterBus();
    bus.QueueRead([0xAB]);
    await using var session = new OpticalModuleSession(bus);
    await session.OpenAsync();
    var accessor = new MsaMemoryAccessor(session);

    var result = await accessor.ReadAsync(
        Address,
        new ModulePage(0x02, 0x11),
        Offset,
        1);

    Assert.Equal(new byte[] { 0xAB }, result);
    Assert.Equal(
        new[] { "W 50:7E 02", "W 50:7F 11", "R 50:80 1" },
        bus.Operations);
}
```

- [ ] **Step 2: Run the focused tests and verify failure**

Run:

```powershell
dotnet test .\tests\OpenCMIS.Module.Core.Tests\OpenCMIS.Module.Core.Tests.csproj -c Release --filter "FullyQualifiedName~MsaMemoryAccessorTests"
```

Expected: compilation fails because `ModulePage` has no `Bank` property or
two-argument constructor.

- [ ] **Step 3: Implement the compatible page value**

Use an explicit record struct so the existing constructor still means bank
zero:

```csharp
public readonly record struct ModulePage
{
    public ModulePage(byte value) : this(0, value) { }

    public ModulePage(byte bank, byte value)
    {
        Bank = bank;
        Value = value;
    }

    public byte Bank { get; }
    public byte Value { get; }

    public override string ToString() => $"Bank 0x{Bank:X2}, Page 0x{Value:X2}";
}
```

Update `MsaMemoryAccessor.SelectPageAsync` to write bank register `0x7E`
followed by page register `0x7F` inside the existing atomic session callback.

- [ ] **Step 4: Update old operation assertions**

Update existing tests from:

```text
W 50:7F 11
```

to:

```text
W 50:7E 00
W 50:7F 11
```

Keep cancellation and page-selection-error assertions.

- [ ] **Step 5: Add bank-aware register access**

Add overloads without breaking existing callers:

```csharp
Task<byte[]> ReadBlockAsync(
    byte bank,
    byte page,
    byte startAddress,
    int length);

Task WriteBlockAsync(
    byte bank,
    byte page,
    byte startAddress,
    byte[] data);
```

In `RegisterAccess`, map them to:

```csharp
_msaMemory.ReadAsync(
    _deviceAddress,
    new ModulePage(bank, page),
    new RegisterOffset(startAddress),
    length);
```

and the corresponding `WriteAsync`. Existing page-only methods delegate to
the new overloads with bank zero. Add `BankedRegisterAccessTests` that require
the typed `RegisterAccess` to forward Bank `0x02` and Page `0x11`.

- [ ] **Step 6: Run core tests**

Run:

```powershell
dotnet test .\tests\OpenCMIS.Module.Core.Tests\OpenCMIS.Module.Core.Tests.csproj -c Release
dotnet test .\tests\OpenCMIS.App.Core.Tests\OpenCMIS.App.Core.Tests.csproj -c Release
```

Expected: all module-core and app-core tests pass.

- [ ] **Step 7: Commit**

```powershell
git add src/OpenCMIS.Module.Core src/OpenCMIS.Protocol.Abstractions/Interfaces/IRegisterAccess.cs src/OpenCMIS.Protocol.Core/Implementations/RegisterAccess.cs tests/OpenCMIS.Module.Core.Tests/MsaMemoryAccessorTests.cs tests/OpenCMIS.App.Core.Tests/BankedRegisterAccessTests.cs
git commit -m "feat: support bank-aware MSA page access"
```

---

### Task 2: Establish DevExpress 25.2.4 and Source Generation

**Files:**
- Modify: `src/OpenCMIS.UI.WPF/OpenCMIS.UI.WPF.csproj`
- Modify: `src/OpenCMIS.UI.WPF/App.xaml`
- Modify: `src/OpenCMIS.UI.WPF/App.xaml.cs`
- Create: `src/OpenCMIS.UI.WPF/Resources/Colors.xaml`
- Create: `src/OpenCMIS.UI.WPF/Resources/CompactStyles.xaml`
- Create: `tests/OpenCMIS.UI.WPF.Tests/OpenCMIS.UI.WPF.Tests.csproj`
- Create: `tests/OpenCMIS.UI.WPF.Tests/DevExpressGenerationTests.cs`
- Modify: `OpenCMIS.sln`

**Interfaces:**
- Produces: compile-time DevExpress properties and commands through
  `[GenerateViewModel]`, `[GenerateProperty]`, and `[GenerateCommand]`.
- Consumes: existing generic-host startup.

- [ ] **Step 1: Create the WPF test project**

Create a `net10.0-windows` xUnit project with:

```xml
<PropertyGroup>
  <TargetFramework>net10.0-windows</TargetFramework>
  <EnableWindowsTargeting>true</EnableWindowsTargeting>
  <IsPackable>false</IsPackable>
</PropertyGroup>
<ItemGroup>
  <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.14.1" />
  <PackageReference Include="xunit" Version="2.9.3" />
  <PackageReference Include="xunit.runner.visualstudio" Version="3.1.4">
    <PrivateAssets>all</PrivateAssets>
  </PackageReference>
  <ProjectReference Include="..\..\src\OpenCMIS.UI.WPF\OpenCMIS.UI.WPF.csproj" />
</ItemGroup>
```

Add it to `OpenCMIS.sln`.

- [ ] **Step 2: Add a failing source-generation test**

Convert `MainViewModel` first and assert generated members exist:

```csharp
[Fact]
public void Main_view_model_exposes_generated_navigation_command()
{
    var property = typeof(MainViewModel).GetProperty("SelectedViewName");
    var command = typeof(MainViewModel).GetProperty("NavigateToCommand");

    Assert.NotNull(property);
    Assert.NotNull(command);
}
```

- [ ] **Step 3: Add exact DevExpress packages**

Add to `OpenCMIS.UI.WPF.csproj`:

```xml
<IncludePackageReferencesDuringMarkupCompilation>true</IncludePackageReferencesDuringMarkupCompilation>
```

and package references:

```xml
<PackageReference Include="DevExpress.Mvvm" Version="25.2.4" />
<PackageReference Include="DevExpress.Mvvm.CodeGenerators" Version="22.1.1"
                  PrivateAssets="all" />
<PackageReference Include="DevExpress.Wpf.Core" Version="25.2.4" />
<PackageReference Include="DevExpress.Wpf.Controls" Version="25.2.4" />
<PackageReference Include="DevExpress.Wpf.Grid" Version="25.2.4" />
<PackageReference Include="DevExpress.Wpf.Accordion" Version="25.2.4" />
<PackageReference Include="DevExpress.Wpf.Themes.Win11Light" Version="25.2.4" />
```

Keep MaterialDesign and CommunityToolkit references temporarily so intermediate
commits build; Task 7 removes them.

- [ ] **Step 4: Configure theme startup**

Set:

```csharp
static App()
{
    ApplicationThemeHelper.ApplicationThemeName = Theme.Win11LightName;
    ApplicationThemeHelper.Preload(PreloadCategories.Core);
}
```

Merge `Colors.xaml` and `CompactStyles.xaml` from `App.xaml`. Define compact
spacing tokens of 4, 6, and 8 device-independent pixels and do not copy Pulse
business resources or fonts.

- [ ] **Step 5: Convert MainViewModel generation only**

Use:

```csharp
[GenerateViewModel]
public partial class MainViewModel
{
    [GenerateProperty] object? activeView;
    [GenerateProperty] string selectedViewName = "DeviceConnection";

    [GenerateCommand]
    void NavigateTo(string viewName)
    {
        SelectedViewName = viewName;
        ActiveView = viewName switch
        {
            "Dashboard" => DashboardVM,
            "ControlPanel" => ControlPanelVM,
            "CdbEditor" => CdbEditorVM,
            "ApplicationSwitch" => ApplicationSwitchVM,
            "PageEditor" => PageEditorVM,
            "ModuleHome" => ModuleHomeVM,
            _ => DeviceConnectionVM
        };
    }
}
```

Keep existing constructor behavior until DeviceSession arrives.

- [ ] **Step 6: Restore, test, and build**

Run:

```powershell
dotnet restore .\OpenCMIS.sln
dotnet test .\tests\OpenCMIS.UI.WPF.Tests\OpenCMIS.UI.WPF.Tests.csproj -c Release
dotnet build .\OpenCMIS.sln -c Release --no-restore
```

Expected: restore succeeds, source-generation test passes, solution builds.

- [ ] **Step 7: Commit**

```powershell
git add OpenCMIS.sln src/OpenCMIS.UI.WPF tests/OpenCMIS.UI.WPF.Tests
git commit -m "build: establish DevExpress WPF foundation"
```

---

### Task 3: Preserve Typed Devices with DeviceSession

**Files:**
- Create: `src/OpenCMIS.UI.WPF/Services/DeviceSession.cs`
- Create: `src/OpenCMIS.UI.WPF/Models/AdapterChoice.cs`
- Replace: `src/OpenCMIS.UI.WPF/ViewModels/DeviceConnectionViewModel.cs`
- Modify: `src/OpenCMIS.UI.WPF/App.xaml.cs`
- Create: `tests/OpenCMIS.UI.WPF.Tests/DeviceSessionTests.cs`
- Create: `tests/OpenCMIS.UI.WPF.Tests/DeviceConnectionViewModelTests.cs`
- Create: `tests/OpenCMIS.UI.WPF.Tests/Fakes/FakeDeviceManager.cs`
- Create: `tests/OpenCMIS.UI.WPF.Tests/Fakes/FakeCmisDevice.cs`
- Create: `tests/OpenCMIS.UI.WPF.Tests/Fakes/FakeRegisterAccess.cs`

**Interfaces:**
- Produces: `DeviceSession.CurrentDeviceInfo`, `DeviceSession.CurrentDevice`,
  `DeviceSession.State`, `DeviceSession.Changed`,
  `SetConnected(DeviceInfo, ICmisDevice)`, and `SetDisconnected()`.
- Produces: `DeviceConnectionViewModel.AvailableAdapters`,
  `AvailableDevices`, `SelectedAdapter`, and `SelectedDevice`.
- Consumes: `IDeviceManager.EnumerateDevicesAsync()`,
  `OpenDeviceAsync(DeviceInfo)`, and `CloseDeviceAsync(ICmisDevice)`.

- [ ] **Step 1: Write failing session and selection tests**

Cover these exact behaviors:

```csharp
[Fact]
public async Task Connect_passes_original_device_info_to_manager()
{
    var profile = new CypressI2cConnectionProfile(
        "cypress", "CY123", 0, 400, new I2cDeviceAddress(0x50));
    var info = new DeviceInfo { Id = "CY123:0", Name = "FX3", Profile = profile };
    var manager = new FakeDeviceManager(info);
    var session = new DeviceSession();
    var vm = new DeviceConnectionViewModel(manager, session);

    await vm.RefreshAsync();
    vm.SelectedAdapter = vm.AvailableAdapters.Single();
    vm.SelectedDevice = vm.AvailableDevices.Single();
    await vm.ConnectAsync();

    Assert.Same(info, manager.OpenedDeviceInfo);
    Assert.Same(profile, manager.OpenedDeviceInfo!.Profile);
}
```

Also test provider filtering, one-provider discovery failure, disconnect, and
connection failure preserving the selected device.

- [ ] **Step 2: Run focused tests and verify failure**

Run:

```powershell
dotnet test .\tests\OpenCMIS.UI.WPF.Tests\OpenCMIS.UI.WPF.Tests.csproj -c Release --filter "FullyQualifiedName~Device"
```

Expected: compilation fails because the new session and typed selectors do not
exist.

- [ ] **Step 3: Implement DeviceSession**

Use one enum:

```csharp
public enum DeviceSessionState
{
    Disconnected,
    Connecting,
    Connected,
    Disconnecting
}
```

The session raises `Changed` after every completed transition. It does not
discover devices or construct profiles.

- [ ] **Step 4: Replace DeviceConnectionViewModel**

Use DevExpress source generation and retain `DeviceInfo` objects:

```csharp
[GenerateViewModel]
public partial class DeviceConnectionViewModel
{
    [GenerateProperty] AdapterChoice? selectedAdapter;
    [GenerateProperty] DeviceInfo? selectedDevice;
    [GenerateProperty] bool isRefreshing;
    [GenerateProperty] string statusMessage = "Ready";

    public ObservableCollection<AdapterChoice> AvailableAdapters { get; } = [];
    public ObservableCollection<DeviceInfo> AvailableDevices { get; } = [];

    [GenerateCommand]
    async Task RefreshAsync()
    {
        discoveredDevices =
            (await deviceManager.EnumerateDevicesAsync()).ToArray();
        ReplaceAdapters(discoveredDevices);
        ApplyAdapterFilter();
    }

    [GenerateCommand]
    async Task ConnectAsync()
    {
        if (SelectedDevice is null)
            return;
        var device = await deviceManager.OpenDeviceAsync(SelectedDevice);
        session.SetConnected(SelectedDevice, device);
    }
}
```

`OnSelectedAdapterChanged` filters the cached discovery result by
`device.Profile.AdapterId`.

- [ ] **Step 5: Register one session**

Register `DeviceSession` as a singleton and the connection ViewModel as a
singleton. Other ViewModels can consume the same session in later tasks.

- [ ] **Step 6: Run tests and solution build**

```powershell
dotnet test .\tests\OpenCMIS.UI.WPF.Tests\OpenCMIS.UI.WPF.Tests.csproj -c Release --filter "FullyQualifiedName~Device"
dotnet build .\OpenCMIS.sln -c Release
```

Expected: device tests pass and solution builds.

- [ ] **Step 7: Commit**

```powershell
git add src/OpenCMIS.UI.WPF tests/OpenCMIS.UI.WPF.Tests
git commit -m "refactor: preserve typed devices in WPF session"
```

---

### Task 4: Build the MSA Edit and Difference Model

**Files:**
- Create: `src/OpenCMIS.UI.WPF/Models/MsaByteChange.cs`
- Create: `src/OpenCMIS.UI.WPF/Models/MsaWriteSegment.cs`
- Create: `src/OpenCMIS.UI.WPF/Models/MsaPageBuffer.cs`
- Create: `tests/OpenCMIS.UI.WPF.Tests/MsaPageBufferTests.cs`

**Interfaces:**
- Produces: `MsaPageBuffer.Load(ReadOnlySpan<byte>)`,
  `SetByte(int, byte)`, `GetByte(int)`, `Changes`,
  `BuildWriteSegments(bool fullPage)`, and
  `ApplyVerifiedReadBack(ReadOnlySpan<byte>)`.
- Produces: `MsaByteChange(int Address, byte Original, byte Edited)`.
- Produces: `MsaWriteSegment(byte StartAddress, byte[] Data)`.

- [ ] **Step 1: Write failing model tests**

Add tests for:

```csharp
[Fact]
public void Returning_byte_to_original_removes_change()
{
    var buffer = new MsaPageBuffer();
    buffer.Load(Enumerable.Range(0, 256).Select(i => (byte)i).ToArray());

    buffer.SetByte(0x82, 0xFF);
    buffer.SetByte(0x82, 0x82);

    Assert.Empty(buffer.Changes);
}

[Fact]
public void Dirty_bytes_are_grouped_into_contiguous_segments()
{
    var buffer = new MsaPageBuffer();
    buffer.Load(new byte[256]);
    buffer.SetByte(0x80, 0x11);
    buffer.SetByte(0x81, 0x22);
    buffer.SetByte(0x84, 0x44);

    Assert.Equal(
        new[]
        {
            new MsaWriteSegment(0x80, [0x11, 0x22]),
            new MsaWriteSegment(0x84, [0x44])
        },
        buffer.BuildWriteSegments(fullPage: false));
}
```

Also cover invalid buffer length, invalid address, full-page segment, matching
read-back, and mismatched read-back without clearing changes.

- [ ] **Step 2: Run tests and verify failure**

```powershell
dotnet test .\tests\OpenCMIS.UI.WPF.Tests\OpenCMIS.UI.WPF.Tests.csproj -c Release --filter "FullyQualifiedName~MsaPageBuffer"
```

Expected: compilation fails because the model types do not exist.

- [ ] **Step 3: Implement immutable changes and segments**

Use records and defensively copy segment data:

```csharp
public sealed record MsaByteChange(int Address, byte Original, byte Edited);

public sealed record MsaWriteSegment
{
    public MsaWriteSegment(byte startAddress, IEnumerable<byte> data)
    {
        StartAddress = startAddress;
        Data = data.ToArray();
    }

    public byte StartAddress { get; }
    public byte[] Data { get; }
}
```

- [ ] **Step 4: Implement MsaPageBuffer**

Maintain separate 256-byte original and edited arrays. Derive changes by
address, and clear changes only after verified read-back. Do not reference
DevExpress from the model.

- [ ] **Step 5: Run model tests**

```powershell
dotnet test .\tests\OpenCMIS.UI.WPF.Tests\OpenCMIS.UI.WPF.Tests.csproj -c Release --filter "FullyQualifiedName~MsaPageBuffer"
```

Expected: all buffer tests pass.

- [ ] **Step 6: Commit**

```powershell
git add src/OpenCMIS.UI.WPF/Models tests/OpenCMIS.UI.WPF.Tests/MsaPageBufferTests.cs
git commit -m "feat: add MSA page edit buffer"
```

---

### Task 5: Implement Read, Write, and Automatic Verification

**Files:**
- Create: `src/OpenCMIS.UI.WPF/ViewModels/MsaPageViewModel.cs`
- Create: `src/OpenCMIS.UI.WPF/ViewModels/MsaHexRowViewModel.cs`
- Create: `src/OpenCMIS.UI.WPF/ViewModels/MsaWriteConfirmationViewModel.cs`
- Create: `src/OpenCMIS.UI.WPF/Services/IMsaWriteConfirmation.cs`
- Create: `src/OpenCMIS.UI.WPF/Services/DevExpressMsaWriteConfirmation.cs`
- Delete: `src/OpenCMIS.UI.WPF/ViewModels/PageEditorViewModel.cs`
- Modify: `src/OpenCMIS.UI.WPF/App.xaml.cs`
- Create: `tests/OpenCMIS.UI.WPF.Tests/MsaPageViewModelTests.cs`
- Create: `tests/OpenCMIS.UI.WPF.Tests/Fakes/AcceptWriteConfirmation.cs`

**Interfaces:**
- Produces: `MsaPageViewModel.Bank`, `Page`, `Rows`, `Changes`,
  `ReadAsync()`, `RefreshAsync()`, `WriteChangesAsync()`, and
  `WriteFullPageAsync()`.
- Consumes: `DeviceSession.CurrentDevice.RegisterAccess`,
  `IMsaWriteConfirmation.ConfirmAsync(IReadOnlyList<MsaByteChange>)`.

- [ ] **Step 1: Write failing workflow tests**

Use `FakeRegisterAccess` to assert:

```csharp
[Fact]
public async Task Write_changes_reads_back_and_clears_verified_changes()
{
    var registers = new FakeRegisterAccess();
    registers.QueueRead(new byte[128], new byte[128]);
    registers.QueueRead(new byte[128], BuildUpperBlock((0x82, 0xAA)));
    var device = new FakeCmisDevice(registers);
    var session = DeviceSession.Connected(device);
    var vm = new MsaPageViewModel(
        session,
        new AcceptWriteConfirmation());

    vm.Page = 0x11;
    await vm.ReadAsync();
    vm.SetByte(0x82, 0xAA);
    await vm.WriteChangesAsync();

    Assert.Contains(
        registers.Writes,
        write => write.Page == 0x11 &&
                 write.Address == 0x82 &&
                 write.Data.SequenceEqual(new byte[] { 0xAA }));
    Assert.Empty(vm.Changes);
    Assert.Equal(MsaVerificationStatus.Verified, vm.VerificationStatus);
}
```

Add tests for mismatch preserving changes, write exception, rejected
confirmation, full-page write, invalid Bank/Page input, disconnected session,
and refresh confirmation when edits exist.

- [ ] **Step 2: Run workflow tests and verify failure**

```powershell
dotnet test .\tests\OpenCMIS.UI.WPF.Tests\OpenCMIS.UI.WPF.Tests.csproj -c Release --filter "FullyQualifiedName~MsaPageViewModel"
```

Expected: compilation fails because the ViewModel does not exist.

- [ ] **Step 3: Implement row projection**

Each `MsaHexRowViewModel` exposes:

```csharp
public int BaseAddress { get; }
public string Offset => $"0x{BaseAddress:X2}";
public IReadOnlyList<MsaHexCellViewModel> Cells { get; }
public string Ascii { get; }
```

Cell changes update `MsaPageBuffer` and refresh the row ASCII text.

- [ ] **Step 4: Implement ViewModel source generation**

Use `[GenerateViewModel(ImplementISupportServices = true)]` and generated async
commands. Disable read/write commands while another page operation executes.
Parse Bank and Page as hexadecimal byte values before transport access.

- [ ] **Step 5: Implement page operations**

Read lower memory from page zero and upper memory from the selected page. For
each dirty segment:

```csharp
await registerAccess.WriteBlockAsync(
    Bank,
    Page,
    segment.StartAddress,
    segment.Data);
```

Immediately read the same page again. Only call
`ApplyVerifiedReadBack(readBack)` when all intended bytes match.

Use the bank-aware Task 1 overloads for both reads and writes. Existing
callers that use the page-only overloads continue to select bank zero.

- [ ] **Step 6: Implement confirmation adapter**

`IMsaWriteConfirmation` is stateless. Its DevExpress implementation opens the
custom difference ViewModel through `IDialogService` and returns true only for
an explicit Write result.

- [ ] **Step 7: Run WPF tests and build**

```powershell
dotnet test .\tests\OpenCMIS.UI.WPF.Tests\OpenCMIS.UI.WPF.Tests.csproj -c Release
dotnet build .\OpenCMIS.sln -c Release
```

Expected: all WPF tests pass and solution builds.

- [ ] **Step 8: Commit**

```powershell
git add src/OpenCMIS.UI.WPF tests/OpenCMIS.UI.WPF.Tests
git commit -m "feat: add verified MSA page editing workflow"
```

---

### Task 6: Build the Compact DevExpress Shell and MSA View

**Files:**
- Replace: `src/OpenCMIS.UI.WPF/Views/MainWindow.xaml`
- Replace: `src/OpenCMIS.UI.WPF/Views/MainWindow.xaml.cs`
- Replace: `src/OpenCMIS.UI.WPF/Views/DeviceConnectionView.xaml`
- Replace: `src/OpenCMIS.UI.WPF/Views/PageEditorView.xaml`
- Rename: `src/OpenCMIS.UI.WPF/Views/PageEditorView.xaml` to `src/OpenCMIS.UI.WPF/Views/MsaPageView.xaml`
- Rename: `src/OpenCMIS.UI.WPF/Views/PageEditorView.xaml.cs` to `src/OpenCMIS.UI.WPF/Views/MsaPageView.xaml.cs`
- Create: `src/OpenCMIS.UI.WPF/Views/MsaWriteConfirmationView.xaml`
- Create: `src/OpenCMIS.UI.WPF/Views/MsaWriteConfirmationView.xaml.cs`
- Modify: `src/OpenCMIS.UI.WPF/ViewModels/MainViewModel.cs`
- Modify: `src/OpenCMIS.UI.WPF/App.xaml.cs`

**Interfaces:**
- Consumes: Task 3 typed device selectors and Task 5 MSA commands.
- Produces: approved layout A shell and approved layout B MSA page.

- [ ] **Step 1: Replace Window with ThemedWindow**

Use:

```xml
<dx:ThemedWindow
    x:Class="OpenCMIS.UI.WPF.Views.MainWindow"
    xmlns:dx="http://schemas.devexpress.com/winfx/2008/xaml/core"
    xmlns:dxacc="http://schemas.devexpress.com/winfx/2008/xaml/accordion"
    xmlns:dxedit="http://schemas.devexpress.com/winfx/2008/xaml/editors"
    ShowIcon="False"
    WindowStartupLocation="CenterScreen"
    MinWidth="1100"
    MinHeight="700">
```

Keep code-behind limited to `InitializeComponent`; use commands and
`EventToCommand` for navigation.

- [ ] **Step 2: Build the top connection bar**

Bind DevExpress selectors to:

```text
DeviceConnectionVM.AvailableAdapters
DeviceConnectionVM.SelectedAdapter
DeviceConnectionVM.AvailableDevices
DeviceConnectionVM.SelectedDevice
```

Show Refresh, Connect/Disconnect, I2C address, current module name, and session
state in one compact row.

- [ ] **Step 3: Build collapsible navigation**

Use `AccordionControl` with the six approved functional areas. Bind selection
to `NavigateToCommand`; do not retain the old `SelectionChanged` code-behind.

- [ ] **Step 4: Build the raw-byte-first GridControl**

Create columns:

```text
Offset | 00 | 01 | 02 | 03 | 04 | 05 | 06 | 07 |
08 | 09 | 0A | 0B | 0C | 0D | 0E | 0F | ASCII
```

Use conditional formatting for modified, mismatch, failed, read-only, and
selected cells. Bind Bank/Page editors and the Read, Refresh, Write Changes,
Write Full Page, Compare, and Export commands.

- [ ] **Step 5: Add the difference dialog**

Use a read-only `GridControl` with Address, Old Value, and New Value columns.
Return Write only when the user selects the explicit confirmation command.

- [ ] **Step 6: Compile XAML**

```powershell
dotnet build .\src\OpenCMIS.UI.WPF\OpenCMIS.UI.WPF.csproj -c Release
```

Expected: zero XAML compilation errors.

- [ ] **Step 7: Commit**

```powershell
git add src/OpenCMIS.UI.WPF
git commit -m "feat: build compact DevExpress WPF shell"
```

---

### Task 7: Migrate Remaining Views and Remove Old MVVM Libraries

**Files:**
- Replace: `src/OpenCMIS.UI.WPF/ViewModels/ApplicationSwitchViewModel.cs`
- Replace: `src/OpenCMIS.UI.WPF/ViewModels/CdbEditorViewModel.cs`
- Replace: `src/OpenCMIS.UI.WPF/ViewModels/ControlPanelViewModel.cs`
- Replace: `src/OpenCMIS.UI.WPF/ViewModels/DashboardViewModel.cs`
- Replace: `src/OpenCMIS.UI.WPF/ViewModels/ModuleHomeViewModel.cs`
- Replace: remaining XAML under `src/OpenCMIS.UI.WPF/Views`
- Modify: `src/OpenCMIS.UI.WPF/OpenCMIS.UI.WPF.csproj`
- Delete: obsolete MaterialDesign-only converters or styles that have no callers.

**Interfaces:**
- Consumes: `DeviceSession` instead of per-page `SetDevice()`.
- Produces: a WPF project with only DevExpress MVVM and no MaterialDesign or
  CommunityToolkit references.

- [ ] **Step 1: Add generated-member smoke tests**

For every ViewModel, reflect its required generated property and command:

```csharp
[Theory]
[InlineData(typeof(ControlPanelViewModel), "ReadRegisterCommand")]
[InlineData(typeof(CdbEditorViewModel), "ReadCdbCommand")]
[InlineData(typeof(ModuleHomeViewModel), "RefreshCommand")]
public void Migrated_view_model_exposes_command(Type type, string property)
{
    Assert.NotNull(type.GetProperty(property));
}
```

- [ ] **Step 2: Convert each ViewModel**

Replace CommunityToolkit attributes with DevExpress code-generator attributes.
Inject `DeviceSession` and read `CurrentDevice` at command execution time.
Remove every `SetDevice(ICmisDevice?)` method.

- [ ] **Step 3: Replace remaining controls**

Use DevExpress editors and GridControls for data-entry and tabular screens.
Replace MaterialDesign resource keys with `Colors.xaml`,
`CompactStyles.xaml`, or DevExpress theme resources.

- [ ] **Step 4: Remove legacy dependencies**

Remove:

```xml
<PackageReference Include="MaterialDesignThemes" Version="5.2.0" />
<PackageReference Include="CommunityToolkit.Mvvm" Version="8.4.0" />
```

Verify no source reference remains:

```powershell
rg -n "MaterialDesign|CommunityToolkit|ObservableObject|RelayCommand" src/OpenCMIS.UI.WPF
```

Expected: no matches.

- [ ] **Step 5: Run WPF tests and build**

```powershell
dotnet test .\tests\OpenCMIS.UI.WPF.Tests\OpenCMIS.UI.WPF.Tests.csproj -c Release
dotnet build .\OpenCMIS.sln -c Release
```

Expected: tests pass and solution builds.

- [ ] **Step 6: Commit**

```powershell
git add src/OpenCMIS.UI.WPF tests/OpenCMIS.UI.WPF.Tests
git commit -m "refactor: complete DevExpress MVVM migration"
```

---

### Task 8: Final Verification and Documentation

**Files:**
- Modify: `README.md`
- Create: `docs/verification/2026-07-31-devexpress-wpf-simulated-verification.md`
- Modify: `docs/superpowers/plans/2026-07-31-devexpress-wpf-msa-editor-refactor.md`

**Interfaces:**
- Produces: reproducible verification evidence and explicit hardware-test
  limitations.

- [ ] **Step 1: Run the full test suite**

```powershell
dotnet test .\OpenCMIS.sln -c Release --no-restore
```

Record total passed, failed, and skipped tests.

- [ ] **Step 2: Run a clean Release build**

```powershell
dotnet clean .\OpenCMIS.sln -c Release
dotnet build .\OpenCMIS.sln -c Release
```

Record warnings and errors separately.

- [ ] **Step 3: Run static checks**

```powershell
rg -n "MaterialDesign|CommunityToolkit|ObservableObject|RelayCommand" src/OpenCMIS.UI.WPF
git diff --check
git status --short
```

Expected: no legacy-framework matches and no whitespace errors.

- [ ] **Step 4: Run UI smoke check**

Start the application without hardware:

```powershell
dotnet run --project .\src\OpenCMIS.UI.WPF\OpenCMIS.UI.WPF.csproj -c Release
```

Verify Win11Light theme, main shell startup, empty device state, navigation,
and simulated-device MSA grid. Do not report physical discovery or I2C
transfer as tested.

- [ ] **Step 5: Document evidence**

Update README with DevExpress prerequisites and add a verification note that
separates:

```text
Simulated unit tests
Release build
UI smoke check
Physical hardware verification: not performed
```

- [ ] **Step 6: Commit**

```powershell
git add README.md docs/verification docs/superpowers/plans/2026-07-31-devexpress-wpf-msa-editor-refactor.md
git commit -m "docs: record DevExpress WPF verification"
```
