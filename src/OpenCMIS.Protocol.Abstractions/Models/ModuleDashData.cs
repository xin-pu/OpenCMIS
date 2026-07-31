using OpenCMIS.Shared;

namespace OpenCMIS.Protocol.Abstractions.Models
{
    public class ModuleDashData
    {
        public ModuleIdentity Identity { get; set; } = new();
        public ModuleMonitors Monitors { get; set; } = new();
        public List<LaneStatus> Lanes { get; set; } = [];
        public ModuleState CurrentState { get; set; }
        public bool IsReady { get; set; }
        public DateTime StatusTimestamp { get; set; }
    }
}
