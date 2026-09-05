# CMIS VDM redesign

## Purpose

Replace the provisional, fixed-register VDM implementation with the CMIS 5.2
descriptor-driven VDM model.  The implementation must be safe for real modules:
it is read-only until each writable CMIS control has its own specified semantics.

## Scope

The device layer first checks the general VDM capability at Page 01h byte 142,
bit 6.  It then reads VDM Page 2Fh for VDM advertisement/control metadata and
the descriptor slots on Pages 20h-23h. Page 2Fh byte 128 bits 1:0 advertise
the number of groups minus one; only advertised descriptor/sample groups are read.
Each two-byte descriptor whose odd-address byte (Type ID) is non-zero defines
one observable instance. Type ID zero is unused regardless of the even byte.
The corresponding two-byte sample is read
from the same offset on Pages 24h-27h.  Flags are read from Page 2Ch: each byte
contains the four threshold-crossing flags for two observable instances. The
first instance uses the low nibble, the second the high nibble. Within each
nibble, bits 0, 1, 2, 3 are high alarm, low alarm, high warning, low warning.

The public snapshot exposes generic observable instances: ordinal, descriptor
bytes, sample bytes/value, and the four flag bits.  A presentation layer may
label an observable only when the descriptor's Observable ID is recognised; it
must preserve unknown descriptors instead of guessing a physical quantity.

## Safety and compatibility

Pages 20h-23h are never written.  The current `WriteVdmConfigAsync`, its CLI
`config set` command, and WPF configuration write command are removed.  Page
2Dh masks and Page 2Fh controls are deliberately out of scope for this read-only
change; adding them requires a separate specification for access, reserved-bit
preservation, and handshake state.

Modules that do not advertise VDM, have empty descriptor pages, or return a
short/failed read produce an unsupported, partial, or unavailable diagnostics
snapshot rather than fabricated monitor readings. Capability/advertisement
failure is unavailable; known support remains separate from read completeness.
Short descriptor pages retain only complete descriptor pairs. Missing samples
and flag bytes remain null (displayed as unknown); known descriptors are retained
even if their samples cannot be read. Descriptor write protection is enforced at
the shared RegisterAccess byte/block boundary, including banked access.

## UI and CLI

CLI output lists VDM instances and their raw descriptor/sample values plus
decoded flag states.  The WPF view lists the same general instance rows and
removes the configuration UI.  CLI monitoring exits after Ctrl+C.  WPF keeps a
single tracked monitoring task; device replacement, interval changes, and stop
wait for the previous task to finish before starting another.

## Tests

Tests use simulated register access to prove capability detection, descriptor to
sample/flag mapping, empty descriptors, unsupported modules, and short-read
handling.  Lifecycle tests prove that stopping monitoring ends the active loop
before a replacement loop begins.

## References

CMIS 5.2 section 8.19 defines descriptor pages 20h-23h, sample pages 24h-27h,
threshold pages 28h-2Bh, flags page 2Ch, masks page 2Dh, and advertisement /
control page 2Fh.  General VDM support is advertised in Page 01h byte 142 bit 6.
