using OpenCMIS.Protocol.Abstractions.Models;

namespace OpenCMIS.UI.CLI;

public static class VdmDiagnosticsPrinter
{
    public static void Write(TextWriter output, VdmDiagnostics diagnostics)
    {
        if (diagnostics.ReadStatus == VdmReadStatus.Unavailable)
        {
            output.WriteLine("VDM diagnostics unavailable; support or advertisement could not be read.");
            return;
        }
        if (!diagnostics.IsSupported)
        {
            output.WriteLine("VDM is not supported by this module or it has no advertised observables.");
            return;
        }

        output.WriteLine("\nVDM Diagnostics (descriptor-driven, read-only)");
        if (diagnostics.ReadStatus == VdmReadStatus.Partial)
            output.WriteLine("Partial snapshot; some advertised data could not be read.");
        output.WriteLine(new string('=', 60));
        output.WriteLine($"  {"Instance",-8} {"Descriptor",-12} {"Sample",-8} {"High alarm",-12} {"High warning",-13} {"Low warning",-12} {"Low alarm",-10}");
        foreach (var observable in diagnostics.ObservableInstances)
        {
            var sample = observable.Sample is { } value ? $"0x{value:X4}" : "unknown";
            output.WriteLine($"  {observable.Instance,-8} {Convert.ToHexString(observable.Descriptor),-12} {sample,-8} {FlagText(observable.Flags.HighAlarm),-12} {FlagText(observable.Flags.HighWarning),-13} {FlagText(observable.Flags.LowWarning),-12} {FlagText(observable.Flags.LowAlarm),-10}");
        }
    }

    private static string FlagText(bool? value) => value switch { true => "set", false => "clear", null => "unknown" };
}
