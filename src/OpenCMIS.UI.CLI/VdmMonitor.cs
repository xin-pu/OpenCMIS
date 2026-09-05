using OpenCMIS.Protocol.Abstractions.Models;

namespace OpenCMIS.UI.CLI;

public static class VdmMonitor
{
    public static async Task RunAsync(Func<Task<VdmDiagnostics>> read, Action<VdmDiagnostics> publish,
        TimeSpan interval, CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var diagnostics = await read().WaitAsync(cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                publish(diagnostics);
                await Task.Delay(interval, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
    }
}
