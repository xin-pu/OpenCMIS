using OpenCMIS.Transport.Abstractions;
using OpenCMIS.Transport.I2C.Serial.Codecs;
using OpenCMIS.Transport.I2C.Serial.Serial;

namespace OpenCMIS.Transport.I2C.Serial.Adapters;

public sealed class LinktelSerialI2cAdapter : SerialI2cAdapterBase
{
    public LinktelSerialI2cAdapter(
        ISerialSessionFactory sessionFactory,
        SerialI2cConnectionProfile profile,
        I2cRetryOptions retryOptions,
        TimeProvider timeProvider)
        : base(
            sessionFactory,
            profile?.PortName ?? throw new ArgumentNullException(nameof(profile)),
            profile.BaudRate,
            retryOptions,
            timeProvider)
    {
    }

    public static LinktelSerialI2cAdapter CreateDefault(
        string portName,
        int baudRate = 115200)
    {
        var address = new I2cDeviceAddress(0x50);
        return new LinktelSerialI2cAdapter(
            new SerialPortSessionFactory(),
            new SerialI2cConnectionProfile("linktel", portName, baudRate, address),
            I2cRetryOptions.Default,
            TimeProvider.System);
    }

    protected override byte[] EncodeRead(
        I2cDeviceAddress device,
        RegisterOffset offset,
        int length) =>
        LinktelI2cCodec.EncodeRead(device, offset, length);

    protected override byte[] EncodeWrite(
        I2cDeviceAddress device,
        RegisterOffset offset,
        ReadOnlySpan<byte> data) =>
        LinktelI2cCodec.EncodeWrite(device, offset, data);

    protected override byte[] ParseRead(
        ReadOnlySpan<byte> response,
        int expectedLength) =>
        LinktelI2cCodec.ParseRead(response, expectedLength);

    protected override void ValidateWrite(ReadOnlySpan<byte> response) =>
        LinktelI2cCodec.ValidateWrite(response);

    protected override int GetWriteResponseLength() => 6;
}
