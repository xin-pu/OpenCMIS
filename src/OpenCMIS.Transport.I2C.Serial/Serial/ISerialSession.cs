namespace OpenCMIS.Transport.I2C.Serial.Serial
{
    /// <summary>
    ///     Provides a mockable boundary around one serial-port session.
    /// </summary>
    public interface ISerialSession : IAsyncDisposable
    {
        bool IsOpen { get; }

        ValueTask OpenAsync(CancellationToken cancellationToken = default);

        ValueTask CloseAsync(CancellationToken cancellationToken = default);

        ValueTask WriteAsync(ReadOnlyMemory<byte> data,
                             CancellationToken    cancellationToken = default);

        ValueTask ReadExactlyAsync(Memory<byte>      destination,
                                   CancellationToken cancellationToken = default);
    }

    public interface ISerialSessionFactory
    {
        ISerialSession Create(string portName, int baudRate);
    }
}
