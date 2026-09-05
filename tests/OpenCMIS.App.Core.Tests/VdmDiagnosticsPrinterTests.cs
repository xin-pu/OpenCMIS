using OpenCMIS.Protocol.Abstractions.Models;
using OpenCMIS.UI.CLI;
using Xunit;

namespace OpenCMIS.App.Core.Tests;

public sealed class VdmDiagnosticsPrinterTests
{
    [Fact]
    public void Unavailable_capability_is_not_reported_as_unsupported()
    {
        using var output = new StringWriter();
        VdmDiagnosticsPrinter.Write(output, new VdmDiagnostics { ReadStatus = VdmReadStatus.Unavailable });
        Assert.Contains("unavailable", output.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("not supported", output.ToString());
    }

    [Fact]
    public void Partial_rows_show_unknown_sample_and_flags_instead_of_clear_or_hex_zero()
    {
        using var output = new StringWriter();
        VdmDiagnosticsPrinter.Write(output, new VdmDiagnostics
        {
            IsSupported = true, ReadStatus = VdmReadStatus.Partial,
            ObservableInstances = [new VdmObservable { Instance = 1, Descriptor = [0, 1] }]
        });
        var text = output.ToString();
        Assert.Contains("partial", text, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(5, text.Split("unknown").Length - 1);
        Assert.DoesNotContain("clear", text);
        Assert.DoesNotContain("0x", text);
    }
}
