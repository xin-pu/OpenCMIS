using System.IO.Ports;
using OpenCMIS.Protocol.Abstractions;
using OpenCMIS.Protocol.Core;
using OpenCMIS.Shared;
using OpenCMIS.Transport.Abstractions;
using OpenCMIS.Transport.I2C;

namespace OpenCMIS.App.Core
{
    /// <summary>
    ///     Manages device lifecycle, enumeration, and identification.
    /// </summary>
    public class DeviceManager : IDeviceManager
    {
        private const int ProbeTimeoutMs = 500;

        /// <inheritdoc />
        public async Task<IEnumerable<DeviceInfo>> EnumerateDevicesAsync()
        {
            var devices = new List<DeviceInfo>();
            var portNames = SerialPort.GetPortNames();

            foreach (var portName in portNames)
            {
                var deviceInfo = await TryProbePortAsync(portName);
                if (deviceInfo != null)
                    devices.Add(deviceInfo);
            }

            return devices;
        }

        /// <inheritdoc />
        public async Task<ICmisDevice> OpenDeviceAsync(DeviceInfo deviceInfo)
        {
            if (deviceInfo == null)
                throw new CmisException(CmisErrorCode.InvalidParameterValue, nameof(deviceInfo));

            // Extract connection parameters
            var portName = deviceInfo.ConnectionParameters.GetValueOrDefault("PortName", "");
            var baudRate = int.Parse(deviceInfo.ConnectionParameters.GetValueOrDefault("BaudRate", "115200"));
            var slaveAddressStr = deviceInfo.ConnectionParameters.GetValueOrDefault("SlaveAddress", "0xA0");
            var slaveAddress = Convert.ToByte(slaveAddressStr, slaveAddressStr.StartsWith("0x") ? 16 : 10);

            // Create transport based on connection type
            IRegisterTransport transport = deviceInfo.ConnectionType switch
            {
                ConnectionType.I2C => new I2CConnectorTypeA(portName, baudRate, slaveAddress),
                _ => throw new CmisException(CmisErrorCode.InvalidParameterValue, deviceInfo.ConnectionType)
            };

            // Build the dependency chain
            var pageManager = new PageManager(transport);
            var addressingStrategy = new StandardAddressingStrategy();
            var registerAccess = new RegisterAccess(transport, pageManager, addressingStrategy);

            // Open connection
            var connected = await transport.OpenAsync();
            if (!connected)
                throw new CmisException(CmisErrorCode.DeviceConnectionFailed, portName);

            return new CmisDevice(deviceInfo, transport, registerAccess);
        }

        /// <inheritdoc />
        public async Task CloseDeviceAsync(ICmisDevice device)
        {
            if (device == null)
                throw new CmisException(CmisErrorCode.InvalidParameterValue, nameof(device));

            await device.CloseAsync();
        }

        private async Task<DeviceInfo?> TryProbePortAsync(string portName)
        {
            try
            {
                using var cts = new CancellationTokenSource(ProbeTimeoutMs);

                // Try Type A connector first (most common)
                var typeAResult = await ProbeWithConnectorTypeA(portName, cts.Token);
                if (typeAResult != null)
                    return typeAResult;

                return null;
            }
            catch (Exception)
            {
                // Port is not available or not a CMIS device
                return null;
            }
        }

        private static async Task<DeviceInfo?> ProbeWithConnectorTypeA(string portName, CancellationToken ct)
        {
            var connector = new I2CConnectorTypeA(portName, baudRate: 115200, slaveAddress: 0xA0);

            try
            {
                var connected = await connector.OpenAsync();
                if (!connected)
                    return null;

                try
                {
                    // Try to read the Identifier register (0x00) to verify CMIS module
                    var identifier = await connector.ReadRegisterAsync(CmisConstants.RegIdentifier);

                    // Basic validation: CMIS identifiers should be nonzero and in known ranges
                    if (identifier == 0x00 || identifier == 0xFF)
                        return null;

                    return new DeviceInfo
                    {
                        Id               = portName,
                        Name             = $"CMIS Module on {portName}",
                        ConnectionType   = ConnectionType.I2C,
                        ConnectionParameters = new Dictionary<string, string>
                        {
                            ["PortName"]      = portName,
                            ["BaudRate"]      = "115200",
                            ["SlaveAddress"]  = "0xA0",
                            ["ConnectorType"] = "TypeA"
                        }
                    };
                }
                finally
                {
                    await connector.CloseAsync();
                }
            }
            catch
            {
                return null;
            }
        }
    }
}
