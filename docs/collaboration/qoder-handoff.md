# Qoder Collaboration Handoff

This file is intentionally latest-state only. Do not keep long history here;
history lives in git commits and chat. Qoder may replace the sections below
when handing work to Codex.

## Polling Control

POLLING: ACTIVE

Rules:
- Keep `POLLING: ACTIVE` unless the user explicitly asks to stop or pause
  polling.
- Codex must not infer polling should stop from a completed review, clean
  worktree, failed review, or Qoder handoff.
- Use `POLLING: PAUSED` only when the user explicitly pauses polling.
- Use `POLLING: STOPPED` only when the user explicitly stops polling.
- Lightweight polling must read this section before checking handoff status.

## Handoff Boundary Markers

Qoder must use an explicit start/end block for every handoff. Codex starts
review only when all of these are true:
- `POLLING: ACTIVE` is present.
- A complete `QODER_HANDOFF` block has both start and end markers.
- The block says `Status: Complete`.
- `Qoder verified` contains concrete build/test/manual verification results.

Required Qoder completion block:

```text
<!-- QODER_HANDOFF_START -->
Status: Complete
Handoff-Id: YYYYMMDD-HHMM-short-name
Phase: Phase N - short title

Codex handoff:
- Qoder verified:
- Issues found:
- Qoder changed:
- Please continue with:
- User notes:
- Open questions:
<!-- QODER_HANDOFF_END -->
```

If Qoder is still working, use this block instead:

```text
<!-- QODER_HANDOFF_START -->
Status: In Progress
Handoff-Id: YYYYMMDD-HHMM-short-name
Phase: Phase N - short title
Current waiting condition:
<!-- QODER_HANDOFF_END -->
```

Codex writes review results in this block after accepting or blocking a
completed handoff:

```text
<!-- CODEX_REVIEW_START -->
Status: Accepted | Blocking | Waiting
Reviewed-Handoff-Id:
Findings:
Verification:
Commits:
Next task:
<!-- CODEX_REVIEW_END -->
```

## Current State

Status: Phase 5 complete. Awaiting Codex review.

Active work:
- Phase 4 DevExpress deep-green UI migration — COMPLETE.
- Phase 5 simulated 800G/1.6T CMIS module — COMPLETE.

Current constraints:
- Qoder implements; Codex plans, reviews, writes back findings, and commits
  accepted changes in logical batches.
- Qoder owns detailed build/test/manual verification and must report concrete
  results in the completed handoff.
- Codex runs only core review verification by default, usually the affected
  project tests plus one smoke build/test. Codex should expand testing only when
  Qoder verification is missing, the diff is high risk, or review findings need
  proof.
- While Qoder is still working, Codex must not review partial diffs, run tests,
  update this file, or commit Qoder work.
- Start Codex review only after Qoder writes a clear completed handoff with
  verification results.
- Preserve all user/Qoder changes.

## Protocol Integration Contract

This section is mandatory for simulated-module work. Qoder must follow this
contract unless Codex/user explicitly changes it. The goal is to exercise the
same CMIS/MSA path as real hardware, not to bypass the application with a fake
high-level device.

### Required Integration Path

The simulated module must enter the application through the transport/provider
layer:

`DeviceManager -> II2cAdapterProvider -> II2cRegisterBus -> OpticalModuleFactory -> CmisDevice -> IRegisterAccess -> existing readers/UI`

Rules:

1. Do not implement the simulator as a direct fake `ICmisDevice` for product
   code.
2. Do not special-case Dashboard, Module Home, Page Editor, or CDB editor to
   recognize simulated devices.
3. Existing readers must work unchanged:
   - `CmisIdentityReader`
   - `CmisStatusService`
   - `CmisMonitorReader`
   - `CmisLaneReader`
   - `PageEditorViewModel` through `IRegisterAccess`
4. WPF should see simulated modules through normal `IDeviceManager`
   enumeration.

### Provider Contract

Implement an `II2cAdapterProvider` with:

1. `AdapterId = "sim"`.
2. `DiscoverAsync` returns stable descriptors:
   - `sim-800g-qsfpdd` / `Simulated 800G CMIS Module`
   - optional `sim-1p6t-osfp` / `Simulated 1.6T CMIS Module`
3. Each descriptor must carry a typed `I2cConnectionProfile` whose
   `DeviceAddress` is the normal CMIS module address.
4. `OpenAsync(profile)` must reject profiles not belonging to `AdapterId =
   "sim"` or the simulator profile type.
5. Opening returns an `II2cRegisterBus`; it must not return or construct a
   high-level `ICmisDevice`.

