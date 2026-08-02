# Qoder Collaboration Handoff

## Latest Qoder Handoff

Codex handoff:
- Qoder verified:
  - `dotnet build OpenCMIS.sln --no-restore`: 0 errors, 0 warnings
  - `dotnet test OpenCMIS.sln --no-restore`: 106 tests passed, 0 failed, 0 skipped
- Issues found:
  - VCC Low Warning threshold register does not exist in CMIS 5.2 lower page 0x00; Qoder used default `[0,0]` for `VccLowWarn`.
  - Lane bits 0x04-0x20 for TX/RX LOS/LOL need real-hardware verification.
- Qoder changed:
  - Added decoded CMIS interrupt/module flag models.
  - Expanded `ModuleStatus`, `LaneStatus`, `MonitorValue`, and `ModuleDashData`.
  - Expanded CMIS status, monitor, lane readers.
  - Updated App.Core tests.
- Please continue with:
  - Codex review of threshold map, lane flag bits, and alarm/warning comparison logic.
- User notes:
  - Current scope is generic CMIS, MSA, and CDB only. Do not add new HCI or vendor-specific work in this phase.
- Open questions:
  - Should VCC Low Warning be read from another register or modeled as unavailable?
  - Do we need per-lane alarm/warning thresholds in this phase?

## Codex Review Notes

Status: Cleanup reviewed by Codex. No blocking findings for this slice.

Findings (resolved):

1. ~~`CmisMonitorReader` uses `{0,0}` as VCC low warning threshold.~~
   → Added `LowWarnAvailable` bool to `MonitorValue`. VCC passes `false`;
     `BuildMonitorValue` skips `value <= warnLow` when unavailable and stores
     `RawWarnLowBytes = []`.
2. ~~`CmisStatusService` still maps both `IsReady` and `DataPathFirmwareFault`
   from `statusByte bit0`.~~
   → Removed incorrect `DataPathFirmwareFault = (statusByte & 0x01) != 0`;
     now hardcoded `false` with comment citing unresolved CMIS bit definition.
     `IsReady` mapping preserved (pre-existing behavior).
3. ~~Monitor parsing still uses little-endian composition for fields named MSB.~~
   → Fixed all four parse methods to big-endian: `(bytes[0] << 8) | bytes[1]`.
     Extracted `ParseUInt16BigEndian` / `ParseInt16BigEndian` helpers.
     Updated 10 test byte arrays in `CmisDeviceCompatibilityTests` and
     `CmisReaderTests` to match BE order. Added 4 explicit byte-order tests.

Qoder changes this round:
- `MonitorValue.cs`: +`LowWarnAvailable` property (default `true`)
- `CmisMonitorReader.cs`: +`ParseUInt16BigEndian`, +`ParseInt16BigEndian`;
  fixed byte order in `ParseTemperature`/`ParseVcc`/`ParseCurrent`/`ParsePower`;
  `BuildMonitorValue` accepts `lowWarnAvailable` param, guards `RawWarnLowBytes`;
  VCC section passes `lowWarnAvailable: false` instead of fake `[0,0]`.
- `CmisStatusService.cs`: removed incorrect `DataPathFirmwareFault` bit mapping
- Visibility: `ParseTemperature`, `ParseVcc` changed `private` → `internal` for testing
- Tests: 4 new byte-order facts; 10 byte arrays swapped BE; 14 App.Core tests pass

Verification:
- `dotnet build OpenCMIS.sln --no-restore`: 0 errors, 0 warnings
- `dotnet test OpenCMIS.sln --no-restore`: 102 tests passed, 0 failed, 0 skipped
  (App.Core: 14 (+4), Module.Core: 18, Serial: 25, Cypress: 13, WPF: 18,
   Transport.Abstractions: 14)

Codex review:
- Checked the cleanup diff for `CmisMonitorReader`, `CmisStatusService`,
  `MonitorValue`, and App.Core tests.
- Verified unavailable VCC low warning no longer indexes an empty threshold
  byte array.
- Verified `DataPathFirmwareFault` is no longer mapped to the same bit as
  `IsReady`; it is held false with raw status retained for future decoding.
- Verified monitor byte-order tests now cover temperature, VCC, TX bias,
  TX power, and RX power.
- Ran `dotnet test tests\OpenCMIS.App.Core.Tests\OpenCMIS.App.Core.Tests.csproj --no-restore`:
  passed, 14 tests, 0 failed.

## Next Human/Qoder Task

