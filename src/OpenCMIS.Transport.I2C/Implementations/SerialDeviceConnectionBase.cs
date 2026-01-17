using System.IO.Ports;
using OpenCMIS.Shared;

namespace OpenCMIS.Transport.I2C
{
    /// <summary>
    ///     Provides base implementation for short-lived serial device connections.
    ///     This implementation opens and closes the port for each operation to support multi-application access.
    /// </summary>
    public abstract class SerialDeviceConnectionBase : DeviceConnection
    {
        protected readonly string PortName;
        protected readonly int BaudRate;
        protected readonly Parity Parity;
        protected readonly int DataBits;
        protected readonly StopBits StopBits;
        protected readonly int ReadTimeout;
        protected readonly int WriteTimeout;
        protected readonly SemaphoreSlim Semaphore = new(1, 1);

        protected SerialDeviceConnectionBase(
            string portName, 
            int baudRate, 
            Parity parity = Parity.None, 
            int dataBits = 8, 
            StopBits stopBits = StopBits.One,
            int readTimeout = 10000,
            int writeTimeout = 10000)
        {
            PortName = portName;
            BaudRate = baudRate;
            Parity = parity;
            DataBits = dataBits;
            StopBits = stopBits;
            ReadTimeout = readTimeout;
            WriteTimeout = writeTimeout;
        }

        /// <inheritdoc />
        public override bool IsConnected => true; // Short connection is "always ready" in principle

        /// <inheritdoc />
        public override Task<bool> OpenAsync() => Task.FromResult(true);

        /// <inheritdoc />
        public override Task CloseAsync() => Task.CompletedTask;

        /// <inheritdoc />
        public sealed override async Task<byte[]> ReadAsync(int length)
        {
            if (length <= 0) return [];

            return await ExecuteAsync(async (port) => await ReadFromPortAsync(port, length));
        }

        /// <inheritdoc />
        public sealed override async Task WriteAsync(byte[] data)
        {
            if (data.Length == 0) return;

            await ExecuteAsync(async (port) => { await WriteToPortAsync(port, data); });
        }

        /// <summary>
        ///     Writes raw data to the serial port.
        /// </summary>
        protected static async Task WriteToPortAsync(SerialPort port, byte[] data)
        {
            await Task.Run(() => port.Write(data, 0, data.Length));
        }

        /// <summary>
        ///     Reads raw data from the serial port.
        /// </summary>
        protected static async Task<byte[]> ReadFromPortAsync(SerialPort port, int length)
        {
            return await Task.Run(() =>
            {
                var buffer = new byte[length];
                var offset = 0;
                while (offset < length)
                {
                    var read = port.Read(buffer, offset, length - offset);
                    if (read == 0) throw new IOException("Serial port read timeout or closed.");
                    offset += read;
                }

                return buffer;
            });
        }

        /// <summary>
        ///     Executes an action on a temporary serial port connection.
        /// </summary>
        /// <typeparam name="T">The return type of the action.</typeparam>
        /// <param name="action">The action to execute.</param>
        /// <returns>The result of the action.</returns>
        protected async Task<T> ExecuteAsync<T>(Func<SerialPort, Task<T>> action)
        {
            await Semaphore.WaitAsync();
            try
            {
                using var port = new SerialPort(PortName, BaudRate, Parity, DataBits, StopBits);
                port.ReadTimeout  = ReadTimeout;
                port.WriteTimeout = WriteTimeout;
                port.Open();
                return await action(port);
            }
            catch (Exception ex)
            {
                throw new CmisException(CmisErrorCode.DeviceCommunicationError, ex);
            }
            finally
            {
                Semaphore.Release();
            }
        }

        /// <summary>
        ///     Executes an action on a temporary serial port connection.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        protected async Task ExecuteAsync(Func<SerialPort, Task> action)
        {
            await Semaphore.WaitAsync();
            try
            {
                using var port = new SerialPort(PortName, BaudRate, Parity, DataBits, StopBits);
                port.ReadTimeout  = ReadTimeout;
                port.WriteTimeout = WriteTimeout;
                port.Open();
                await action(port);
            }
            catch (Exception ex)
            {
                throw new CmisException(CmisErrorCode.DeviceCommunicationError, ex);
            }
            finally
            {
                Semaphore.Release();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Semaphore.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
