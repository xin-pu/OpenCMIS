# OpenCMIS Optical Module and I2C Architecture Design

Date: 2026-07-31
Status: Approved on 2026-07-31
Target branch: `codex/refactor-module-i2c-architecture`

## 1. Purpose

Refactor OpenCMIS into a modular architecture that separates optical-module communication, CMIS semantics, hardware-independent I2C contracts, serial I2C implementations, and optional Cypress hardware support.

The refactor may change internal APIs and move existing types. The existing WPF and CLI high-level workflows should remain compatible where practical through facades and temporary adapters.

## 2. Goals

- Target .NET 10.
- Keep the OpenCMIS core usable on platforms supported by `net10.0`.
- Add an independent optical-module communication project within the OpenCMIS solution.
- Implement atomic MSA page access and vendor HCI access.
- Replace generic `TypeA` and `TypeB` connector names with hardware/protocol-specific names.
- Support Linktel, HM serial, HM multichannel, FIC2USB, and EUI3 through a common I2C contract.
- Import Cypress as source rather than consuming `Pulse.Instruments.Cypress` through NuGet.
- Keep Cypress support optional and Windows-specific.
- Remove all source and runtime dependencies on Pulse.
- Provide deterministic unit coverage based on fakes and mocks.
- Preserve the current `ICmisDevice` high-level usage model during migration.

## 3. Non-goals

- Making FIC2USB or EUI3 operate on Linux or macOS.
- Providing real-hardware validation in the first implementation.
- Reproducing Pulse instrument lifecycle, UI metadata attributes, driver reflection, memory-map file parsing, or test-bench integration.
- Introducing repositories, persistence aggregates, or domain events.
- Claiming hardware compatibility based only on simulated tests.
- Publishing or redistributing Cypress-proprietary source without the required authorization.

## 4. Architectural Assessment

The current implementation is useful as a prototype but has boundaries that will not scale to additional hardware and protocols:

- `CmisDevice` mixes application orchestration, CMIS semantics, register reads, parsing, conversion, and state transitions.
- `DeviceManager` constructs concrete serial connectors directly.
- `IRegisterTransport` inherits raw connection operations and does not carry an I2C target address or cancellation token.
- MSA page selection and the following register operation are not one atomic transaction.
- `PageManager` caches page state independently of HCI operations that reuse page/status registers.
- `DeviceInfo.ConnectionParameters` is a string dictionary rather than a typed connection profile.
- Module models are distributed across Shared, Transport, Protocol, and App projects.
- Connector names `TypeA` and `TypeB` do not communicate the actual bridge protocol or hardware.

The refactor uses lightweight domain-driven design with ports and adapters. Optical-module communication and CMIS semantics are modeled as domains. Serial and Cypress integrations remain infrastructure.

## 5. Target Project Structure

| Project | Target framework | Responsibility |
| --- | --- | --- |
| `OpenCMIS.Shared` | `net10.0` | Common error infrastructure and genuinely cross-cutting primitives |
| `OpenCMIS.Transport.Abstractions` | `net10.0` | Hardware-independent I2C port, profiles, descriptors, and capabilities |
| `OpenCMIS.Transport.I2C.Serial` | `net10.0` | Linktel, HM serial, and HM multichannel adapters |
| `OpenCMIS.Cypress` | `net10.0-windows` | Imported Cypress/CyUSB source and low-level device APIs |
| `OpenCMIS.Transport.I2C.Cypress` | `net10.0-windows` | FIC2USB and EUI3 adapters over `OpenCMIS.Cypress` |
| `OpenCMIS.Module.Core` | `net10.0` | Optical-module session, MSA access, HCI access, and module communication value objects |
| `OpenCMIS.Protocol.Abstractions` | `net10.0` | Public CMIS-facing contracts and models |
| `OpenCMIS.Protocol.Core` | `net10.0` | CMIS 5.2 identity, monitor, state, application, and register semantics |
| `OpenCMIS.CDB.*` | `net10.0` | CDB contracts and CMIS CDB implementation |
| `OpenCMIS.App.Core` | `net10.0` | Provider selection, lifecycle orchestration, and compatibility facade assembly |
| `OpenCMIS.UI.CLI` | `net10.0` | Cross-platform host; registers serial providers only |
| `OpenCMIS.UI.WPF` | `net10.0-windows` | Windows host; registers serial and Cypress providers |

