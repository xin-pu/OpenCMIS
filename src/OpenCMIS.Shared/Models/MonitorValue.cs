namespace OpenCMIS.Shared.Models
{
    public class MonitorValue
    {
        public double Value { get; set; }
        public string Unit { get; set; } = string.Empty;
        public bool HasAlarm { get; set; }
        public bool HasWarning { get; set; }
        public double AlarmHigh { get; set; }
        public double AlarmLow { get; set; }
        public double WarnHigh { get; set; }
        public double WarnLow { get; set; }

        public string GetStatusText()
        {
            if (HasAlarm) return "ALARM";
            if (HasWarning) return "WARN";
            return "OK";
        }
    }
}
