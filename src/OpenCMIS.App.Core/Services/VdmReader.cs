using OpenCMIS.Protocol.Abstractions;
using OpenCMIS.Protocol.Abstractions.Models;
using OpenCMIS.Shared;

namespace OpenCMIS.App.Core.Services;

/// <summary>Reads the CMIS 5.2 descriptor-driven, read-only VDM snapshot.</summary>
public sealed class VdmReader(IRegisterAccess registers)
{
    public async Task<VdmDiagnostics> ReadAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var capability = await ReadByteAsync(CmisConstants.VdmCapabilityPage,
            CmisConstants.VdmCapabilityByte, cancellationToken);
        if (capability is null)
            return new VdmDiagnostics { ReadStatus = VdmReadStatus.Unavailable };
        if ((capability & CmisConstants.VdmCapabilityBit) == 0)
            return new VdmDiagnostics { IsSupported = false };

        var advertisement = await ReadByteAsync(0x2F, 0x80, cancellationToken);
        if (advertisement is null)
            return new VdmDiagnostics { IsSupported = true, ReadStatus = VdmReadStatus.Unavailable };
        var groupCount = (advertisement.Value & 0x03) + 1;
        var flags = await ReadPageAsync(CmisConstants.VdmFlagsPage, cancellationToken, groupCount * 32);
        var partial = flags.Length < groupCount * 32;
        var observables = new List<VdmObservable>();
        for (var group = 0; group < groupCount; group++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var descriptorPage = (byte)(CmisConstants.VdmDescriptorPageStart + group);
            var samplePage = (byte)(CmisConstants.VdmSamplePageStart + group);
            var descriptors = await ReadPageAsync(descriptorPage, cancellationToken);
            var samples = await ReadPageAsync(samplePage, cancellationToken);
            partial |= descriptors.Length < 128 || samples.Length < 128;
            var slots = Math.Min(descriptors.Length, 128) / CmisConstants.VdmObservableSlotSize;
            for (var slot = 0; slot < slots; slot++)
            {
                var offset = slot * CmisConstants.VdmObservableSlotSize;
                var descriptor = new[] { descriptors[offset], descriptors[offset + 1] };
                if (descriptor[1] == 0)
                    continue;

                var flagIndex = group * 32 + slot / 2;
                var flagAvailable = flagIndex < flags.Length;
                var flagByte = flagAvailable ? flags[flagIndex] : (byte)0;
                var nibble = slot % 2 == 0 ? flagByte & 0x0F : flagByte >> 4;
                observables.Add(new VdmObservable
                {
                    Instance = group * 64 + slot + 1,
                    Descriptor = descriptor,
                    Sample = offset + 1 < samples.Length
                        ? (ushort)(samples[offset] << 8 | samples[offset + 1]) : null,
                    Flags = !flagAvailable ? new VdmObservableFlags() : new VdmObservableFlags
                    {
                        HighAlarm = (nibble & 0x01) != 0,
                        HighWarning = (nibble & 0x04) != 0,
                        LowWarning = (nibble & 0x08) != 0,
                        LowAlarm = (nibble & 0x02) != 0
                    }
                });
            }
        }

        return new VdmDiagnostics
        {
            IsSupported = true,
            ReadStatus = partial ? VdmReadStatus.Partial : VdmReadStatus.Complete,
            ObservableInstances = observables
        };
    }

    private async Task<byte?> ReadByteAsync(byte page, byte address, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var value = await registers.ReadByteAsync(page, address);
            cancellationToken.ThrowIfCancellationRequested();
            return value;
        }
        catch (OperationCanceledException) { throw; }
        catch { return null; }
    }

    private async Task<byte[]> ReadPageAsync(byte page, CancellationToken cancellationToken, int length = 128)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var bytes = await registers.ReadBlockAsync(page, CmisConstants.VdmObservableOffset, length);
            cancellationToken.ThrowIfCancellationRequested();
            return bytes;
        }
        catch (OperationCanceledException) { throw; }
        catch
        {
            return [];
        }
    }
}
