namespace OpenCMIS.Core
{
    /// <summary>
    ///     Represents a CMIS protocol command.
    /// </summary>
    public class CmisCommand
    {
        /// <summary>
        ///     Gets or sets the command type.
        /// </summary>
        public CommandType Type { get; set; }

        /// <summary>
        ///     Gets or sets the command parameters.
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; } = new();
    }
}

