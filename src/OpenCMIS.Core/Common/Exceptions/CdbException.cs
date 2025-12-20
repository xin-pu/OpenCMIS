namespace OpenCMIS.Core
{
    /// <summary>
    ///     Represents exceptions related to CDB validation errors.
    /// </summary>
    public class CdbValidationException : CmisException
    {
        /// <summary>
        ///     Initializes a new instance of the CdbValidationException class.
        /// </summary>
        public CdbValidationException() : base("CDB validation failed.")
        {
        }

        /// <summary>
        ///     Initializes a new instance of the CdbValidationException class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public CdbValidationException(string message) : base(message)
        {
        }
    }

    /// <summary>
    ///     Represents exceptions related to CDB version mismatches.
    /// </summary>
    public class CdbVersionMismatchException : CmisException
    {
        /// <summary>
        ///     Initializes a new instance of the CdbVersionMismatchException class.
        /// </summary>
        public CdbVersionMismatchException() : base("CDB version mismatch.")
        {
        }

        /// <summary>
        ///     Initializes a new instance of the CdbVersionMismatchException class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public CdbVersionMismatchException(string message) : base(message)
        {
        }
    }
}