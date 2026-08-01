namespace OpenCMIS.Transport.Abstractions;

/// <summary>
/// Describes a typed adapter connection and its optical-module I2C address.
/// </summary>
public abstract record I2cConnectionProfile
{
    protected I2cConnectionProfile(
        string adapterId,
        I2cDeviceAddress deviceAddress)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(adapterId);
        AdapterId = adapterId;
        DeviceAddress = deviceAddress;
    }

    public string AdapterId { get; }

    public I2cDeviceAddress DeviceAddress { get; }
}

public sealed record SerialI2cConnectionProfile : I2cConnectionProfile
{
    public SerialI2cConnectionProfile(
        string adapterId,
        string portName,
        int baudRate,
        I2cDeviceAddress deviceAddress)
        : base(adapterId, deviceAddress)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(portName);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(baudRate);

        PortName = portName;
        BaudRate = baudRate;
    }

    public string PortName { get; }

    public int BaudRate { get; }
}

public sealed record HmMultiChannelConnectionProfile : I2cConnectionProfile
{
    public HmMultiChannelConnectionProfile(
        string adapterId,
        string portName,
        int baudRate,
        byte channel,
        I2cDeviceAddress deviceAddress)
        : base(adapterId, deviceAddress)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(portName);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(baudRate);

        if (channel is < 1 or > 5)
        {
            throw new ArgumentOutOfRangeException(
                nameof(channel),
                channel,
                "HM multichannel address must be between 1 and 5.");
        }

        PortName = portName;
        BaudRate = baudRate;
        Channel = channel;
    }

    public string PortName { get; }

    public int BaudRate { get; }

    public byte Channel { get; }
}

public sealed record CypressI2cConnectionProfile : I2cConnectionProfile
{
    public CypressI2cConnectionProfile(
        string adapterId,
        string serialNumber,
        int port,
        int speedKhz,
        I2cDeviceAddress deviceAddress)
        : base(adapterId, deviceAddress)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serialNumber);
        ArgumentOutOfRangeException.ThrowIfNegative(port);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(speedKhz);

        SerialNumber = serialNumber;
        Port = port;
        SpeedKhz = speedKhz;
    }

    public string SerialNumber { get; }

    public int Port { get; }

    public int SpeedKhz { get; }
}
