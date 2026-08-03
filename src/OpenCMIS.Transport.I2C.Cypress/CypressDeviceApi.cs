using OpenCMIS.Cypress;

namespace OpenCMIS.Transport.I2C.Cypress
{
    public sealed class CypressDeviceApi : ICypressDeviceApi
    {
        private CyUSBDevices? _devices;
        private EZUSBDevice?  _selectedDevice;
        private bool          _disposed;

        public IReadOnlyList<CypressDeviceDescriptor> Discover()
        {
            ThrowIfDisposed();
            EnsureDevices();

            return _devices!
                  .GetEzusbDevice(DeviceType.DeviceFIC2USB)
                  .Select(ToDescriptor)
                  .Concat(
                           _devices.GetEzusbDevice(DeviceType.DeviceEui3)
                                   .Select(ToDescriptor))
                  .ToArray();
        }

        public bool Open(string serialNumber)
        {
            ThrowIfDisposed();
            ArgumentException.ThrowIfNullOrWhiteSpace(serialNumber);
            EnsureDevices();

            try
            {
                _selectedDevice = _devices!.GetEzusbDevice(serialNumber);
                return _selectedDevice is DeviceFIC2USB or DeviceEUI3;
            }
            catch (KeyNotFoundException)
            {
                _selectedDevice = null;
                return false;
            }
        }

        public bool Read(int        port,
                         int        speedKhz,
                         byte       address8Bit,
                         int        length,
                         out byte[] data)
        {
            ThrowIfDisposed();
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(length);
            data = new byte[length];

            switch (_selectedDevice)
            {
                case DeviceFIC2USB fic2Usb:
                    return fic2Usb.I2CRead(
                            ToFicPort(port),
                            ToFicSpeed(speedKhz),
                            address8Bit,
                            length,
                            ref data);

                case DeviceEUI3 eui3:
                    ValidateEuiPort(port);
                    ValidateEuiSpeed(speedKhz);
                    eui3.I2CSetFrequency(speedKhz);
                    eui3.I2CRead(address8Bit, ref data, length);
                    return true;

                default: return false;
            }
        }

        public bool Write(int                port,
                          int                speedKhz,
                          byte               address8Bit,
                          ReadOnlySpan<byte> data)
        {
            ThrowIfDisposed();
            if (data.IsEmpty)
            {
                throw new ArgumentException(
                        "Transfer buffer cannot be empty.",
                        nameof(data));
            }

            var buffer = data.ToArray();
            switch (_selectedDevice)
            {
                case DeviceFIC2USB fic2Usb:
                    return fic2Usb.I2CWrite(
                            ToFicPort(port),
                            ToFicSpeed(speedKhz),
                            address8Bit,
                            buffer);

                case DeviceEUI3 eui3:
                    ValidateEuiPort(port);
                    ValidateEuiSpeed(speedKhz);
                    eui3.I2CSetFrequency(speedKhz);
                    eui3.I2CWrite(address8Bit, buffer, buffer.Length);
                    return true;

                default: return false;
            }
        }

        public void Close()
        {
            _selectedDevice = null;
        }

        public ValueTask DisposeAsync()
        {
            if (!_disposed)
            {
                Close();
                _devices?.Dispose();
                _devices  = null;
                _disposed = true;
            }

            return ValueTask.CompletedTask;
        }

        private void EnsureDevices()
        {
            _devices ??= new ();
        }

        private static CypressDeviceDescriptor ToDescriptor(EZUSBDevice device)
        {
            var kind = device switch
                       {
                           DeviceFIC2USB => CypressDeviceKind.Fic2Usb,
                           DeviceEUI3    => CypressDeviceKind.Eui3,
                           _ => throw new NotSupportedException(
                                        $"Unsupported Cypress device type: {device.GetType().Name}.")
                       };

            return new (
                    device.SerialNumber,
                    kind,
                    $"{kind} {device.SerialNumber}");
        }

        private static FIC2USB_I2CPort ToFicPort(int port)
        {
            if (port is < 0 or > 7)
            {
                throw new ArgumentOutOfRangeException(
                        nameof(port),
                        port,
                        "FIC2USB I2C port must be between 0 and 7.");
            }

            return (FIC2USB_I2CPort) port;
        }

        private static FIC2USB_I2CSpeed ToFicSpeed(int speedKhz)
        {
            return speedKhz switch
                   {
                       100 => FIC2USB_I2CSpeed.LS100Khz,
                       400 => FIC2USB_I2CSpeed.HS400Khz,
                       _ => throw new ArgumentOutOfRangeException(
                                    nameof(speedKhz),
                                    speedKhz,
                                    "FIC2USB supports 100 kHz or 400 kHz.")
                   };
        }

        private static void ValidateEuiPort(int port)
        {
            if (port != 0)
            {
                throw new ArgumentOutOfRangeException(
                        nameof(port),
                        port,
                        "EUI3 exposes one I2C controller at logical port 0.");
            }
        }

        private static void ValidateEuiSpeed(int speedKhz)
        {
            if (speedKhz is not (50 or 90 or 200 or 400))
            {
                throw new ArgumentOutOfRangeException(
                        nameof(speedKhz),
                        speedKhz,
                        "EUI3 supports 50, 90, 200, or 400 kHz.");
            }
        }

        private void ThrowIfDisposed()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
        }
    }

    public sealed class CypressDeviceApiFactory : ICypressDeviceApiFactory
    {
        public ICypressDeviceApi Create()
        {
            return new CypressDeviceApi();
        }
    }
}
