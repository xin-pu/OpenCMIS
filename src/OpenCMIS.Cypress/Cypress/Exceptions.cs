namespace OpenCMIS.Cypress
{
    /// <summary>
    ///     Class of I2C access exception
    /// </summary>
    public class I2CAccessException : Exception
    {
        private const string mainMessage = "Error occurred during I2C communication";

        /// <summary>
        ///     Initializes a new instance of the <see cref="I2CAccessException" /> class.
        /// </summary>
        public I2CAccessException()
                : base(mainMessage + ".") { }

        /// <summary>
        ///     Initializes a new instance of the <see cref="I2CAccessException" /> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public I2CAccessException(string message)
                : base($"{mainMessage} - {message}") { }

        /// <summary>
        ///     Initializes a new instance of the <see cref="I2CAccessException" /> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="inner">The inner.</param>
        public I2CAccessException(string message, Exception inner)
                : base($"{mainMessage} - {message}", inner) { }
    }

    /// <summary>
    ///     Class of I2C no ACK exception
    /// </summary>
    public class I2CNoACKException : Exception
    {
        private const string mainMessage = "No ACK returned from device";

        /// <summary>
        ///     Initializes a new instance of the <see cref="I2CNoACKException" /> class.
        /// </summary>
        public I2CNoACKException()
                : base(mainMessage + ".") { }

        /// <summary>
        ///     Initializes a new instance of the <see cref="I2CNoACKException" /> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public I2CNoACKException(string message)
                : base($"{mainMessage} - {message}") { }

        /// <summary>
        ///     Initializes a new instance of the <see cref="I2CNoACKException" /> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="inner">The inner.</param>
        public I2CNoACKException(string message, Exception inner)
                : base($"{mainMessage} - {message}", inner) { }
    }

    /// <summary>
    ///     Class of I2C bit error exception
    /// </summary>
    public class I2CBitErrorException : Exception
    {
        private const string mainMessage = "Data transfer had bit errors";

        /// <summary>
        ///     Initializes a new instance of the <see cref="I2CBitErrorException" /> class.
        /// </summary>
        public I2CBitErrorException()
                : base(mainMessage + ".") { }

        /// <summary>
        ///     Initializes a new instance of the <see cref="I2CBitErrorException" /> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public I2CBitErrorException(string message)
                : base($"{mainMessage} - {message}") { }

        /// <summary>
        ///     Initializes a new instance of the <see cref="I2CBitErrorException" /> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="inner">The inner.</param>
        public I2CBitErrorException(string message, Exception inner)
                : base($"{mainMessage} - {message}", inner) { }
    }

    /// <summary>
    ///     Class of Cypress USB initialization exception
    /// </summary>
    public class CyUSBInitException : Exception
    {
        private const string mainMessage = "Error occurred during CypressUSB initialization";

        /// <summary>
        ///     Initializes a new instance of the <see cref="CyUSBInitException" /> class.
        /// </summary>
        public CyUSBInitException()
                : base(mainMessage + ".") { }

        /// <summary>
        ///     Initializes a new instance of the <see cref="CyUSBInitException" /> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public CyUSBInitException(string message)
                : base($"{mainMessage} - {message}") { }

        /// <summary>
        ///     Initializes a new instance of the <see cref="CyUSBInitException" /> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="inner">The inner.</param>
        public CyUSBInitException(string message, Exception inner)
                : base($"{mainMessage} - {message}", inner) { }
    }

    /// <summary>
    ///     Class of Cypress transfer data endpoint exception
    /// </summary>
    public class CyXferDataEndPointException : Exception
    {
        private const string mainMessage = "Error occurred during transfer of data from/to endpoints";

        /// <summary>
        ///     Initializes a new instance of the <see cref="CyXferDataEndPointException" /> class.
        /// </summary>
        public CyXferDataEndPointException()
                : base(mainMessage + ".") { }

        /// <summary>
        ///     Initializes a new instance of the <see cref="CyXferDataEndPointException" /> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public CyXferDataEndPointException(string message)
                : base($"{mainMessage} - {message}") { }

        /// <summary>
        ///     Initializes a new instance of the <see cref="CyXferDataEndPointException" /> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="inner">The inner.</param>
        public CyXferDataEndPointException(string message, Exception inner)
                : base($"{mainMessage} - {message}", inner) { }
    }

    /// <summary>
    ///     Class of Cypress packet mismatch exception
    /// </summary>
    public class CyPacketMismatchException : Exception
    {
        private const string mainMessage = "Sent & returned data packet are mismatched.";

        /// <summary>
        ///     Initializes a new instance of the <see cref="CyPacketMismatchException" /> class.
        /// </summary>
        public CyPacketMismatchException()
                : base(mainMessage + ".") { }

        /// <summary>
        ///     Initializes a new instance of the <see cref="CyPacketMismatchException" /> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public CyPacketMismatchException(string message)
                : base($"{mainMessage} - {message}") { }

        /// <summary>
        ///     Initializes a new instance of the <see cref="CyPacketMismatchException" /> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="inner">The inner.</param>
        public CyPacketMismatchException(string message, Exception inner)
                : base($"{mainMessage} - {message}", inner) { }
    }
}
