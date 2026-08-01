# OpenCMIS DevExpress WPF and MSA Editor Design

Date: 2026-07-31

Status: Approved for implementation planning

Target framework: .NET 10 (`net10.0-windows`)

## Goal

Refactor `OpenCMIS.UI.WPF` into a compact DevExpress WPF application that:

- selects an I2C adapter and a discovered physical device without losing the
  device's typed connection profile;
- uses DevExpress controls and DevExpress MVVM throughout the WPF project;
- provides a raw-byte-first MSA page editor with direct read and write support;
- automatically reads back and verifies data after a write;
- keeps protocol, module, application, and transport projects independent of
  DevExpress;
- remains testable with simulated hardware until physical devices are
  available.

This work changes the WPF presentation and its application-facing state. It
does not redesign the optical-module protocol or add another domain project.

## Decisions

### DevExpress boundary

Only `OpenCMIS.UI.WPF` references DevExpress. The following projects remain
UI-framework independent:

- `OpenCMIS.Protocol.*`
- `OpenCMIS.Module.Core`
- `OpenCMIS.Transport.*`
- `OpenCMIS.App.Core`

`OpenCMIS.UI.WPF` removes `MaterialDesignThemes` and
`CommunityToolkit.Mvvm`. ViewModels use DevExpress MVVM compile-time
generation, commands, and services.

All DevExpress runtime WPF packages use version `25.2.4`.
`DevExpress.Mvvm.CodeGenerators` uses `22.1.1` because that is the latest
published official DevExpress analyzer package; there is no `25.2.4` release
of this analyzer. It is a build-time DevExpress dependency, not a second MVVM
framework.

ViewModels use these generator attributes where appropriate:

- `GenerateViewModel`
- `GenerateProperty`
- `GenerateCommand`

Generated ViewModels that need view services enable
`ImplementISupportServices`. Dialogs and UI interactions use DevExpress MVVM
services instead of direct `MessageBox` calls or view-specific references.

### Minimal application state

The existing `DeviceManager` remains the device catalog and factory
coordinator. It already discovers all registered `II2cAdapterProvider`
implementations and returns `DeviceInfo` objects that contain typed
`I2cConnectionProfile` instances.

The refactor adds only one shared UI state object, `DeviceSession`. It owns:

- the current `DeviceInfo`;
- the current open `ICmisDevice`;
- connecting, connected, and disconnecting state;
- the last connection error;
- a state-changed notification for interested ViewModels.

No separate device-catalog service is added. Device discovery continues
through `IDeviceManager`. No separate MSA editor service is added. MSA edit
state belongs to the MSA page ViewModel.

`MainViewModel` owns navigation and global status only. It no longer calls
`SetDevice()` on every child ViewModel. Pages observe `DeviceSession` and
react when its active device changes.

## Main Window

The main shell uses DevExpress `ThemedWindow` with the `Win11Light` theme,
following the proven Pulse startup pattern:

- set `ApplicationThemeHelper.ApplicationThemeName`;
- preload the DevExpress core theme;
- resolve ViewModels and the main window through the existing generic host.

The selected layout is a compact top connection bar with a collapsible
left-side navigation area.

### Top connection bar

The connection bar contains:

1. adapter-type selector;
2. physical-device selector filtered by adapter;
3. I2C device address;
4. refresh button;
5. connect or disconnect button;
6. current module identity and connection state.

Device discovery binds to complete `DeviceInfo` objects. The UI must not
flatten results to `PortName` or reconstruct a serial-specific `DeviceInfo`
when connecting.

The adapter selector is derived from discovered devices' typed profiles. It
supports the existing serial, HM multi-channel, and Cypress profiles and can
display future providers without a shell redesign.

Advanced connection fields are collapsed by default. Expanding them displays
profile-specific values such as baud rate, channel, Cypress port, and I2C
speed. Editing these values produces an updated typed profile; it does not
fall back to the legacy string dictionary.

### Navigation

The left navigation uses short labels and icons, supports collapse, and keeps
the existing functional areas:

- connection;
- module overview;
- MSA page editor;
- register control;
- CDB editor;
- application switching.

The dedicated connection page may retain detailed diagnostics, but normal
device selection and connection remain available globally in the top bar.

## MSA Page Editor

The selected design is raw-byte-first.

### Command area

The compact command area contains:

- Bank;
- Page;
- Read;
- Refresh;
- Write Changes;
- a write menu containing Write Full Page;
- Compare;
- Export.

Bank and Page values are validated before transport calls. Commands are
asynchronous, prevent unintended concurrent execution, and expose busy and
error state.

### Hex grid

The primary surface is a DevExpress `GridControl`. Each row represents 16
bytes and contains:

- row base address;
- hexadecimal columns `00` through `0F`;
- ASCII representation.

Cells visually distinguish:

- read-only bytes;
- writable bytes;
- modified bytes;
- write failures;
- read-back mismatches;
- the current selection.

Selecting a byte updates the detail panel with its absolute address, raw
value, access permission, known MSA field, and decoded interpretation.
Field parsing remains available as a secondary tab and can select the
corresponding byte range in the hex grid.

### Edit model

`MsaPageViewModel` owns:

- the last successfully read page snapshot;
- the editable page buffer;
- the set of modified byte addresses;
- the current byte selection;
- the latest write and verification result.

Changing a cell compares it with the original snapshot. Returning a byte to
its original value removes it from the modified set.

Write Changes sends only modified writable bytes. Write Full Page is available
as an explicit advanced operation. Both operations display a DevExpress
confirmation dialog containing address, old value, and new value differences
before transport access begins.

### Write and read-back flow

The default write workflow is:

1. validate the active device, Bank, Page, values, and access permissions;
2. build an immutable difference snapshot;
3. show the difference confirmation dialog;
4. write the selected differences or the full page;
5. automatically read the same Bank and Page again;
6. compare the read-back buffer with the intended values;
7. mark the operation as verified or identify mismatched addresses.

A successful read-back replaces the page snapshot and clears verified dirty
bytes. A mismatch or read-back exception does not discard the user's edit
snapshot. The user can retry, refresh, or export the differences.

Manual Refresh always performs a new read of the current Bank and Page. If
unsaved edits exist, it asks for confirmation before replacing the edit
buffer.

## Device and Page Data Flow

### Device discovery and connection

1. The connection ViewModel calls `IDeviceManager.EnumerateDevicesAsync()`.
2. Discovery results remain complete `DeviceInfo` instances.
3. The adapter selector filters those instances by `Profile.AdapterId`.
4. The physical-device selector chooses one `DeviceInfo`.
5. Connect passes the selected object to
   `IDeviceManager.OpenDeviceAsync()`.
6. `DeviceSession` publishes the resulting `ICmisDevice`.
7. Page ViewModels enable their commands based on session state.

Failure of one adapter provider does not hide devices discovered by other
providers. Provider-specific failures are displayed separately.

### Page operations

The MSA ViewModel calls the existing optical-module device abstraction. It
does not access Cypress, serial ports, or P/Invoke APIs directly. All
hardware-specific behavior stays behind the transport provider and bus
interfaces.

## Error Handling

- Discovery reports provider-specific failures while retaining successful
  results.
- Connection failure leaves the selected device visible and allows retry.
- Disconnect clears the active device and disables page operations.
- Invalid Bank, Page, address, or byte input is rejected before I2C access.
- Write failure preserves the original and edited buffers and marks the
  addresses that were not verified.
- Read-back mismatch shows expected and actual values per address.
- Cancellation stops pending UI work without presenting it as a hardware
  failure.
- Exceptions are logged with transport and device context but without
  credentials or sensitive configuration.

## Testing and Verification

Physical hardware is not currently available. Verification therefore has
three explicitly separated levels.

### Simulated unit tests

Tests use fake adapter providers and fake optical-module devices to cover:

- adapter filtering and physical-device selection;
- preservation of the original typed `DeviceInfo.Profile`;
- connection-session state transitions;
- page-to-hex-row mapping;
- byte editing and dirty-address tracking;
- returning a byte to its original value;
- difference snapshot generation;
- modified-byte writes;
- full-page writes;
- automatic read-back success;
- automatic read-back mismatch;
- write and read-back exceptions;
- manual refresh with unsaved edits;
- asynchronous command re-entry protection.

### Build and static verification

- restore DevExpress dependencies;
- build the complete solution for Release;
- confirm generated ViewModel properties and commands compile;
- confirm all XAML dictionaries and views load at compile time;
- run the full automated test suite;
- run `git diff --check`.

### UI smoke verification

Launch the WPF application without hardware and verify:

- the Win11Light theme loads;
- the main shell resolves through dependency injection;
- the connection bar displays empty discovery state;
- navigation opens each page;
- simulated devices can populate the connection bar and MSA grid.

Hardware discovery, physical I2C reads, physical writes, and read-back timing
remain explicitly unverified until hardware testing is available.

## Scope Exclusions

This refactor does not:

- add another domain or DDD project;
- change the CMIS protocol model;
- expose DevExpress types outside `OpenCMIS.UI.WPF`;
- redesign Cypress native bindings;
- claim physical hardware compatibility from simulated tests;
- add multi-device simultaneous connections;
- add background polling or automatic periodic page writes.