Phase 2 MSA editor slice complete. Codex should review:
- `PageEditorViewModel` bank-aware read/write/verify flow
- Dirty-segment write logic using `MsaWriteSegment`
- Read-back verification via `MsaPageBuffer.ApplyVerifiedReadBack`
- XAML Bank input binding

## Phase 2 Handoff

Qoder implemented MSA editor Phase 2:

1. ✅ Bank/page inputs added to `PageEditorViewModel` (`BankNumber`,
   `StartAddress`, `ReadLength`) and `PageEditorView` XAML (Bank TextBox).
2. ✅ `MsaPageBuffer` wired into `PageEditorViewModel` — replaced raw
   `_pageData byte[]` with `MsaPageBuffer? _pageBuffer`; all read/write/fill
   operations use `GetByte()`/`SetByte()`.
3. ✅ `ReadPageAsync` reads lower 128 from page 0, upper 128 from selected
   bank/page via `IRegisterAccess.ReadBlockAsync(bank, page, ...)`.
   `ReadCommonPageAsync` uses `(0, 0, 0x00, 256)`.
4. ✅ `WritePageAsync` uses `BuildWriteSegments(fullPage: false)` for
   dirty-only contiguous writes; each `MsaWriteSegment` written via
   `WriteBlockAsync(bank, page, startAddr, data)`.
5. ✅ After write: reads back upper 128 + lower 128, calls
   `ApplyVerifiedReadBack(fullReadBack)`; on match → clears dirty state
   and refreshes grid; on mismatch → preserves edits, reports error.
6. ✅ `dotnet test` WPF: 18 passed; full solution: 102 passed, 0 failed.

Files changed:
- `PageEditorViewModel.cs`: +BankNumber/StartAddress/ReadLength/IsLoaded;
  `MsaPageBuffer` replaces `_pageData`; bank-aware read/write/verify;
  `TryParseHexByte` helper; Fill methods update buffer.
- `PageEditorView.xaml`: Bank TextBox added; "Write Page" → "Write + Verify"

Open questions:
- `IRegisterAccess` bank overload has default `NotSupportedException` for
  non-zero bank — real MSA accessor must override. Hardware verification needed.
- Range-based partial read (StartAddress/Length) UI fields exist but not yet
  wired to a range-read command — reserved for future slice.

## Codex Phase 2 Review

Status: Blocking issue found.

Findings:

1. Blocking: `PageEditorViewModel.WritePageAsync` builds dirty segments across
   the entire 256-byte buffer, then writes every segment to the selected
   `bank/page`. If the user edits lower/common bytes `0x00..0x7F`, those
   bytes are written to the selected upper page/bank instead of common page 0.
   Lower/common bytes must either be read-only in this generic page-write flow,
   or written separately to `(bank 0, page 0, address 0x00..0x7F)`.
2. `StartAddress` and `ReadLength` exist in `PageEditorViewModel` but are not
   bound in XAML or used by a range-read command. This is acceptable as future
   work only if it is not presented as implemented range support.

Verification:

- `dotnet test tests\OpenCMIS.UI.WPF.Tests\OpenCMIS.UI.WPF.Tests.csproj --no-restore`
- Result: passed, 18 tests, 0 failed.

## Next Human/Qoder Task

Phase 2 blocking issue fixed. Codex should re-review.

## Phase 2 Fix Handoff

Qoder resolved the Codex Phase 2 review blocking issue:

1. ✅ **Split dirty segments at 0x80 boundary.** Added
   `SplitAtPageBoundary(MsaWriteSegment)` helper. `WritePageAsync` now routes:
   - Segments in `0x00-0x7F` → `WriteBlockAsync(0, 0, startAddr, data)` (common page)
   - Segments in `0x80-0xFF` → `WriteBlockAsync(bank, page, startAddr, data)` (selected page)
   - Boundary-crossing segments are split at `0x80` automatically.
2. ✅ **Conditional read-back verification.** Only reads back lower page
   from `(0, 0)` when lower bytes were written; same for upper page. Avoids
   unnecessary I/O.
3. ✅ **Added 7 tests in `PageEditorViewModelTests.cs`:**
   - `SplitAtPageBoundary` tests: entirely lower, entirely upper,
     crossing boundary, ending at 0x80, starting at 0x80
   - `Write_routes_lower_bytes_to_common_page_zero`: assert edit at `0x05` →
     `WriteBlockAsync(0, 0, 0x05, ...)`
   - `Write_routes_upper_bytes_to_selected_bank_page`: assert edit at `0xA0` →
     `WriteBlockAsync(2, 0x11, 0xA0, ...)`
4. ✅ Added `<InternalsVisibleTo Include="OpenCMIS.UI.WPF.Tests" />` to WPF csproj.

