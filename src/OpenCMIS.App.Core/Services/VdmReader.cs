using OpenCMIS.Protocol.Abstractions;
using OpenCMIS.Protocol.Abstractions.Models;
using OpenCMIS.Shared;

namespace OpenCMIS.App.Core.Services;

/// <summary>Reads the CMIS 5.2 descriptor-driven, read-only VDM snapshot.</summary>
public sealed class VdmReader(IRegisterAccess registers)
{
    public async Task<VdmDiagnostics> ReadAsync(CancellationToken cancellationToken = default)
    {
        var capability = await registers.ReadByteAsync(
            CmisConstants.VdmCapabilityPage, CmisConstants.VdmCapabilityByte);
        if ((capability & CmisConstants.VdmCapabilityBit) == 0)
            return new VdmDiagnostics { IsSupported = false };

        var flags = await ReadPageAsync(CmisConstants.VdmFlagsPage, cancellationToken);
        var observables = new List<VdmObservable>();
        for (var group = 0; group < 4; group++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var descriptorPage = (byte)(CmisConstants.VdmDescriptorPageStart + group);
            var samplePage = (byte)(CmisConstants.VdmSamplePageStart + group);
            var descriptors = await ReadPageAsync(descriptorPage, cancellationToken);
            var samples = await ReadPageAsync(samplePage, cancellationToken);
            var slots = Math.Min(descriptors.Length, samples.Length) / CmisConstants.VdmObservableSlotSize;
            for (var slot = 0; slot < slots; slot++)
            {
                var offset = slot * CmisConstants.VdmObservableSlotSize;
                var descriptor = new[] { descriptors[offset], descriptors[offset + 1] };
                if (descriptor[0] == 0 && descriptor[1] == 0)
                    continue;

                var flagIndex = group * 32 + slot / 2;
                var flagByte = flagIndex < flags.Length ? flags[flagIndex] : (byte)0;
                var nibble = slot % 2 == 0 ? flagByte >> 4 : flagByte & 0x0F;
                observables.Add(new VdmObservable
                {
                    Instance = group * 64 + slot + 1,
                    Descriptor = descriptor,
                    Sample = (ushort)(samples[offset] << 8 | samples[offset + 1]),
                    Flags = new VdmObservableFlags
                    {
                        HighAlarm = (nibble & 0x08) != 0,
                        HighWarning = (nibble & 0x04) != 0,
                        LowWarning = (nibble & 0x02) != 0,
                        LowAlarm = (nibble & 0x01) != 0
                    }
                });
            }
        }

        return new VdmDiagnostics { IsSupported = observables.Count > 0, ObservableInstances = observables };
    }

    private async Task<byte[]> ReadPageAsync(byte page, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            return await registers.ReadBlockAsync(
                page, CmisConstants.VdmObservableOffset, 128);
        }
        catch
        {
            return [];
        }
    }
}