Dependency direction:

```text
WPF / CLI
    -> App.Core
        -> Protocol.Core
            -> Module.Core
                -> Transport.Abstractions

Transport.I2C.Serial
    -> Transport.Abstractions

Transport.I2C.Cypress
    -> Transport.Abstractions
    -> OpenCMIS.Cypress
```

`App.Core`, `Protocol.Core`, and `Module.Core` must not reference either concrete I2C implementation project.

## 6. Domain Boundaries

### 6.1 Optical-module communication

`OpenCMIS.Module.Core` owns physical module-memory communication:

- `OpticalModuleSession`
- `MsaMemoryAccessor`
- `HciMemoryAccessor`
- `HciCommandCodec`
- `I2cDeviceAddress`
- `RegisterOffset`
- `ModulePage`
- `HciTableId`
- module communication options

`OpticalModuleSession` owns the synchronization boundary for a single module/bus session. MSA and HCI operations must share this boundary.

### 6.2 CMIS protocol

`OpenCMIS.Protocol.Core` owns CMIS meanings built over module memory:

- module identity
- module state
- monitoring values
- lane state
- CMIS applications
- CDB-facing register semantics

It must not know whether the underlying adapter is serial, Cypress, or simulated.

### 6.3 Vendor HCI extension

HCI is a vendor-specific module subdomain, not a CMIS standard feature. It is isolated under an HCI namespace and exposed separately from standard CMIS services.

### 6.4 Hardware infrastructure

Serial and Cypress projects implement hardware ports. They do not interpret CMIS pages, module identity fields, monitor scaling, or HCI tables.

## 7. I2C Port

The replacement for the current `IRegisterTransport` is a target-address-aware and cancellation-aware port:

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
```

Design rules:

- `I2cDeviceAddress` stores a canonical 7-bit address.
- Conversion to an 8-bit write address occurs only in an adapter that requires it.
- Adapters must not ignore or silently replace the supplied device address.
- `Capabilities` advertises maximum transfer sizes.
- Common segmentation composes large operations from supported transfer sizes.
- Exact-length reads must handle partial underlying reads correctly.
- Empty writes and invalid ranges are rejected before hardware access.

## 8. Provider and Connection Model

`DeviceManager` receives providers instead of constructing connectors:

```csharp
public interface II2cAdapterProvider
{
    string AdapterId { get; }

    ValueTask<IReadOnlyList<I2cAdapterDescriptor>> DiscoverAsync(
        CancellationToken cancellationToken = default);

    ValueTask<II2cRegisterBus> OpenAsync(
        I2cConnectionProfile profile,
        CancellationToken cancellationToken = default);
}
```

Initial providers:

- `LinktelSerialAdapterProvider`
- `HmSerialAdapterProvider`
- `HmMultiChannelAdapterProvider`
- `Fic2UsbAdapterProvider`
- `Eui3AdapterProvider`

Typed profiles replace internal use of the current string dictionary:

- `SerialI2cConnectionProfile`
- `HmMultiChannelConnectionProfile`
- `CypressI2cConnectionProfile`

The existing `ConnectionParameters` dictionary remains accepted at the compatibility boundary and is converted immediately into a typed profile.

## 9. Adapter Naming

| Existing/Pulse name | New name |
| --- | --- |
| `II2cAccessor` / `IRegisterTransport` | `II2cRegisterBus` |
| `I2CConnectorTypeA` | `LinktelSerialI2cAdapter` |
| `I2CConnectorTypeB` / `HMI2CConnector` | `HmSerialI2cAdapter` |
| `HMI2CMultiChanConnector` | `HmMultiChannelI2cAdapter` |
| `FsbConnector` | `Fic2UsbI2cAdapter` |
| `Eui3Connector` | `Eui3I2cAdapter` |
| `AdapterModule` | Split into `OpticalModuleSession`, MSA, HCI, and CMIS services |
| `HciProtocolHelper` | `HciCommandCodec` |

Low-level Cypress type names may remain unchanged inside `OpenCMIS.Cypress` to minimize risk. OpenCMIS-specific names are applied at the adapter boundary.

## 10. MSA Data Flow

Each MSA operation is one atomic session operation:

```text
Acquire the module session gate
    -> validate device, page, offset, and length
    -> select the required page through register 0x7F
    -> perform a segmented register read or write
    -> release the gate