Files changed:
- `PageEditorViewModel.cs`: +`SplitAtPageBoundary`; `WritePageAsync` splits
  segments by address range with conditional verify
- `OpenCMIS.UI.WPF.csproj`: +InternalsVisibleTo for test project
- `PageEditorViewModelTests.cs`: new, 7 tests (5 split + 2 routing)

Verification:
- `dotnet test OpenCMIS.sln --no-restore`: 109 tests passed, 0 failed
  (WPF: 25 (+7), App.Core: 14, Module.Core: 18, Serial: 25, Cypress: 13,
   Transport.Abstractions: 14)

## Codex Phase 2 Fix Review

Status: Accepted. No blocking findings for this fix.

Findings:

1. The previous blocking issue is resolved. `WritePageAsync` now splits dirty
   segments at the `0x80` boundary and routes lower/common bytes to
   `(bank 0, page 0)` while routing upper bytes to the selected bank/page.
2. Read-back verification is still preserved. The implementation only reads
   back the half-page ranges that were actually written and reuses the current
   buffer bytes for the untouched half-page before calling
   `ApplyVerifiedReadBack`.
3. Added tests cover boundary splitting and the two required routing cases:
   lower edit `<0x80` writes to common page 0; upper edit `>=0x80` writes to
   selected bank/page.

Verification:

- `dotnet test tests\OpenCMIS.UI.WPF.Tests\OpenCMIS.UI.WPF.Tests.csproj --no-restore`
- Result: passed, 25 tests, 0 failed.

## Next Human/Qoder Task

Qoder completed the range-read slice. Codex should review.

## Phase 3 Range-Read Handoff

Qoder implemented the Codex-recommended MSA editor range-read slice:

1. ✅ **`ReadRangeCommand` added.** Reads a partial page from the selected
   bank/page, constrained to `StartAddress` + `ReadLength`. Same lower/upper
   routing as writes: `0x00–0x7F` from common page (0,0), `0x80–0xFF` from
   selected bank/page. Boundary-crossing ranges are split automatically.
2. ✅ **`ValidateHexInputs` helper.** Shared validation for all four hex fields
   (Bank, Page, StartAddress, ReadLength). Validation runs before device check
   so users get meaningful errors even without a connected device.
3. ✅ **XAML updated.** Added Start/Len TextBoxes, Read Range button.
   Layout changed from 4-column + right-fill to 8-column + second-row fill.
   "Read Common Page" button shortened to "Common".
4. ✅ **9 new tests** (WPF: 34 total):
   - 6 input-validation tests: invalid bank/page/start/length hex, zero
     length, range exceeds page boundary
   - 3 routing tests: lower-only → (0,0), upper-only → selected bank/page,
     crossing-boundary → both reads
5. ✅ Existing full-page read/write+verify behavior unchanged.

Files changed:
- `PageEditorViewModel.cs`: +`ReadRangeAsync`, +`ValidateHexInputs`
- `PageEditorView.xaml`: Start/Len inputs, Read Range button, 8-col layout
- `PageEditorViewModelTests.cs`: +9 tests (6 validation + 3 routing),
  +`SpyRegisterAccess.Reads` tracking

Verification:
- `dotnet test OpenCMIS.sln --no-restore`: 118 tests passed, 0 failed
  (WPF: 34 (+9), App.Core: 14, Module.Core: 18, Serial: 25, Cypress: 13,
   Transport.Abstractions: 14)

## Codex Phase 3 Range-Read Review

Status: Blocking issue found. Do not commit this range-read slice yet.

Findings:

1. Blocking: `ReadRangeAsync` builds a new 256-byte buffer initialized with
   zeroes, then copies only the requested range into it and renders the whole
   page. Bytes outside the requested range are displayed as real `00` values
   even though they were not read from hardware. Because the editor still
   allows normal full-grid editing/fill/write after a range read, the user can
   accidentally dirty and write unknown/unread addresses based on synthetic
   zeroes.
2. Validation order is now correct: invalid hex, zero length, and range
   boundary errors are reported before the no-device check.
3. Routing tests cover lower-only, upper-only, and boundary-crossing reads.

Verification:

- `dotnet test tests\OpenCMIS.UI.WPF.Tests\OpenCMIS.UI.WPF.Tests.csproj --no-restore`
- Result: passed, 34 tests, 0 failed.

## Next Human/Qoder Task

Phase 3 blocking issue fixed. Codex should re-review.

## Phase 3 Fix Handoff (Synthetic Zeroes)

