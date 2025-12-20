namespace OpenCMIS.Core
{
    /// <summary>
    ///     Represents exceptions related to invalid register operations.
    /// </summary>
    public class InvalidRegisterException : CmisException
    {
        /// <summary>
        ///     Initializes a new instance of the InvalidRegisterException class.
        /// </summary>
        public InvalidRegisterException() : base("Invalid register address or page.")
        {
        }

        /// <summary>
        ///     Initializes a new instance of the InvalidRegisterException class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public InvalidRegisterException(string message) : base(message)
        {
        }
    }

    /// <summary>
    ///     Represents exceptions related to protocol violations.
    /// </summary>
    public class ProtocolViolationException : CmisException
    {
        /// <summary>
        ///     Initializes a new instance of the ProtocolViolationException class.
        /// </summary>
        public ProtocolViolationException() : base("CMIS protocol violation detected.")
        {
        }

        /// <summary>
        ///     Initializes a new instance of the ProtocolViolationException class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public ProtocolViolationException(string message) : base(message)
        {
        }
    }
}