namespace OpenCMIS.Core;

/// <summary>
/// Represents the base exception for all CMIS-related errors.
/// </summary>
public class CmisException : Exception
{
    /// <summary>
    /// Initializes a new instance of the CmisException class.
    /// </summary>
    public CmisException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the CmisException class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public CmisException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the CmisException class with a specified error message and a reference to the inner exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public CmisException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

