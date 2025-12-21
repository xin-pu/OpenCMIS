using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using OpenCMIS.Core.Extensions;

namespace OpenCMIS.Core
{
    /// <summary>
    ///     Represents errors that occur during CMIS application execution.
    /// </summary>
    [Serializable]
    public class CmisException : Exception
    {
        private readonly object[] _formatArgs;

        /// <summary>
        ///     Initializes a new instance of the <see cref="CmisException" /> class with a default error code.
        /// </summary>
        public CmisException()
                : this(CmisErrorCode.NotDefined) { }

        /// <summary>
        ///     Initializes a new instance of the <see cref="CmisException" /> class with a specified error message.
        ///     This constructor is provided for backward compatibility.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public CmisException(string message)
                : base(message)
        {
            ErrorCode   = CmisErrorCode.NotDefined;
            _formatArgs = Array.Empty<object>();
            HResult     = unchecked((int) (0xB0000000 + (ushort) CmisErrorCode.NotDefined));
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="CmisException" /> class with a specified error message and a reference
        ///     to the inner exception.
        ///     This constructor is provided for backward compatibility.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public CmisException(string message, Exception innerException)
                : base(message, innerException)
        {
            ErrorCode   = CmisErrorCode.NotDefined;
            _formatArgs = Array.Empty<object>();
            HResult     = unchecked((int) (0xB0000000 + (ushort) CmisErrorCode.NotDefined));
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="CmisException" /> class with the specified error code.
        /// </summary>
        /// <param name="errorCode">The error code.</param>
        /// <param name="formatArgs">Optional format arguments for the error message.</param>
        public CmisException(CmisErrorCode errorCode, params object[] formatArgs)
                : base(GetFormattedMessage(errorCode, formatArgs))
        {
            ErrorCode   = errorCode;
            _formatArgs = formatArgs ?? Array.Empty<object>();
            HResult     = unchecked((int) (0xB0000000 + (ushort) errorCode));
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="CmisException" /> class with the specified error code and inner
        ///     exception.
        /// </summary>
        /// <param name="errorCode">The error code.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        /// <param name="formatArgs">Optional format arguments for the error message.</param>
        public CmisException(CmisErrorCode errorCode, Exception innerException, params object[] formatArgs)
                : base(GetFormattedMessage(errorCode, formatArgs), innerException)
        {
            ErrorCode   = errorCode;
            _formatArgs = formatArgs ?? Array.Empty<object>();
            HResult     = unchecked((int) (0xB0000000 + (ushort) errorCode));
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="CmisException" /> class with serialized data.
        /// </summary>
        /// <param name="info">The serialization info.</param>
        /// <param name="context">The streaming context.</param>
        [Obsolete("This API supports obsolete formatter-based serialization")]
        protected CmisException(SerializationInfo info, StreamingContext context)
                : base(info, context)
        {
            ErrorCode   = (CmisErrorCode) info.GetValue(nameof(ErrorCode), typeof(CmisErrorCode))!;
            _formatArgs = (object[]) info.GetValue(nameof(FormatArgs), typeof(object[]))!;
        }

        /// <summary>
        ///     Gets the error code associated with this exception.
        /// </summary>
        public CmisErrorCode ErrorCode { get; }

        /// <summary>
        ///     Gets the format arguments used for the error message.
        /// </summary>
        public IReadOnlyList<object> FormatArgs => _formatArgs;

        /// <summary>
        ///     Creates a new CmisException with the specified error code.
        /// </summary>
        /// <param name="code">The error code.</param>
        /// <param name="args">Optional format arguments.</param>
        /// <returns>A new CmisException instance.</returns>
        public static CmisException Create(CmisErrorCode code, params object[] args)
        {
            return new (code, args);
        }

        /// <summary>
        ///     Creates a new CmisException wrapping an inner exception.
        /// </summary>
        /// <param name="code">The error code.</param>
        /// <param name="inner">The inner exception.</param>
        /// <param name="args">Optional format arguments.</param>
        /// <returns>A new CmisException instance.</returns>
        public static CmisException Wrap(CmisErrorCode code, Exception inner, params object[] args)
        {
            return new (code, inner, args);
        }

        /// <summary>
        ///     Throws a CmisException if the condition is true.
        /// </summary>
        /// <param name="condition">The condition to check.</param>
        /// <param name="code">The error code.</param>
        /// <param name="args">Optional format arguments.</param>
        /// <exception cref="CmisException">Thrown when condition is true.</exception>
        public static void ThrowIf(bool condition, CmisErrorCode code, params object[] args)
        {
            if (condition)
                throw new CmisException(code, args);
        }

        /// <summary>
        ///     Sets the <see cref="SerializationInfo" /> with information about the exception.
        /// </summary>
        /// <param name="info">The serialization info.</param>
        /// <param name="context">The streaming context.</param>
        [Obsolete("This API supports obsolete formatter-based serialization")]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(nameof(ErrorCode),  ErrorCode);
            info.AddValue(nameof(FormatArgs), _formatArgs);
        }

        /// <summary>
        ///     Returns a detailed string representation of the exception.
        /// </summary>
        /// <returns>A string that represents the current exception.</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"CmisException: ErrorCode = {ErrorCode} ({(ushort) ErrorCode})");
            sb.AppendLine($"Message: {Message}");

            if (FormatArgs.Count > 0)
            {
                sb.AppendLine("Format Arguments:");
                for (var i = 0; i < FormatArgs.Count; i++)
                {
                    var arg = FormatArgs[i];
                    sb.AppendLine($"  [{i}] {arg?.GetType().Name ?? "null"}: {arg ?? "null"}");
                }
            }

            if (InnerException != null)
            {
                sb.AppendLine();
                sb.AppendLine("=== Inner Exception ===");
                sb.AppendLine(InnerException.ToString());
            }

            if (!string.IsNullOrEmpty(StackTrace))
            {
                sb.AppendLine();
                sb.AppendLine("Stack Trace:");
                sb.AppendLine(StackTrace);
            }

            return sb.ToString();
        }

        /// <summary>
        ///     Gets the formatted error message based on error code and format arguments.
        /// </summary>
        /// <param name="errorCode">The error code.</param>
        /// <param name="formatArgs">Optional format arguments.</param>
        /// <returns>The formatted error message.</returns>
        private static string GetFormattedMessage(CmisErrorCode errorCode, object[]? formatArgs)
        {
            var message = errorCode.GetLocalizedDescription();

            if (string.IsNullOrEmpty(message))
                return $"Error occurred: {errorCode} (Code: {(ushort) errorCode})";

            if (formatArgs is null || formatArgs.Length == 0)
                return message;

            try
            {
                return string.Format(CultureInfo.InvariantCulture, message, formatArgs);
            }
            catch (FormatException ex)
            {
                // When formatting fails, return original message with args details
                var argsStr = string.Join(", ", formatArgs.Select(a => a?.ToString() ?? "null"));
                return $"{message} [Format Error: {ex.Message}. Args: {argsStr}]";
            }
        }
    }
}