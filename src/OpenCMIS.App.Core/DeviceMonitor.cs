using OpenCMIS.Protocol.Abstractions;
using OpenCMIS.Protocol.Abstractions.Models;
using OpenCMIS.Shared;
using OpenCMIS.Transport.Abstractions;

namespace OpenCMIS.App.Core
{
    /// <summary>
    ///     Provides real-time monitoring capabilities for device status and alerts.
    /// </summary>
    public class DeviceMonitor
    {
        private readonly ICmisDevice              _device;
        private          CancellationTokenSource? _cancellationTokenSource;
        private          ModuleStatus?            _lastStatus;

        /// <summary>
        ///     Initializes a new instance of the DeviceMonitor class.
        /// </summary>
        /// <param name="device">The CMIS device to monitor.</param>
        public DeviceMonitor(ICmisDevice device)
        {
            _device = device ?? throw new ArgumentNullException(nameof(device));
        }

        /// <summary>
        ///     Gets a value indicating whether monitoring is active.
        /// </summary>
        public bool IsMonitoring { get; private set; }

        /// <summary>
        ///     Occurs when the device status changes.
        /// </summary>
        public event EventHandler<StatusChangedEventArgs>? StatusChanged;

        /// <summary>
        ///     Occurs when a device alert is detected.
        /// </summary>
        public event EventHandler<AlertEventArgs>? Alert;

        /// <summary>
        ///     Starts monitoring the device with the specified interval.
        /// </summary>
        /// <param name="interval">The monitoring interval.</param>
        public Task StartMonitoringAsync(TimeSpan interval)
        {
            if (IsMonitoring)
                return Task.CompletedTask;

            IsMonitoring             = true;
            _cancellationTokenSource = new ();

            _ = Task.Run(() => MonitoringLoopAsync(interval, _cancellationTokenSource.Token));

            return Task.CompletedTask;
        }

        /// <summary>
        ///     Stops monitoring the device.
        /// </summary>
        public async Task StopMonitoringAsync()
        {
            if (!IsMonitoring)
                return;

            IsMonitoring = false;
            _cancellationTokenSource?.Cancel();

            await Task.CompletedTask;
        }

        private async Task MonitoringLoopAsync(TimeSpan interval, CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var currentStatus = await _device.GetStatusAsync();

                    // Check for status changes
                    if (_lastStatus != null && currentStatus.CurrentState != _lastStatus.CurrentState)
                    {
                        var args = new StatusChangedEventArgs(_lastStatus, currentStatus);
                        StatusChanged?.Invoke(this, args);
                    }

                    // Check for alerts
                    if (currentStatus.HasAlerts)
                    {
                        foreach (var alertMessage in currentStatus.ActiveAlerts)
                        {
                            var alertArgs = new AlertEventArgs {AlertType = AlertType.Warning, Message = alertMessage};
                            Alert?.Invoke(this, alertArgs);
                        }
                    }

                    _lastStatus = currentStatus;
                }
                catch (Exception ex)
                {
                    // Report communication errors as alerts
                    var alertArgs = new AlertEventArgs {AlertType = AlertType.Error, Message = $"Monitor error: {ex.Message}"};
                    Alert?.Invoke(this, alertArgs);
                }

                try
                {
                    await Task.Delay(interval, ct);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            // Cleanup
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
            _lastStatus              = null;
        }
    }
}
