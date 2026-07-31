using OpenCMIS.Transport.Abstractions;

namespace OpenCMIS.Module.Core.Tests.Fakes;

internal sealed class ScriptedI2cRegisterBus : II2cRegisterBus
{
    private readonly Queue<byte[]> _reads = [];
    private readonly TaskCompletionSource _pauseObserved =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _resume =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private bool _pauseAfterFirstWrite;
    private int _writeCount;

    public bool IsOpen { get; private set; }

    public bool IsDisposed { get; private set; }

    public Exception? NextWriteException { get; set; }

    public I2cTransferCapabilities Capabilities { get; } =
        I2cTransferCapabilities.Unbounded;

    public List<string> Operations { get; } = [];

    public Task PauseObserved => _pauseObserved.Task;

    public void QueueRead(byte[] data)
    {
        _reads.Enqueue(data);
    }

    public void PauseAfterFirstWrite()
    {
        _pauseAfterFirstWrite = true;
    }

    public void Resume()
    {
        _resume.TrySetResult();
    }

    public ValueTask OpenAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IsOpen = true;
        return ValueTask.CompletedTask;
    }

    public ValueTask CloseAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IsOpen = false;
        return ValueTask.CompletedTask;
    }

    public ValueTask ReadAsync(
        I2cDeviceAddress device,
        RegisterOffset offset,
        Memory<byte> destination,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Operations.Add($"R {device.Value:X2}:{offset.Value:X2} {destination.Length}");
        var data = _reads.Dequeue();
        if (data.Length != destination.Length)
        {
            throw new InvalidOperationException(
                $"Scripted {data.Length} bytes but requested {destination.Length}.");
        }

        data.CopyTo(destination);
        return ValueTask.CompletedTask;
    }

    public async ValueTask WriteAsync(
        I2cDeviceAddress device,
        RegisterOffset offset,
        ReadOnlyMemory<byte> data,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Operations.Add(
            $"W {device.Value:X2}:{offset.Value:X2} {Convert.ToHexString(data.Span)}");
        _writeCount++;

        if (NextWriteException is not null)
        {
            var exception = NextWriteException;
            NextWriteException = null;
            throw exception;
        }

        if (_pauseAfterFirstWrite && _writeCount == 1)
        {
            _pauseObserved.TrySetResult();
            await _resume.Task.WaitAsync(cancellationToken);
        }
    }

    public ValueTask DisposeAsync()
    {
        IsOpen = false;
        IsDisposed = true;
        return ValueTask.CompletedTask;
    }
}
