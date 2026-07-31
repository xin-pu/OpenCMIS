namespace OpenCMIS.Shared
{
    /// <summary>
    ///     Defines error codes for the CMIS system.
    ///     Error codes are organized by functional area for easier maintenance and categorization.
    /// </summary>
    public enum CmisErrorCode : ushort
    {
        #region System Core Errors - 0-99

        /// <summary>
        ///     Error type not defined.
        /// </summary>
        [Info("Error type not defined", "未定义错误")]
        NotDefined = 0,

        /// <summary>
        ///     An unhandled system exception occurred.
        /// </summary>
        [Info("Unhandled system exception", "未处理的系统异常")]
        UnhandledSystemException = 10,

        /// <summary>
        ///     Failed to initialize system resources.
        /// </summary>
        [Info("Resource initialization failure", "资源初始化失败")]
        ResourceInitializationFailed = 20,

        #endregion

        #region Device Connection Errors - 100-199

        /// <summary>
        ///     Device is not connected.
        /// </summary>
        [Info("Device is not connected", "设备未连接")]
        DeviceNotConnected = 100,

        /// <summary>
        ///     Device connection failed.
        /// </summary>
        [Info("Device connection failed", "设备连接失败")]
        DeviceConnectionFailed = 110,

        /// <summary>
        ///     Device disconnection failed.
        /// </summary>
        [Info("Device disconnection failed", "设备断开失败")]
        DeviceDisconnectionFailed = 120,

        /// <summary>
        ///     Device operation timed out.
        /// </summary>
        [Info("Device operation timed out", "设备操作超时")]
        DeviceTimeout = 130,

        /// <summary>
        ///     Device communication error.
        /// </summary>
        [Info("Device communication error", "设备通信错误")]
        DeviceCommunicationError = 140,

        /// <summary>
        ///     Device not found.
        /// </summary>
        [Info("Device not found", "设备未找到")]
        DeviceNotFound = 150,

        #endregion

        #region Protocol Errors - 200-299

        /// <summary>
        ///     Invalid register address or page.
        /// </summary>
        [Info("Invalid register address or page", "无效的寄存器地址或页面")]
        InvalidRegister = 200,

        /// <summary>
        ///     CMIS protocol violation detected.
        /// </summary>
        [Info("CMIS protocol violation detected", "检测到CMIS协议违规")]
        ProtocolViolation = 210,

        /// <summary>
        ///     Invalid page address range.
        /// </summary>
        [Info("Invalid page address range", "无效的页面地址范围")]
        InvalidPageAddress = 220,

        /// <summary>
        ///     Register read operation failed.
        /// </summary>
        [Info("Register read operation failed", "寄存器读取操作失败")]
        RegisterReadFailed = 230,

        /// <summary>
        ///     Register write operation failed.
        /// </summary>
        [Info("Register write operation failed", "寄存器写入操作失败")]
        RegisterWriteFailed = 240,

        /// <summary>
        ///     Invalid command type.
        /// </summary>
        [Info("Invalid command type", "无效的命令类型")]
        InvalidCommandType = 250,

        /// <summary>
        ///     Command execution failed.
        /// </summary>
        [Info("Command execution failed", "命令执行失败")]
        CommandExecutionFailed = 260,

        #endregion

        #region CDB Errors - 300-399

        /// <summary>
        ///     CDB validation failed.
        /// </summary>
        [Info("CDB validation failed", "CDB验证失败")]
        CdbValidationFailed = 300,

        /// <summary>
        ///     CDB version mismatch.
        /// </summary>
        [Info("CDB version mismatch", "CDB版本不匹配")]
        CdbVersionMismatch = 310,

        /// <summary>
        ///     CDB read operation failed.
        /// </summary>
        [Info("CDB read operation failed", "CDB读取操作失败")]
        CdbReadFailed = 320,

        /// <summary>
        ///     CDB write operation failed.
        /// </summary>
        [Info("CDB write operation failed", "CDB写入操作失败")]
        CdbWriteFailed = 330,

        /// <summary>
        ///     CDB format error.
        /// </summary>
        [Info("CDB format error", "CDB格式错误")]
        CdbFormatError = 340,

        /// <summary>
        ///     CDB checksum error.
        /// </summary>
        [Info("CDB checksum error", "CDB校验和错误")]
        CdbChecksumError = 350,

        #endregion

        #region Module State Errors - 400-499

        /// <summary>
        ///     Invalid module state transition.
        /// </summary>
        [Info("Invalid module state transition", "无效的模块状态转换")]
        InvalidStateTransition = 400,

        /// <summary>
        ///     Module state machine error.
        /// </summary>
        [Info("Module state machine error", "模块状态机错误")]
        ModuleStateMachineError = 410,

        /// <summary>
        ///     Module initialization failed.
        /// </summary>
        [Info("Module initialization failed", "模块初始化失败")]
        ModuleInitializationFailed = 420,

        /// <summary>
        ///     Module power control failed.
        /// </summary>
        [Info("Module power control failed", "模块电源控制失败")]
        ModulePowerControlFailed = 430,

        #endregion

        #region Data Validation Errors - 500-599

        /// <summary>
        ///     Invalid parameter value.
        /// </summary>
        [Info("Invalid parameter value", "参数值无效")]
        InvalidParameterValue = 500,

        /// <summary>
        ///     Parameter out of range.
        /// </summary>
        [Info("Parameter out of range", "参数超出范围")]
        ParameterOutOfRange = 510,

        /// <summary>
        ///     Invalid data format.
        /// </summary>
        [Info("Invalid data format", "数据格式无效")]
        InvalidDataFormat = 520,

        /// <summary>
        ///     Data size mismatch.
        /// </summary>
        [Info("Data size mismatch", "数据大小不匹配")]
        DataSizeMismatch = 530,

        #endregion

        #region I2C Transport Errors - 600-699

        [Info("I2C adapter not found", "I2C adapter not found")]
        I2cAdapterNotFound = 600,

        [Info("I2C connection failed", "I2C connection failed")]
        I2cConnectionFailed = 610,

        [Info("I2C transfer failed", "I2C transfer failed")]
        I2cTransferFailed = 620,

        [Info("Invalid I2C response", "Invalid I2C response")]
        I2cInvalidResponse = 630,

        [Info("MSA page selection failed", "MSA page selection failed")]
        MsaPageSelectionFailed = 640,

        #endregion

        #region Unclassified Errors - 9990-9999

        /// <summary>
        ///     Generic unhandled exception wrapper.
        /// </summary>
        [Info("Unhandled exception occurred", "发生未处理的异常")]
        UnhandledExceptionWrapper = 9990,

        #endregion
    }
}
