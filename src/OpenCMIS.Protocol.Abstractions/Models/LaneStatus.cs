namespace OpenCMIS.Protocol.Abstractions.Models
{
    public class LaneStatus
    {
        public int LaneNumber { get; set; }
        public double TxPower { get; set; }
        public double RxPower { get; set; }
        public double TxBias { get; set; }

        /// <summary>Raw lane status flags byte (0xA6) for debug.</summary>
        public byte RawFlags { get; set; }

        /// <summary>Data path is enabled for this lane.</summary>
        public bool IsEnabled { get; set; }

        /// <summary>TX fault detected on this lane.</summary>
        public bool HasFault { get; set; }

        /// <summary>TX loss of signal (data path output disabled due to fault).</summary>
        public bool TxLos { get; set; }

        /// <summary>TX loss of lock (CDR or PLL unlocked).</summary>
        public bool TxLol { get; set; }

        /// <summary>RX loss of signal (no optical input detected).</summary>
        public bool RxLos { get; set; }

        /// <summary>RX loss of lock (CDR or PLL unlocked).</summary>
        public bool RxLol { get; set; }

        /// <summary>Reserved/unknown bits in the lane status flags byte.</summary>
        public byte ReservedBits { get; set; }

        public string StatusText { get; set; } = string.Empty;

        public string GetStateText()
        {
            if (HasFault) return "Fault";
            if (!IsEnabled) return "Disabled";
            if (TxLos || RxLos) return "LOS";
            if (TxLol || RxLol) return "LOL";
            return "OK";
        }
    }
}
