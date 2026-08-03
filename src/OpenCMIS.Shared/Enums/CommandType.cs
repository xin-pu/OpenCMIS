namespace OpenCMIS.Shared
{
    /// <summary>
    ///     Defines the types of CMIS commands.
    /// </summary>
    public enum CommandType
    {
        /// <summary>
        ///     Unknown command type.
        /// </summary>
        Unknown = 0,

        /// <summary>
        ///     State machine control command.
        /// </summary>
        StateControl = 1,

        /// <summary>
        ///     Module initialization command.
        /// </summary>
        Initialize = 2
    }
}
