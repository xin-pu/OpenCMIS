using OpenCMIS.Protocol.Abstractions;

namespace OpenCMIS.UI.WPF.Services
{
    public class CmisGuiService
    {
        public ICmisDevice? CurrentDevice { get; set; }
        public bool IsConnected => CurrentDevice?.IsConnected ?? false;
    }
}
