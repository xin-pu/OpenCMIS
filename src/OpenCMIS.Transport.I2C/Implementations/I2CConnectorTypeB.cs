using OpenCMIS.Transport.Abstractions;
using OpenCMIS.Transport.I2C.Serial.Adapters;

namespace OpenCMIS.Transport.I2C;

/// <summary>
/// Compatibility name for the HM serial I2C adapter.
/// </summary>
[Obsolete(
    "Use HmSerialI2cAdapter or adapter ID 'hm'. " +
    "This compatibility name will be removed in a future release.")]
public sealed class I2CConnectorTypeB : LegacyRegisterTransportAdapter
{
    public I2CConnectorTypeB(
        string portName,
        int baudRate = 1500000,
        byte slaveAddress = 0xA0)
        : this(
            HmSerialI2cAdapter.CreateDefault(portName, baudRate),
            slaveAddress)
    {
    }

    public I2CConnectorTypeB(
        II2cRegisterBus inner,
        byte slaveAddress = 0xA0)
        : base(inner, slaveAddress)
    {
    }
}
