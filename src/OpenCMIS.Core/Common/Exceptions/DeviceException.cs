namespace OpenCMIS.Core
{
    /// <summary>
    ///     Represents exceptions related to device operations.
    /// </summary>
    public class DeviceNotConnectedException : CmisException
    {
        /// <summary>
        ///     Initializes a new instance of the DeviceNotConnectedException class.
        /// </summary>
        public DeviceNotConnectedException() : base("Device is not connected.")
        {
        }

        /// <summary>
        ///     Initializes a new instance of the DeviceNotConnectedException class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public DeviceNotConnectedException(string message) : base(message)
        {
        }
    }

    /// <summary>
    ///     Represents exceptions related to device timeout operations.
    /// </summary>
    public class DeviceTimeoutException : CmisException
    {
        /// <summary>
        ///     Initializes a new instance of the DeviceTimeoutException class.
        /// </summary>
        public DeviceTimeoutException() : base("Device operation timed out.")
        {
        }

        /// <summary>
        ///     Initializes a new instance of the DeviceTimeoutException class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public DeviceTimeoutException(string message) : base(message)
        {
        }
    }

    /// <summary>
    ///     Represents exceptions related to device communication errors.
    /// </summary>
    public class DeviceCommunicationException : CmisException
    {
        /// <summary>
        ///     Initializes a new instance of the DeviceCommunicationException class.
        /// </summary>
        public DeviceCommunicationException() : base("Device communication error occurred.")
        {
        }

        /// <summary>
        ///     Initializes a new instance of the DeviceCommunicationException class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public DeviceCommunicationException(string message) : base(message)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the DeviceCommunicationException class with a specified error message and a reference
        ///     to the inner exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public DeviceCommunicationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}