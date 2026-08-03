namespace OpenCMIS.Protocol.Abstractions.Models
{
    /// <summary>
    ///     Decoded CMIS interrupt flag latches (registers 0x04-0x05, 2 bytes).
    ///     Each bit is a latched interrupt that is cleared on read.
    /// </summary>
    public sealed class CmisInterruptFlags
    {
        /// <summary>
        ///     Raw interrupt flag bytes from registers 0x04-0x05 (MSB order).
        /// </summary>
        public byte[] RawBytes { get; init; } = [];

        // Byte 0 (register 0x04) bits

        /// <summary>Temperature high alarm interrupt.</summary>
        public bool TempHighAlarm { get; init; }

        /// <summary>Temperature low alarm interrupt.</summary>
        public bool TempLowAlarm { get; init; }

        /// <summary>VCC high alarm interrupt.</summary>
        public bool VccHighAlarm { get; init; }

        /// <summary>VCC low alarm interrupt.</summary>
        public bool VccLowAlarm { get; init; }

        /// <summary>TX power high alarm interrupt (any lane).</summary>
        public bool TxPowerHighAlarm { get; init; }

        /// <summary>TX power low alarm interrupt (any lane).</summary>
        public bool TxPowerLowAlarm { get; init; }

        /// <summary>RX power high alarm interrupt (any lane).</summary>
        public bool RxPowerHighAlarm { get; init; }

        /// <summary>RX power low alarm interrupt (any lane).</summary>
        public bool RxPowerLowAlarm { get; init; }

        // Byte 1 (register 0x05) bits

        /// <summary>TX bias high alarm interrupt (any lane).</summary>
        public bool TxBiasHighAlarm { get; init; }

        /// <summary>TX bias low alarm interrupt (any lane).</summary>
        public bool TxBiasLowAlarm { get; init; }

        /// <summary>TX fault interrupt (any lane).</summary>
        public bool TxFault { get; init; }

        /// <summary>RX LOS interrupt (any lane).</summary>
        public bool RxLOS { get; init; }

        /// <summary>Reserved bits (bits 12-15 of the flag word).</summary>
        public byte Reserved { get; init; }

        /// <summary>
        ///     Returns human-readable alert names for bits that are set.
        /// </summary>
        public IReadOnlyList<string> GetActiveFlags()
        {
            var flags = new List<string>();
            AddIf(TempHighAlarm,    "Temperature high alarm");
            AddIf(TempLowAlarm,     "Temperature low alarm");
            AddIf(VccHighAlarm,     "VCC high alarm");
            AddIf(VccLowAlarm,      "VCC low alarm");
            AddIf(TxPowerHighAlarm, "TX power high alarm");
            AddIf(TxPowerLowAlarm,  "TX power low alarm");
            AddIf(RxPowerHighAlarm, "RX power high alarm");
            AddIf(RxPowerLowAlarm,  "RX power low alarm");
            AddIf(TxBiasHighAlarm,  "TX bias high alarm");
            AddIf(TxBiasLowAlarm,   "TX bias low alarm");
            AddIf(TxFault,          "TX fault");
            AddIf(RxLOS,            "RX LOS");
            return flags;

            void AddIf(bool condition, string message)
            {
                if (condition)
                    flags.Add(message);
            }
        }
    }
}
