using OpenCMIS.Transport.Abstractions;

namespace OpenCMIS.Transport.I2C.Cypress
{
    public sealed class Eui3I2cAdapter : CypressI2cAdapterBase
    {
        private static readonly I2cTransferCapabilities TransferCapabilities =
                new (48, 48);

        public Eui3I2cAdapter(ICypressDeviceApi api,
                              int               port,
                              int               speedKhz)
                : base(api, port, speedKhz, TransferCapabilities) { }
    }
}
