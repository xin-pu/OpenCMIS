namespace OpenCMIS.Protocol.Abstractions.Models
{
    public class MonitorValue
    {
        public double Value { get; set; }
        public string Unit { get; set; } = string.Empty;

        /// <summary>Raw monitor register bytes (MSB first) for debug.</summary>
        public byte[] RawBytes { get; set; } = [];

        public bool HasAlarm { get; set; }
        public bool HasWarning { get; set; }
        public double AlarmHigh { get; set; }
        public double AlarmLow { get; set; }
        public double WarnHigh { get; set; }
        public double WarnLow { get; set; }

        /// <summary>
        ///     Gets or sets whether the low warning threshold is available from the module.
        ///     When false, WarnLow and RawWarnLowBytes should be ignored.
        /// </summary>
        public bool LowWarnAvailable { get; set; } = true;

        /// <summary>Raw alarm high threshold bytes (MSB first) for debug.</summary>
        public byte[] RawAlarmHighBytes { get; set; } = [];

        /// <summary>Raw alarm low threshold bytes (MSB first) for debug.</summary>
        public byte[] RawAlarmLowBytes { get; set; } = [];

        /// <summary>Raw warning high threshold bytes (MSB first) for debug.</summary>
        public byte[] RawWarnHighBytes { get; set; } = [];

        /// <summary>Raw warning low threshold bytes (MSB first) for debug.</summary>
        public byte[] RawWarnLowBytes { get; set; } = [];

        public string GetStatusText()
        {
            if (HasAlarm) return "ALARM";
            if (HasWarning) return "WARN";
            return "OK";
        }
    }
}
