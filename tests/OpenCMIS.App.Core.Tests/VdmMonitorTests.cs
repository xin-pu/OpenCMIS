using OpenCMIS.Protocol.Abstractions.Models;
using OpenCMIS.UI.CLI;
using Xunit;

namespace OpenCMIS.App.Core.Tests;

public sealed class VdmMonitorTests
{
    [Fact]
    public async Task Cancellation_exits_while_read_is_pending_without_publishing_or_reading_again()
    {
        var pending = new TaskCompletionSource<VdmDiagnostics>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var cts = new CancellationTokenSource();
        var reads = 0;
        var published = 0;
        var monitor = VdmMonitor.RunAsync(() =>
        {
            reads++;
            return pending.Task;
        }, _ => published++, TimeSpan.FromMilliseconds(1), cts.Token);

        cts.Cancel();
        try
        {
            await monitor.WaitAsync(TimeSpan.FromSeconds(1));
            Assert.Equal(1, reads);
            Assert.Equal(0, published);
        }
        finally
        {
            pending.TrySetResult(new VdmDiagnostics());
            await monitor;
        }
        Assert.Equal(1, reads);
        Assert.Equal(0, published);
    }

    [Fact]
    public async Task Cancellation_before_start_performs_no_read()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await VdmMonitor.RunAsync(() => throw new InvalidOperationException("Unexpected read"),
            _ => throw new InvalidOperationException("Unexpected output"), TimeSpan.FromSeconds(2), cts.Token);
    }
}