```

Correctness takes priority over page caching in the first implementation. If page caching is later introduced, it must live in `OpticalModuleSession` and be invalidated by every HCI operation.

## 11. HCI Data Flow

HCI uses the same module-session gate as MSA:

```text
Acquire the module session gate
    -> encode the HCI command
    -> write 0x7F to register 0x7F
    -> poll register 0x80 until an allowed ready value is observed
    -> write the command to register 0x81
    -> write 0x7E to register 0x80
    -> poll register 0x80 again
    -> read the response from register 0x80
    -> validate response length and protocol structure
    -> extract the requested payload
    -> invalidate any MSA page state
    -> release the gate
```

HCI polling uses `TimeProvider`, a configurable timeout, and configurable ready values. Cancellation stops polling promptly.

## 12. Retry and Error Handling

The existing `CmisException` compatibility model remains. New error codes distinguish failure boundaries:

- `I2cAdapterNotFound`
- `I2cConnectionFailed`
- `I2cTransferFailed`
- `I2cInvalidResponse`
- `MsaPageSelectionFailed`
- `HciCommandTimeout`
- `HciInvalidResponse`
- `HciCommandRejected`

Rules:

- `OperationCanceledException` propagates without wrapping.
- Only transient I/O failures and transfer timeouts are retryable.
- Invalid arguments, invalid response frames, and rejected protocol commands are not retried.
- HCI busy polling is not counted as I2C retry behavior.
- Discovery returns structured probe failures for diagnostics.
- Production operations do not swallow arbitrary exceptions.

## 13. Cypress Platform Boundary

`OpenCMIS.Cypress` and `OpenCMIS.Transport.I2C.Cypress` target `net10.0-windows`.

The rest of the core targets `net10.0` and contains no compile-time dependency on Cypress. Composition occurs at the host:

- WPF registers serial and Cypress providers.
- The cross-platform CLI registers serial providers.
- Non-Windows processes do not load Cypress assemblies.

Existing Cypress source depends on Windows APIs, Windows device notifications, and `CyUSB3.sys`. Changing only its target framework would not make it cross-platform.

The imported Cypress source must retain required notices and may only be committed or redistributed when the applicable license permits it.

## 14. Compatibility Strategy

High-level compatibility is preserved while internal contracts are replaced:

- Keep current `ICmisDevice` identity, status, monitor, lane, and close operations.
- Refactor `CmisDevice` into a facade over Protocol and Module services.
- Retain an `IRegisterAccess` compatibility adapter temporarily.
- Mark `I2CConnectorTypeA` and `I2CConnectorTypeB` obsolete and forward them to named adapters during migration.
- Continue accepting `DeviceInfo.ConnectionParameters` at UI/API boundaries.
- Move transport-only models into `Transport.Abstractions`, raw MSA/HCI addressing
  models into `Module.Core`, and public CMIS identity/monitor/lane models into
  `Protocol.Abstractions`, without requiring an immediate WPF workflow rewrite.

The refactor may break consumers of internal concrete classes. Public high-level behavior remains the compatibility priority.

## 15. Test Strategy

The first implementation has no real-hardware tests. Tests are deterministic simulations.

### 15.1 Protocol and module tests

Use `ScriptedI2cRegisterBus`, a fake that:

- provides scripted read results and failures
- records every read and write
- records call order
- supports controllable completion for concurrency tests

Coverage includes:

- MSA Page 0 and Page 17 operation sequences
- page selection and access atomicity
- MSA and HCI concurrency serialization
- HCI busy-to-ready transitions
- HCI timeout and cancellation
- invalid HCI length, status, and response structure
- MSA page-state invalidation after HCI
- 7-bit and 8-bit I2C address conversion
- transfer segmentation and reassembly

`TimeProvider` removes real waiting from timeout tests.

### 15.2 Serial adapter tests

Pure codec tests validate Linktel and HM request/response frames. Mock serial sessions cover:

- partial reads
- timeouts
- cancellation
- invalid acknowledgements
- invalid checksums or status values
- multiple channels

### 15.3 Cypress adapter tests

Mock `ICypressDeviceApi` to verify:

- discovery mapping
- FIC2USB/EUI3 selection
- port and speed mapping
- segmentation behavior
- transfer success and failure conversion
- disposal

The low-level CyUSB implementation receives build coverage only in this phase.

### 15.4 Verification language

Passing tests confirm the modeled operation sequence, frame encoding, parsing, concurrency, and error behavior. They do not confirm communication with a real module, serial bridge, FIC2USB device, EUI3 device, or installed Cypress driver.

## 16. Migration Sequence

1. Add tests and the new hardware-independent transport contracts.
2. Add address, register, capability, descriptor, and typed-profile value objects.
3. Implement serial codecs and named serial adapters.
4. Add `OpenCMIS.Module.Core`, `OpticalModuleSession`, and atomic MSA access.
5. Add HCI codec and HCI access.
6. Refactor CMIS services and reduce `CmisDevice` to a facade.
7. Refactor `DeviceManager` to provider-based discovery and composition.
8. Confirm that the Cypress license permits the intended repository use, then
   import the authorized source into the Windows-only project.
9. Implement FIC2USB and EUI3 adapters over the Cypress boundary.
10. Register serial providers in CLI and serial/Cypress providers in WPF.
11. Add compatibility adapters and mark old concrete connector types obsolete.
12. Update architecture documentation and run the full simulated test suite.

Each step must leave the solution buildable. Existing WPF and CLI high-level workflows are checked after each compatibility-sensitive step.

## 17. Acceptance Criteria

- The solution targets .NET 10 and builds without warnings.
- `Module.Core`, Protocol, App, and serial transport projects target `net10.0`.
- Cypress projects target `net10.0-windows`.
- No OpenCMIS project references a Pulse project or Pulse NuGet package.
- Cypress source is not imported or published until its license permits the
  intended repository use.
- Core/App/Protocol projects do not reference concrete serial or Cypress assemblies.
- MSA and HCI share one atomic module-session synchronization boundary.
- All hardware adapters honor an explicit I2C target address.
- `DeviceManager` selects providers through abstractions.
- Existing WPF/CLI high-level module workflows compile against the refactored facade.
- The simulated unit suite covers operation sequences, concurrency, cancellation, timeouts, segmentation, and adapter selection.
- Verification output clearly states that no real hardware was tested.

## 18. Risks and Controls

- **Cypress licensing:** Do not publish imported proprietary source until redistribution rights are confirmed.
- **Protocol assumptions:** Preserve Pulse HCI wire behavior initially and lock it down with characterization tests before cleanup.
- **Address ambiguity:** Canonicalize addresses at the transport boundary and test both legacy `0xA0` inputs and canonical `0x50`.
- **Concurrency regressions:** Use deterministic interleaving tests around the shared module-session gate.
- **UI regressions:** Preserve `ICmisDevice` and typed facade behavior while moving internals.
- **False verification confidence:** Separate simulated unit success from future hardware validation in all reports.
