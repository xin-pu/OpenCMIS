namespace OpenCMIS.Protocol.Abstractions
{
    /// <summary>
    ///     Represents the result of a command execution.
    /// </summary>
    public class CommandResult
    {
        /// <summary>
        ///     Gets or sets a value indicating whether the command execution was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        ///     Gets or sets the result data.
        /// </summary>
        public object? Data { get; set; }

        /// <summary>
        ///     Gets or sets the error message if the command failed.
        /// </summary>
        public string? ErrorMessage { get; set; }
    }
}