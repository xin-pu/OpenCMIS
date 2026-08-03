namespace OpenCMIS.Transport.I2C.Cypress
{
    public interface ICypressDeviceApi : IAsyncDisposable
    {
        IReadOnlyList<CypressDeviceDescriptor> Discover();

        bool Open(string serialNumber);

        bool Read(int        port,
                  int        speedKhz,
                  byte       address8Bit,
                  int        length,
                  out byte[] data);

        bool Write(int                port,
                   int                speedKhz,
                   byte               address8Bit,
                   ReadOnlySpan<byte> data);

        void Close();
    }

    public interface ICypressDeviceApiFactory
    {
        ICypressDeviceApi Create();
    }

    public sealed record CypressDeviceDescriptor(string            SerialNumber,
                                                 CypressDeviceKind Kind,
                                                 string            DisplayName);

    public enum CypressDeviceKind
    {
        Fic2Usb,
        Eui3
    }
}
