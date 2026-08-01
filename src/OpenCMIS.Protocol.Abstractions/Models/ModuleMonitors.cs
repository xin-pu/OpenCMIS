namespace OpenCMIS.Protocol.Abstractions.Models
{
    public class ModuleMonitors
    {
        public MonitorValue Temperature { get; set; } = new();
        public MonitorValue VCC { get; set; } = new();
        public List<MonitorValue> TxBiasPerLane { get; set; } = [];
        public List<MonitorValue> TxPowerPerLane { get; set; } = [];
        public List<MonitorValue> RxPowerPerLane { get; set; } = [];
        public double TotalTxPower { get; set; }
        public double TotalRxPower { get; set; }
        public double MaxTxBias { get; set; }

        public void ComputeTotals()
        {
            TotalTxPower = TxPowerPerLane.Sum(m => m.Value);
            TotalRxPower = RxPowerPerLane.Sum(m => m.Value);
            MaxTxBias = TxBiasPerLane.Count > 0 ? TxBiasPerLane.Max(m => m.Value) : 0;
        }
    }
}
