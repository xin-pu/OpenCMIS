namespace OpenCMIS.Shared
{
    /// <summary>
    ///     Represents errors that occur during CMIS operations.
    /// </summary>
    public class CmisException : Exception
    {
        public CmisException(CmisErrorCode errorCode, string? message = null, Exception? innerException = null)
            : base(message ?? errorCode.ToString(), innerException)
        {
            ErrorCode = errorCode;
        }

        public CmisException(CmisErrorCode errorCode, params object[] args)
            : base(string.Format(errorCode.ToString(), args))
        {
            ErrorCode = errorCode;
        }

        public CmisException(CmisErrorCode errorCode, Exception innerException, params object[] args)
            : base(string.Format(errorCode.ToString(), args), innerException)
        {
            ErrorCode = errorCode;
        }

        public CmisErrorCode ErrorCode { get; }

        public static void ThrowIf(bool condition, CmisErrorCode errorCode, params object[] args)
        {
            if (condition)
                throw new CmisException(errorCode, string.Format(errorCode.ToString(), args));
        }
    }
}