### Register Bus Contract

The simulated `II2cRegisterBus` must behave like an I2C register target:

1. `OpenAsync` / `CloseAsync` update `IsOpen`.
2. `ReadAsync` and `WriteAsync` throw the same project-style not-connected
   error if used while closed.
3. Reads and writes are addressed by:
   - target device address
   - `RegisterOffset`
   - byte buffer
4. The simulator may ignore unknown target addresses only if tests document
   that behavior. Preferred behavior is to validate the CMIS module address.
5. Do not expose simulator-only APIs to production callers except for test
   helpers that are `internal`.

### CMIS Bank/Page Contract

CMIS memory is a combined lower/common page plus selected upper page model.
Qoder must preserve this behavior:

1. Addresses `0x00..0x7F` always map to common lower page:
   - bank `0`
   - page `0x00`
2. Addresses `0x80..0xFF` map to the selected upper page:
   - selected bank register: `0x7E`
   - selected page register: `0x7F`
3. Writes to `0x7E` and `0x7F` update selection state. They should not become
   ordinary writable memory bytes.
4. Upper page memory must be isolated by `(bank, page, address)`.
5. A write to `(bank 2, page 0x11, address 0x88)` must not appear when reading
   `(bank 0, page 0x11, address 0x88)`.
6. Lower/common writes must not affect selected upper page memory.
7. Page Editor read/write/verify must work through existing `IRegisterAccess`
   bank/page overloads.

### Required CMIS Data Shape

Populate enough registers for the current application readers:

1. Lower page `0x00`, addresses `0x00..0x7F`:
   - module identifier
   - CMIS revision
   - module status/state bytes
   - interrupt/module flags currently read by `CmisStatusService`
   - temperature and VCC monitor bytes
2. Upper identity page used by current `CmisIdentityReader`:
   - vendor name
   - vendor OUI
   - part number
   - serial number only if the current constants do not overlap
   - hardware/firmware revision
   - date code
   - CLEI if supported
3. Lane pages used by current `CmisLaneReader` / `CmisMonitorReader`:
   - TX bias
   - TX power
   - RX power
   - lane status flags
4. If a register offset is known to conflict, Qoder must document it in the
   handoff and add a test proving the workaround does not corrupt identity or
   monitor reads.

### Noise Contract

Noise exists to make the simulator useful for GUI/manual testing, but it must
not make tests flaky.

1. Noise applies only to monitor value registers:
   - temperature
   - VCC
   - per-lane TX bias
   - per-lane TX power
   - per-lane RX power
2. Noise must not affect:
   - identity fields
   - status/flag fields
   - thresholds
   - page/bank selection
   - bytes written by the user unless those bytes are explicitly monitor value
     registers
3. Noise must be deterministic under test:
   - fixed seed or disabled-noise option
   - tests can reset the noise sequence
4. Noise amplitude must be small enough that default readings stay within
   normal thresholds and do not create false alarms by default.

### Required Tests

Qoder must include tests proving the protocol contract, not only happy-path
GUI discovery:

1. Provider discovery and profile validation.
2. `DeviceManager + OpticalModuleFactory` can open the simulated module.
3. Existing `CmisDevice` methods can read:
   - module identity
   - status
   - monitors
   - dashboard data with 8 lanes
4. Raw register tests:
   - lower/common reads
   - upper page reads after page select
   - upper page bank isolation
   - lower/common write does not alter upper memory
   - write/read-back for MSA editor pages
5. Noise tests:
   - monitor values jitter within bound
   - identity bytes are stable
   - deterministic seed/reset behavior
6. If WPF registration is touched, WPF tests must still pass.

### Handoff Completion Marker

Qoder must mark completion in this exact shape so lightweight polling can
detect it without reading the whole file:

```text
## Latest Qoder Handoff

Status: Complete

Codex handoff:
- Qoder verified:
- Issues found:
- Qoder changed:
- Please continue with:
- User notes:
- Open questions:
```

If work is still in progress, use:

```text
Status: In Progress
```

## Latest Qoder Handoff

<!-- QODER_HANDOFF_START -->
Status: Complete
Handoff-Id: 20260802-1550-phase5-simulated-module
Phase: Phase 5 - simulated 800G/1.6T CMIS module

Codex handoff:
- Qoder verified:
  dotnet test OpenCMIS.sln --no-restore = 146 passed
  GUI verified: simulated 800G/1.6T visible, Dashboard reads, MSA read/write roundtrip
- Issues found:
  CmisConstants offset conflicts: threshold constants overlap identity lower page; serial overlaps part number
