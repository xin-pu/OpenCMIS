using OpenCMIS.Transport.Abstractions;

namespace OpenCMIS.Transport.I2C.Cypress
{
    public sealed class Fic2UsbI2cAdapter : CypressI2cAdapterBase
    {
        private static readonly I2cTransferCapabilities TransferCapabilities =
                new (255, 255);

        public Fic2UsbI2cAdapter(ICypressDeviceApi api,
                                 int               port,
                                 int               speedKhz)
                : base(api, port, speedKhz, TransferCapabilities) { }
    }
}
