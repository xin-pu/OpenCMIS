namespace OpenCMIS.Protocol.Abstractions.Models
{
    /// <summary>
    /// Decoded CMIS module flags (registers 0x06-0x07, 2 bytes).
    /// These indicate module capabilities and current configuration.
    /// </summary>
    public sealed class CmisModuleFlags
    {
        /// <summary>
        /// Raw module flag bytes from registers 0x06-0x07 (MSB order).
        /// </summary>
        public byte[] RawBytes { get; init; } = [];

        // Byte 0 (register 0x06) bits

        /// <summary>CDB (Configuration Data Block) support (FlatMem or Banked).</summary>
        public bool CdbSupported { get; init; }

        /// <summary>Diagnostic monitoring support.</summary>
        public bool DiagMonSupported { get; init; }

        /// <summary>Module state control supported.</summary>
        public bool StateControlSupported { get; init; }

        /// <summary>Reserved or additional capability bits in byte 0.</summary>
        public byte Byte0Reserved { get; init; }

        // Byte 1 (register 0x07)

        /// <summary>Maximum supported data rate code (register 0x07, full byte).</summary>
        public byte MaxDataRate { get; init; }

        /// <summary>
        /// Returns human-readable capability names that are active.
        /// </summary>
        public IReadOnlyList<string> GetActiveCapabilities()
        {
            var caps = new List<string>();
            if (CdbSupported) caps.Add("CDB");
            if (DiagMonSupported) caps.Add("Diagnostic Monitoring");
            if (StateControlSupported) caps.Add("State Control");
            return caps;
        }
    }
}
