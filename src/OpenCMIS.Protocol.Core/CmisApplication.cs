namespace OpenCMIS.Protocol.Core
{
    /// <summary>
    ///     Represents a CMIS 5.2 Application (product mode).
    ///     Different Applications define different register maps and capabilities for optical modules.
    /// </summary>
    public class CmisApplication
    {
        /// <summary>
        ///     Initializes a new instance of the CmisApplication class.
        /// </summary>
        /// <param name="appCode">The application code.</param>
        /// <param name="name">The application name.</param>
        /// <param name="description">The application description.</param>
        public CmisApplication(byte appCode, string name, string description)
        {
            AppCode     = appCode;
            Name        = name;
            Description = description;
        }

        /// <summary>
        ///     Gets the application code written to the Application Select register.
        /// </summary>
        public byte AppCode { get; }

        /// <summary>
        ///     Gets the human-readable application name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        ///     Gets the application description.
        /// </summary>
        public string Description { get; }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"[0x{AppCode:X2}] {Name}";
        }
    }
}
