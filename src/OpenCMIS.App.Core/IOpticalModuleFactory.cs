using OpenCMIS.Protocol.Abstractions;
using OpenCMIS.Transport.Abstractions;

namespace OpenCMIS.App.Core
{
    public interface IOpticalModuleFactory
    {
        ValueTask<ICmisDevice> CreateAsync(DeviceInfo        deviceInfo,
                                           II2cRegisterBus   bus,
                                           CancellationToken cancellationToken = default);
    }
}
