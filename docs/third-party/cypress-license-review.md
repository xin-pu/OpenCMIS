# Cypress Source License Review

- Review date: 2026-07-31
- Source repository: `E:\Code Pulse\pulse.instruments.cypress`
- Source commit: `d2bfd4aacf948935c3e9b9bbba18cf1f63b9b8b7`
- Source project: `src/Pulse.Instruments.Cypress/Pulse.Instruments.Cypress.csproj`
- Copyright holder named in source headers: Cypress Semiconductor Corporation
- Referenced license: `<install>/license/license.rtf`
- License file availability: not present in the source repository or the Cypress installation directories checked on the review machine
- Authorization: the OpenCMIS repository owner confirmed in the Codex task on 2026-07-31 that the company has authority to copy, modify, and commit this source into the OpenCMIS repository
- Authorized scope recorded here: import into this OpenCMIS repository, maintenance, and modification for the company's OpenCMIS development
- External redistribution/public publication: not established by this review; requires a separate license review
- Required notices: preserve all original Cypress copyright and proprietary-license headers and include `THIRD-PARTY-NOTICES.md`
- Signing key: the source project references `cyusb.snk`, but that file is absent; no private key is imported or generated, and the OpenCMIS assembly is not strong-name signed
- Behavioral changes allowed in the import commit: none; mechanical changes are limited to namespace and assembly/build metadata plus trailing-whitespace normalization
- Compatibility warning policy: imported nullable annotations, obsolete Code Access Security usage, legacy pointer comparison, and inexact firmware-file reads are not behaviorally rewritten in the import commit; their compiler/analyzer warnings are isolated in `OpenCMIS.Cypress.csproj`
- Reviewer: repository owner authorization confirmed through the Codex task; implementation review recorded by Codex

This file records the repository authorization decision and import provenance. It is not a substitute for the missing Cypress license text and does not grant rights beyond the scope stated above.