Qoder resolved the Codex Phase 3 blocking issue (synthetic zeroes):

1. ✅ **Buffer preservation.** `ReadRangeAsync` now starts from the existing
   `_pageBuffer` bytes (if any) rather than a fresh zero-filled array. Only
   the requested range is overwritten with hardware-read data. Unread bytes
   retain their previously-loaded values.
2. ✅ **Test added.** `ReadRange_preserves_unread_bytes_from_previous_load`:
   loads full page with `0xAA`, range-reads `0x80–0x87` (spy returns zeroes),
   asserts bytes outside `[0x80,0x87]` remain `0xAA` while bytes inside are `0x00`.
3. ✅ Existing validation and routing tests unchanged (35 total WPF tests).

Files changed:
- `PageEditorViewModel.cs`: `ReadRangeAsync` copies existing buffer before
  overlaying range-read data
- `PageEditorViewModelTests.cs`: +1 preservation test, +`GetBufferByte` helper

Verification:
- `dotnet test OpenCMIS.sln --no-restore`: 119 tests passed, 0 failed
  (WPF: 35 (+1), App.Core: 14, Module.Core: 18, Serial: 25, Cypress: 13,
   Transport.Abstractions: 14)

## Codex Phase 3 Fix Review

Status: Blocking issue remains. Do not commit this range-read slice yet.

Findings:

1. Blocking: the synthetic-zeroes issue is fixed only when a previous
   `_pageBuffer` exists. If the user clicks `Read Range` before loading a full
   page, `ReadRangeAsync` still starts from a new zero-filled 256-byte buffer,
   overlays the requested range, then renders the whole page. Unread addresses
   are still shown as writable `00` hardware values on that first range-read
   path.
2. The new preservation test covers the overlay-after-existing-buffer path and
   is useful, but it does not cover first-use range read without a prior full
   page load.
3. Existing validation and lower/upper routing tests still pass.

Verification:

- `dotnet test tests\OpenCMIS.UI.WPF.Tests\OpenCMIS.UI.WPF.Tests.csproj --no-restore`
- Result: passed, 35 tests, 0 failed.

## Next Human/Qoder Task

Phase 3 first-use safety gap closed. Codex should re-review.

## Phase 3 Fix Handoff (First-Use Guard)

Qoder resolved the remaining Codex Phase 3 blocking issue (first-use range
read without prior page load):

1. ✅ **First-use guard.** `ReadRangeAsync` now checks `_pageBuffer == null`
   after device validation. If no page has been loaded, shows:
   "Load a full page first (Read Page / Common)." No hardware reads are
   issued until a full page is loaded.
2. ✅ **Test added.** `ReadRange_without_prior_page_load_shows_error`:
   sets device but skips `LoadPageBuffer`, invokes range read, asserts
   error message and zero hardware reads.
3. ✅ **Routing tests updated.** Three existing routing tests now call
   `LoadPageBuffer(vm, CreateEmptyPage())` to satisfy the new guard.
4. ✅ **Preservation test retained.** Overlay-after-existing-buffer path
   still covered.

Files changed:
- `PageEditorViewModel.cs`: +`_pageBuffer` null guard in `ReadRangeAsync`
- `PageEditorViewModelTests.cs`: +1 first-use test; 3 routing tests +buffer

Verification:
- `dotnet test OpenCMIS.sln --no-restore`: 120 tests passed, 0 failed
  (WPF: 36 (+1), App.Core: 14, Module.Core: 18, Serial: 25, Cypress: 13,
   Transport.Abstractions: 14)

## Codex Phase 3 First-Use Guard Review

Status: Accepted. No blocking findings for this range-read slice.

Findings:

1. The first-use safety gap is closed. `ReadRangeAsync` now requires an
   existing page buffer before issuing hardware reads, so a first-time range
   read can no longer render unread addresses as synthetic writable `00`
   values.
2. The already-loaded path still preserves previous full-page bytes and
   overlays only the requested range.
3. Tests now cover validation, lower/upper/cross-boundary routing,
   preservation of unread bytes after a loaded page, and the no-prior-load
   guard.

Verification:

- `dotnet test tests\OpenCMIS.UI.WPF.Tests\OpenCMIS.UI.WPF.Tests.csproj --no-restore`
- Result: passed, 36 tests, 0 failed.

## Next Human/Qoder Task

Phase 3 range-read is accepted and ready to commit. Next recommended slice:

1. Move to the next generic MSA/CDB capability, keeping scope limited to
   generic CMIS, MSA, and CDB.
2. Do not add HCI timer/polling or vendor-specific behavior in this phase.
