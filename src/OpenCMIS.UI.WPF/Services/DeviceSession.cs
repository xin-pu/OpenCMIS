using OpenCMIS.Protocol.Abstractions;
using OpenCMIS.Transport.Abstractions;

namespace OpenCMIS.UI.WPF.Services
{
    public enum DeviceSessionState
    {
        Disconnected,
        Connecting,
        Connected,
        Disconnecting
    }

    public sealed class DeviceSession
    {
        public DeviceInfo?        CurrentDeviceInfo   { get; private set; }
        public ICmisDevice?       CurrentDevice       { get; private set; }
        public DeviceSessionState State               { get; private set; } = DeviceSessionState.Disconnected;
        public Exception?         LastConnectionError { get; private set; }

        public event EventHandler? Changed;

        public void SetConnecting()
        {
            State               = DeviceSessionState.Connecting;
            LastConnectionError = null;
            OnChanged();
        }

        public void SetConnected(DeviceInfo deviceInfo, ICmisDevice device)
        {
            CurrentDeviceInfo   = deviceInfo ?? throw new ArgumentNullException(nameof(deviceInfo));
            CurrentDevice       = device     ?? throw new ArgumentNullException(nameof(device));
            State               = DeviceSessionState.Connected;
            LastConnectionError = null;
            OnChanged();
        }

        public void SetConnectionFailed(Exception exception)
        {
            LastConnectionError = exception ?? throw new ArgumentNullException(nameof(exception));
            CurrentDeviceInfo   = null;
            CurrentDevice       = null;
            State               = DeviceSessionState.Disconnected;
            OnChanged();
        }

        public void SetDisconnecting()
        {
            State = DeviceSessionState.Disconnecting;
            OnChanged();
        }

        public void SetDisconnected()
        {
            CurrentDeviceInfo   = null;
            CurrentDevice       = null;
            State               = DeviceSessionState.Disconnected;
            LastConnectionError = null;
            OnChanged();
        }

        private void OnChanged()
        {
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }
}