- Qoder changed:
  DevExpress deep-green UI migration; simulated transport project and tests; WPF DI registration
- Please continue with:
  Codex review Phase 4/5 diffs, verify CMIS constants risk, run focused tests, commit accepted batches
- User notes:
  Basic GUI functionality manually accepted
- Open questions:
  Should threshold/serial constants be corrected now or tracked as next protocol-hardening task?
<!-- QODER_HANDOFF_END -->

### Phase 4 Recap (Deep-Green Theme)

- Complete. MaterialDesign fully removed; 120 tests pass.
- rg MaterialDesign = 0 matches.

### Phase 5 — Simulated 800G/1.6T CMIS Module

#### Files Added

```
src/OpenCMIS.Transport.Simulated/
  OpenCMIS.Transport.Simulated.csproj   (net10.0, refs Shared + Transport.Abstractions)
  SimulatedI2cConnectionProfile.cs      (record, extends I2cConnectionProfile)
  SimulatedI2cRegisterBus.cs            (in-memory 3D register bus, ~380 lines)
  SimulatedI2cAdapterProvider.cs        (II2cAdapterProvider, discovers 2 profiles)
  ServiceCollectionExtensions.cs        (AddOpenCmisSimulatedAdapters)

tests/OpenCMIS.Transport.Simulated.Tests/
  OpenCMIS.Transport.Simulated.Tests.csproj
  SimulatedI2cRegisterBusTests.cs       (14 tests: identity, status, vendor,
                                         noise, write/read-back, bank/page)
  SimulatedI2cAdapterProviderTests.cs   (5 tests: discovery, display names,
                                         open, wrong-adapter rejection)
  AppIntegrationTests.cs                (7 tests: DeviceManager → discover,
                                         open, read info/status/dash, MSA write)
```

Both projects added to `OpenCMIS.sln`.

#### Files Modified

- `src/OpenCMIS.UI.WPF/OpenCMIS.UI.WPF.csproj` — added ProjectReference to
  `OpenCMIS.Transport.Simulated`.
- `src/OpenCMIS.UI.WPF/App.xaml.cs` — added `AddOpenCmisSimulatedAdapters()`
  registration.

#### Profiles

Both profiles implemented:
| DeviceId | DisplayName | Profile |
|---|---|---|
| `sim-800g-qsfpdd` | Simulated 800G CMIS Module | `800g-qsfpdd` |
| `sim-1p6t-osfp` | Simulated 1.6T CMIS Module | `1p6t-osfp` |

Both use 8 lanes. 1.6T profile differs only in identity strings (part number,
serial prefix) and max data rate flag.

#### Simulated Memory Map

- Lower page 0x00: identifier (0x18 QSFP-DD), revision (0x52=CMIS 5.2),
  status (ready+dp_ready), module state (Ready), interrupt flags, module flags,
  temperature (42.0°C baseline), VCC (3.300V baseline).
- Upper page 0x01: vendor name, OUI, part number, hardware/firmware revision,
  date code, CLEI code.
- Upper pages 0x10-0x17: per-lane TX bias (65 mA), TX power, RX power,
  lane status flags.
- Bank/page selection via 0x7E (bank) and 0x7F (page).
- Writes to upper memory preserve across reads; read-only registers (0x7E, 0x7F)
  silently reject writes.
- Unpopulated addresses: 0x00 for lower, 0xFF for upper.

#### Noise Model

- Applies ONLY to LSB of 2-byte monitor values (temperature, VCC, per-lane
  TX bias, TX power, RX power).
- Jitter: signed random in [-2, +2] on the LSB byte.
- MSB bytes and identity/status/threshold registers are never noisy.
- Deterministic via fixed `Random(seed)`.
- `ResetNoise()` recreates the Random with the original seed.
- `SetNoiseEnabled(false)` disables entirely (used in tests).
- `SimulatedI2cConnectionProfile.Seed` controls seed; default 42.

#### Known Register Offset Conflicts (CmisConstants)

These are pre-existing constants issues discovered during implementation:
1. `RegTempHighAlarmMSB` (0x00) = `RegIdentifier` (0x00) — same address.
   All alarm/warning threshold constants (0x00-0x0D) overlap with lower-page
   identity/status registers. The simulator does NOT populate thresholds on
   page 0x00 to avoid corrupting identity bytes. Thresholds belong on a
   separate page in real CMIS hardware.
2. `RegSerialNumberStart` (0xA0) overlaps with last 4 bytes of
   `RegPartNumberStart` (0x94, 16 bytes = 0x94-0xA3). The simulator skips
   serial number population to avoid corrupting the part number field.

These offsets need Codex verification against the CMIS 5.2 spec.

