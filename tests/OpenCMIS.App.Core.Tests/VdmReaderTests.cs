using OpenCMIS.Protocol.Abstractions.Models;
using Xunit;

namespace OpenCMIS.App.Core.Tests;

public sealed class VdmReaderTests
{
    [Fact]
    public void DescriptorSampleAndFlagsAreRetainedForInstanceOne()
    {
        var descriptor = new byte[] { 0x12, 0x34 };
        var flags = new VdmObservableFlags
        {
            HighAlarm = true,
            HighWarning = false,
            LowWarning = true,
            LowAlarm = false
        };
        var diagnostics = new VdmDiagnostics
        {
            ObservableInstances =
            [
                new VdmObservable
                {
                    Instance = 1,
                    Descriptor = descriptor,
                    Sample = 0xABCD,
                    Flags = flags
                }
            ]
        };

        var observable = Assert.Single(diagnostics.ObservableInstances);
        Assert.Equal(1, observable.Instance);
        Assert.Equal(descriptor, observable.Descriptor);
        Assert.Equal((ushort)0xABCD, observable.Sample);
        Assert.Same(flags, observable.Flags);
    }
}
