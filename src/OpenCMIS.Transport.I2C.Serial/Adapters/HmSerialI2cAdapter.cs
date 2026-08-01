using OpenCMIS.Transport.Abstractions;
using OpenCMIS.Transport.I2C.Serial.Codecs;
using OpenCMIS.Transport.I2C.Serial.Serial;

namespace OpenCMIS.Transport.I2C.Serial.Adapters;

public class HmSerialI2cAdapter : SerialI2cAdapterBase
{
    public HmSerialI2cAdapter(
        ISerialSessionFactory sessionFactory,
        SerialI2cConnectionProfile profile,
        I2cRetryOptions retryOptions,
        TimeProvider timeProvider)
        : this(
            sessionFactory,
            profile?.PortName ?? throw new ArgumentNullException(nameof(profile)),
            profile.BaudRate,
            retryOptions,
            timeProvider)
    {
    }

    protected HmSerialI2cAdapter(
        ISerialSessionFactory sessionFactory,
        string portName,
        int baudRate,
        I2cRetryOptions retryOptions,
        TimeProvider timeProvider)
        : base(sessionFactory, portName, baudRate, retryOptions, timeProvider)
    {
    }

    public static HmSerialI2cAdapter CreateDefault(
        string portName,
        int baudRate = 1500000)
    {
        var address = new I2cDeviceAddress(0x50);
        return new HmSerialI2cAdapter(
            new SerialPortSessionFactory(),
            new SerialI2cConnectionProfile("hm", portName, baudRate, address),
            I2cRetryOptions.Default,
            TimeProvider.System);
    }

    protected virtual byte ReadCommand => HmI2cCodec.DefaultReadCommand;

    protected virtual byte WriteCommand => HmI2cCodec.DefaultWriteCommand;

    protected override byte[] EncodeRead(
        I2cDeviceAddress device,
        RegisterOffset offset,
        int length) =>
        HmI2cCodec.EncodeRead(device, offset, length, ReadCommand);

    protected override byte[] EncodeWrite(
        I2cDeviceAddress device,
        RegisterOffset offset,
        ReadOnlySpan<byte> data) =>
        HmI2cCodec.EncodeWrite(device, offset, data, WriteCommand);

    protected override byte[] ParseRead(
        ReadOnlySpan<byte> response,
        int expectedLength) =>
        HmI2cCodec.ParseRead(response, expectedLength);

    protected override void ValidateWrite(ReadOnlySpan<byte> response) =>
        HmI2cCodec.ValidateWrite(response);

    protected override int GetWriteResponseLength() => 1;
}
