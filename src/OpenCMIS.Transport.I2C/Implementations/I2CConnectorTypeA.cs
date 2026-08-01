using OpenCMIS.Transport.Abstractions;
using OpenCMIS.Transport.I2C.Serial.Adapters;

namespace OpenCMIS.Transport.I2C;

/// <summary>
/// Compatibility name for the Linktel serial I2C adapter.
/// </summary>
[Obsolete(
    "Use LinktelSerialI2cAdapter or adapter ID 'linktel'. " +
    "This compatibility name will be removed in a future release.")]
public sealed class I2CConnectorTypeA : LegacyRegisterTransportAdapter
{
    public I2CConnectorTypeA(
        string portName,
        int baudRate = 115200,
        byte slaveAddress = 0xA0)
        : this(
            LinktelSerialI2cAdapter.CreateDefault(portName, baudRate),
            slaveAddress)
    {
    }

    public I2CConnectorTypeA(
        II2cRegisterBus inner,
        byte slaveAddress = 0xA0)
        : base(inner, slaveAddress)
    {
    }
}
