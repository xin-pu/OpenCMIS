namespace OpenCMIS.Shared
{
    /// <summary>
    ///     Defines the module state machine states.
    /// </summary>
    public enum ModuleState
    {
        /// <summary>
        ///     Initialization state.
        /// </summary>
        Initialization = 0,

        /// <summary>
        ///     Low power state.
        /// </summary>
        LowPwr = 1,

        /// <summary>
        ///     Power up state.
        /// </summary>
        PwrUp = 2,

        /// <summary>
        ///     Ready state.
        /// </summary>
        Ready = 3,

        /// <summary>
        ///     Power down state.
        /// </summary>
        PwrDn = 4,

        /// <summary>
        ///     Fault state.
        /// </summary>
        Fault = 5
    }
}