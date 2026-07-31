using System.IO.Ports;
using OpenCMIS.Transport.Abstractions;

namespace OpenCMIS.Transport.I2C.Serial.Serial;

/// <summary>
/// Implements a serial session using <see cref="SerialPort"/>.
/// </summary>
public sealed class SerialPortSession : ISerialSession
{
    private readonly SerialPort? _serialPort;
    private Stream? _stream;
    private bool _disposed;

    public SerialPortSession(
        SerialI2cConnectionProfile profile,
        int readTimeout = 10000,
        int writeTimeout = 10000)
        : this(
            profile?.PortName ?? throw new ArgumentNullException(nameof(profile)),
            profile.BaudRate,
            readTimeout,
            writeTimeout)
    {
    }

    public SerialPortSession(
        string portName,
        int baudRate,
        int readTimeout = 10000,
        int writeTimeout = 10000)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(portName);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(baudRate);

        _serialPort = new SerialPort(
            portName,
            baudRate,
            Parity.None,
            8,
            StopBits.One)
        {
            ReadTimeout = readTimeout,
            WriteTimeout = writeTimeout
        };
    }

    public SerialPortSession(Stream stream)
    {
        _stream = stream ?? throw new ArgumentNullException(nameof(stream));
    }

    public bool IsOpen => _serialPort?.IsOpen ?? _stream is not null;

    public ValueTask OpenAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        cancellationToken.ThrowIfCancellationRequested();

        if (_serialPort is not null && !_serialPort.IsOpen)
        {
            _serialPort.Open();
            _stream = _serialPort.BaseStream;
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask CloseAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (_serialPort?.IsOpen == true)
        {
            _serialPort.Close();
            _stream = null;
        }

        return ValueTask.CompletedTask;
    }

    public async ValueTask WriteAsync(
        ReadOnlyMemory<byte> data,
        CancellationToken cancellationToken = default)
    {
        var stream = GetOpenStream();
        await stream.WriteAsync(data, cancellationToken).ConfigureAwait(false);
        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask ReadExactlyAsync(
        Memory<byte> destination,
        CancellationToken cancellationToken = default)
    {
        var stream = GetOpenStream();
        var totalRead = 0;

        while (totalRead < destination.Length)
        {
            var read = await stream.ReadAsync(
                    destination[totalRead..],
                    cancellationToken)
                .ConfigureAwait(false);

            if (read == 0)
            {
                throw new EndOfStreamException(
                    $"Serial stream ended after {totalRead} of {destination.Length} bytes.");
            }

            totalRead += read;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        if (_serialPort is not null)
        {
            if (_serialPort.IsOpen)
            {
                _serialPort.Close();
            }

            _serialPort.Dispose();
        }
        else if (_stream is not null)
        {
            await _stream.DisposeAsync().ConfigureAwait(false);
        }

        _stream = null;
        _disposed = true;
    }

    private Stream GetOpenStream()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _stream ??
               throw new InvalidOperationException("The serial session is not open.");
    }
}

public sealed class SerialPortSessionFactory : ISerialSessionFactory
{
    public ISerialSession Create(string portName, int baudRate)
    {
        return new SerialPortSession(portName, baudRate);
    }
}
