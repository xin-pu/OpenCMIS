using System.IO.Ports;

namespace OpenCMIS.Transport.I2C.Serial.Providers
{
    public interface ISerialPortCatalog
    {
        IReadOnlyList<string> GetPortNames();
    }

    public sealed class SystemSerialPortCatalog : ISerialPortCatalog
    {
        public IReadOnlyList<string> GetPortNames()
        {
            return SerialPort.GetPortNames()
                             .Order(StringComparer.OrdinalIgnoreCase)
                             .ToArray();
        }
    }
}
