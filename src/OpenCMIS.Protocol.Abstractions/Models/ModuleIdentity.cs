namespace OpenCMIS.Protocol.Abstractions.Models
{
    public class ModuleIdentity
    {
        public string VendorName       { get; set; } = string.Empty;
        public string VendorOUI        { get; set; } = string.Empty;
        public string PartNumber       { get; set; } = string.Empty;
        public string SerialNumber     { get; set; } = string.Empty;
        public string HardwareRevision { get; set; } = string.Empty;
        public string FirmwareRevision { get; set; } = string.Empty;
        public string DateCode         { get; set; } = string.Empty;
        public string ModuleType       { get; set; } = string.Empty;
        public string ConnectorType    { get; set; } = string.Empty;
        public string CmisVersion      { get; set; } = string.Empty;
        public string CLEICode         { get; set; } = string.Empty;
    }
}