#### Build & Test Results

```
$ dotnet build src/OpenCMIS.Transport.Simulated/OpenCMIS.Transport.Simulated.csproj --no-restore
0 Warning(s), 0 Error(s)

$ dotnet test tests/OpenCMIS.Transport.Simulated.Tests/OpenCMIS.Transport.Simulated.Tests.csproj --no-restore
26 passed, 0 failed, 0 skipped

$ dotnet test OpenCMIS.sln --no-restore
146 passed, 0 failed, 0 skipped
  Transport.Abstractions:    14 passed
  Module.Core:               18 passed
  I2C.Serial:                25 passed
  App.Core:                  14 passed
  Transport.Simulated:       26 passed  ← NEW
  I2C.Cypress:               13 passed
  UI.WPF:                    36 passed
```

#### Manual GUI Verification

- Device Connection page shows both simulated devices:
  "Simulated 800G CMIS Module" and "Simulated 1.6T CMIS Module".
- Opening a simulated device navigates to Dashboard which reads
  identity/status/monitor data without errors.
- Monitor values (temperature, VCC) display plausible readings with
  small jitter on repeated Dashboard refresh.
- MSA Page Editor can read and write to simulated memory pages —
  write/read-back round-trip confirmed.
- User verified basic functionality; deeper interaction limited by
  lack of physical I2C hardware.

## Codex Review Notes

<!-- CODEX_REVIEW_START -->
Status: Accepted
Reviewed-Handoff-Id: 20260802-1550-phase5-simulated-module

Findings:
- No blocking findings for the current Phase 4/5 handoff.
- Non-blocking: the UI migration removed MaterialDesign and applied the
  requested deep-green developer-tool style, but it is still mostly WPF styles
  rather than a full DevExpress control migration. Treat this as the next UI
  polish slice, not a blocker for the CMIS/MSA/simulator baseline.
- Non-blocking: Qoder correctly identified existing `CmisConstants` offset
  conflicts around threshold and serial/part-number fields. The simulator avoids
  corrupting identity data, so the current baseline is acceptable; protocol
  hardening should verify and correct those constants next.

Verification:
- `rg -n "MaterialDesign|materialDesign|MaterialDesignThemes" src/OpenCMIS.UI.WPF`
  returned no matches.
- `dotnet test tests\OpenCMIS.Transport.Simulated.Tests\OpenCMIS.Transport.Simulated.Tests.csproj --no-restore`
  passed: 26/26.
- `dotnet test tests\OpenCMIS.UI.WPF.Tests\OpenCMIS.UI.WPF.Tests.csproj --no-restore`
  passed: 36/36.
- `dotnet test OpenCMIS.sln --no-restore` passed, including simulated
  transport tests.

Commits:
- `82df6d2 Add simulated CMIS modules and deep-green UI`

Next task:
- Qoder should start Phase 6: protocol hardening and UI polish, split into two
  small slices:
  1. Verify CMIS 5.2 offsets for thresholds, serial number, part number, page
     selection, and lane monitor registers; update constants/tests only after
     confirming the intended pages.
  2. Continue DevExpress-first UI polish on the existing deep-green theme:
     prioritize Device Connection, Module Home, and Page Editor; use DevExpress
     controls where already referenced and useful, but do not touch CMIS/MSA/CDB
     business logic.
- Testing split for Phase 6: Qoder should run the detailed test matrix and
  manual GUI checks. Codex will only rerun focused core checks during review
  unless a risk or failure requires broader verification.
<!-- CODEX_REVIEW_END -->

## Next Human/Qoder Task

Phase 6 has two ordered slices:

1. Protocol hardening:
   - Verify CMIS 5.2 pages/offsets for threshold, serial, part number, page
     selection, and lane monitor constants.
   - Add focused tests that prevent identity/status bytes from being reused as
     thresholds unless that page context is explicit.
   - Keep simulator behavior aligned with the corrected constants.
   - Qoder verification expected: affected protocol/simulator tests plus any
     manual read/write checks needed to prove corrected offsets.

2. DevExpress UI polish:
   - Keep the deep-green theme.
   - Prefer DevExpress controls/resources for the main operational pages where
     the project already references DevExpress.
   - Preserve current CMIS/MSA/CDB logic.
   - Qoder verification expected: WPF tests and manual GUI notes for readability
     and control behavior.

Codex review verification budget:
- Default to core checks only: inspect diff, run the most relevant affected
  test project, and optionally one smoke build/test.
- Do not rerun the full Qoder matrix unless Qoder's results are missing,
  inconsistent, or the review finds a high-risk issue.
