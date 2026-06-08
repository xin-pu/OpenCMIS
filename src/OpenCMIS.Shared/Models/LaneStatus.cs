namespace OpenCMIS.Shared.Models
{
    public class LaneStatus
    {
        public int LaneNumber { get; set; }
        public double TxPower { get; set; }
        public double RxPower { get; set; }
        public double TxBias { get; set; }
        public bool IsEnabled { get; set; }
        public bool HasFault { get; set; }
        public string StatusText { get; set; } = string.Empty;

        public string GetStateText()
        {
            if (HasFault) return "Fault";
            if (!IsEnabled) return "Disabled";
            return "OK";
        }
    }
}
