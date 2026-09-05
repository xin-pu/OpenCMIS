# Task 2+3 report: integrate safe CMIS VDM diagnostics

## Result

`CmisDevice` and `ICmisDevice` now expose a read-only, descriptor-driven VDM
snapshot through the Task 1 `VdmReader`.  The legacy configuration, monitor,
FEC, and flag readers/models were removed from the worktree.  The CLI now
prints generic observable rows (instance, raw descriptor, raw sample, and all
four threshold flags), supports `vdm read` and `vdm monitor`, rejects writes to
descriptor pages 20h-23h, and releases its Ctrl+C handler and cancellation
resources when monitoring ends.

## TDD evidence

### RED

Added `Device_reads_descriptor_driven_diagnostics_from_its_register_access` to
`tests/OpenCMIS.App.Core.Tests/VdmReaderTests.cs`.  It instantiates a connected
`CmisDevice` with simulated registers containing Page 01h byte 142 bit 6,
descriptor Page 20h data, sample Page 24h data, and Page 2Ch flags.

Command run before replacing the old consumer:

```powershell
dotnet test tests\OpenCMIS.App.Core.Tests\OpenCMIS.App.Core.Tests.csproj --filter FullyQualifiedName~Device_reads_descriptor_driven_diagnostics_from_its_register_access
```

It failed at compile time as expected for the transitional tree: the old
`VdmConfigReader`, `VdmMonitorReader`, and `VdmStatisticsReader` still consumed
the fixed constants removed in Task 1 (for example `VdmConfigPage`,
`RegVdmControl`, and `VdmMaxLanes`).

### GREEN

After routing `CmisDevice.ReadVdmDiagnosticsAsync()` to `VdmReader` and
removing the unsafe readers, ran:

```powershell
dotnet test tests\OpenCMIS.App.Core.Tests\OpenCMIS.App.Core.Tests.csproj --filter FullyQualifiedName~VdmReaderTests
```

Result: 3 passed, 0 failed.  This includes the new API-level integration test
and checks the independent high-alarm, high-warning, low-warning, and
low-alarm bits from a `0xF` flag nibble.

Additional scoped verification:

```powershell
dotnet test tests\OpenCMIS.App.Core.Tests\OpenCMIS.App.Core.Tests.csproj
dotnet build src\OpenCMIS.UI.CLI\OpenCMIS.UI.CLI.csproj --no-restore
```

Results: App.Core tests 17 passed, 0 failed; CLI build succeeded with 0
warnings and 0 errors.

## Full-suite evidence

Ran once:

```powershell
dotnet test OpenCMIS.sln
```

The suite is not yet buildable because the separately scoped WPF VDM lifecycle
and UI migration still references the removed unsafe types.  The WPF source
generator and `VdmDiagnosticsViewModel.cs` report missing `VdmConfig` and
`VdmFecStats`.  All test projects that completed before the WPF compilation
barrier passed: Transport.Abstractions 14, App.Core 17, Module.Core 18,
Transport.I2C.Serial 25, Transport.Simulated 29, and Transport.I2C.Cypress 13.

## Files changed

- `src/OpenCMIS.App.Core/CmisDevice.cs`
- `src/OpenCMIS.Protocol.Abstractions/Interfaces/ICmisDevice.cs`
- `src/OpenCMIS.Protocol.Abstractions/Models/VdmDiagnostics.cs`
- `src/OpenCMIS.UI.CLI/Program.cs`
- `tests/OpenCMIS.App.Core.Tests/VdmReaderTests.cs`
- `tests/OpenCMIS.App.Core.Tests/DeviceManagerTests.cs`
- `tests/OpenCMIS.UI.WPF.Tests/Fakes/FakeCmisDevice.cs`
- `tests/OpenCMIS.UI.WPF.Tests/PageEditorViewModelTests.cs`

Unsafe reader/model files removed from the worktree:

- `VdmConfigReader`, `VdmMonitorReader`, `VdmStatisticsReader`, and
  `VdmFlagDecoder`
- `VdmConfig`, `VdmFecStats`, `VdmFlags`, `VdmLaneFlags`, `VdmLaneMonitor`,
  `VdmModuleMonitor`, and `VdmStatus`

## Concerns / follow-up

- The full solution remains blocked until Task 4 replaces the existing WPF VDM
  view-model/configuration UI with generic observable rows and lifecycle-safe
  monitoring.
- The removed unsafe files were pre-existing untracked worktree files, so their
  deletion does not appear as a Git deletion in this task commit; they are
  nevertheless absent from the resulting worktree.
