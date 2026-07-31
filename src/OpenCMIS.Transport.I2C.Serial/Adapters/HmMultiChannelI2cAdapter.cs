using OpenCMIS.Transport.Abstractions;
using OpenCMIS.Transport.I2C.Serial.Serial;

namespace OpenCMIS.Transport.I2C.Serial.Adapters;

public sealed class HmMultiChannelI2cAdapter : HmSerialI2cAdapter
{
    private static readonly byte[] ReadCommands =
        [0xE2, 0xE4, 0xD3, 0xE6, 0xE8];

    private static readonly byte[] WriteCommands =
        [0xE1, 0xE3, 0xD2, 0xE5, 0xE7];

    private readonly byte _channel;

    public HmMultiChannelI2cAdapter(
        ISerialSessionFactory sessionFactory,
        HmMultiChannelConnectionProfile profile,
        I2cRetryOptions retryOptions,
        TimeProvider timeProvider)
        : base(
            sessionFactory,
            profile?.PortName ?? throw new ArgumentNullException(nameof(profile)),
            profile.BaudRate,
            retryOptions,
            timeProvider)
    {
        _channel = profile.Channel;
    }

    protected override byte ReadCommand => ReadCommands[_channel - 1];

    protected override byte WriteCommand => WriteCommands[_channel - 1];
}
