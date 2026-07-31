using OpenCMIS.Shared;
using OpenCMIS.Transport.Abstractions;

namespace OpenCMIS.Module.Core.Hci;

/// <summary>
/// Executes vendor HCI commands within the optical-module session gate.
/// </summary>
public sealed class HciMemoryAccessor : IHciMemoryAccessor
{
    private static readonly RegisterOffset TableRegister = new(0x7F);
    private static readonly RegisterOffset StatusRegister = new(0x80);
    private static readonly RegisterOffset CommandRegister = new(0x81);
    private static readonly byte[] StartCommand = [0x7F];
    private static readonly byte[] EndCommand = [0x7E];

    private readonly OpticalModuleSession _session;
    private readonly HciOptions _options;
    private readonly TimeProvider _timeProvider;

    public HciMemoryAccessor(
        OpticalModuleSession session,
        HciOptions options,
        TimeProvider timeProvider)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _timeProvider = timeProvider ??
                        throw new ArgumentNullException(nameof(timeProvider));

        if (_options.ReadyValues.Count == 0)
        {
            throw new ArgumentException(
                "At least one HCI ready value is required.",
                nameof(options));
        }

        if (_options.Timeout <= TimeSpan.Zero ||
            _options.InitialPollDelay < TimeSpan.Zero ||
            _options.MaximumPollDelay < _options.InitialPollDelay)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                "HCI timing options are invalid.");
        }
    }

    public ValueTask<byte[]> ReadAsync(
        I2cDeviceAddress device,
        HciTableId table,
        RegisterOffset offset,
        int length,
        CancellationToken cancellationToken = default)
    {
        var command = HciCommandCodec.EncodeRead(table, offset, length);
        return _session.ExecuteAsync(
            async (bus, token) =>
            {
                await ExecuteCommandAsync(bus, device, command, token)
                    .ConfigureAwait(false);
                var response = new byte[length + 8];
                await bus.ReadAsync(device, StatusRegister, response, token)
                    .ConfigureAwait(false);
                return HciCommandCodec.ExtractReadPayload(response, length);
            },
            cancellationToken);
    }

    public async ValueTask WriteAsync(
        I2cDeviceAddress device,
        HciTableId table,
        RegisterOffset offset,
        ReadOnlyMemory<byte> data,
        CancellationToken cancellationToken = default)
    {
        var command = HciCommandCodec.EncodeWrite(table, offset, data.Span);
        await _session.ExecuteAsync(
                async (bus, token) =>
                {
                    await ExecuteCommandAsync(bus, device, command, token)
                        .ConfigureAwait(false);
                    return true;
                },
                cancellationToken)
            .ConfigureAwait(false);
    }

    private async ValueTask ExecuteCommandAsync(
        II2cRegisterBus bus,
        I2cDeviceAddress device,
        ReadOnlyMemory<byte> command,
        CancellationToken cancellationToken)
    {
        await bus.WriteAsync(
                device,
                TableRegister,
                StartCommand,
                cancellationToken)
            .ConfigureAwait(false);
        await WaitForReadyAsync(bus, device, cancellationToken)
            .ConfigureAwait(false);
        await bus.WriteAsync(
                device,
                CommandRegister,
                command,
                cancellationToken)
            .ConfigureAwait(false);
        await bus.WriteAsync(
                device,
                StatusRegister,
                EndCommand,
                cancellationToken)
            .ConfigureAwait(false);
        await WaitForReadyAsync(bus, device, cancellationToken)
            .ConfigureAwait(false);
    }

    private async ValueTask WaitForReadyAsync(
        II2cRegisterBus bus,
        I2cDeviceAddress device,
        CancellationToken cancellationToken)
    {
        var started = _timeProvider.GetTimestamp();
        var delay = _options.InitialPollDelay;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var elapsed = _timeProvider.GetElapsedTime(started);
            if (elapsed >= _options.Timeout)
            {
                throw new CmisException(CmisErrorCode.HciCommandTimeout);
            }

            var status = new byte[1];
            await bus.ReadAsync(
                    device,
                    StatusRegister,
                    status,
                    cancellationToken)
                .ConfigureAwait(false);
            if (_options.ReadyValues.Contains(status[0]))
            {
                return;
            }

            var remaining = _options.Timeout -
                            _timeProvider.GetElapsedTime(started);
            if (remaining <= TimeSpan.Zero)
            {
                throw new CmisException(CmisErrorCode.HciCommandTimeout);
            }

            var actualDelay = delay <= remaining ? delay : remaining;
            if (actualDelay > TimeSpan.Zero)
            {
                await Task.Delay(
                        actualDelay,
                        _timeProvider,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                await Task.Yield();
            }

            var doubledTicks = Math.Min(
                delay.Ticks * 2,
                _options.MaximumPollDelay.Ticks);
            delay = TimeSpan.FromTicks(doubledTicks);
        }
    }
}
