using OpenCMIS.Protocol.Abstractions;
using OpenCMIS.Shared;

namespace OpenCMIS.Protocol.Core
{
    /// <summary>
    ///     Processes CMIS protocol commands by routing them to appropriate handlers.
    /// </summary>
    public class CommandProcessor
    {
        private readonly IRegisterAccess _registerAccess;

        /// <summary>
        ///     Initializes a new instance of the CommandProcessor class.
        /// </summary>
        /// <param name="registerAccess">The register access interface.</param>
        public CommandProcessor(IRegisterAccess registerAccess)
        {
            _registerAccess = registerAccess ?? throw new CmisException(CmisErrorCode.InvalidParameterValue, nameof(registerAccess));
        }

        /// <summary>
        ///     Processes a CMIS command.
        /// </summary>
        /// <param name="command">The command to process.</param>
        /// <returns>The command execution result.</returns>
        public async Task<CommandResult> ProcessCommandAsync(CmisCommand command)
        {
            if (command == null)
                throw new CmisException(CmisErrorCode.InvalidParameterValue, nameof(command));

            try
            {
                return command.Type switch
                       {
                           CommandType.StateControl => await HandleStateControlAsync(command),
                           CommandType.Initialize   => await HandleInitializeAsync(command),
                           _                        => throw new CmisException(CmisErrorCode.InvalidCommandType, command.Type)
                       };
            }
            catch (CmisException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return new()
                       {
                           Success      = false,
                           ErrorMessage = ex.Message
                       };
            }
        }

        /// <summary>
        ///     Reads a single byte from a register.
        /// </summary>
        public async Task<byte> ReadRegisterAsync(byte page, byte address)
        {
            return await _registerAccess.ReadByteAsync(page, address);
        }

        /// <summary>
        ///     Writes a single byte to a register.
        /// </summary>
        public async Task WriteRegisterAsync(byte page, byte address, byte value)
        {
            await _registerAccess.WriteByteAsync(page, address, value);
        }

        /// <summary>
        ///     Reads a block of data from registers.
        /// </summary>
        public async Task<byte[]> ReadBlockAsync(byte page, byte startAddress, int length)
        {
            return await _registerAccess.ReadBlockAsync(page, startAddress, length);
        }

        private async Task<CommandResult> HandleStateControlAsync(CmisCommand command)
        {
            var targetState = command.Parameters.TryGetValue("State", out var stateObj)
                                      ? (ModuleState) Convert.ToInt32(stateObj)
                                      : ModuleState.Ready;

            var stateValue = (byte) targetState;
            await _registerAccess.WriteByteAsync(0x00, CmisConstants.RegModuleState, stateValue);

            // Read back to confirm
            var currentState = await _registerAccess.ReadByteAsync(0x00, CmisConstants.RegModuleState);

            return new()
                   {
                       Success = (ModuleState) currentState == targetState,
                       Data    = new[] {(ModuleState) currentState}
                   };
        }

        private async Task<CommandResult> HandleInitializeAsync(CmisCommand command)
        {
            // CMIS initialization sequence:
            // 1. Verify module is in LowPwr state
            var currentState = await _registerAccess.ReadByteAsync(0x00, CmisConstants.RegModuleState);
            if ((ModuleState) currentState == ModuleState.Fault)
            {
                return new()
                       {
                           Success      = false,
                           ErrorMessage = "Module is in Fault state, cannot initialize"
                       };
            }

            // 2. Transition through state machine: LowPwr -> PwrUp -> Ready
            if ((ModuleState) currentState == ModuleState.LowPwr)
            {
                await _registerAccess.WriteByteAsync(0x00, CmisConstants.RegModuleState, (byte) ModuleState.PwrUp);
                await Task.Delay(100);
            }

            var pwrUpState = await _registerAccess.ReadByteAsync(0x00, CmisConstants.RegModuleState);
            if ((ModuleState) pwrUpState == ModuleState.PwrUp)
            {
                await _registerAccess.WriteByteAsync(0x00, CmisConstants.RegModuleState, (byte) ModuleState.Ready);
                await Task.Delay(100);
            }

            var finalState = await _registerAccess.ReadByteAsync(0x00, CmisConstants.RegModuleState);

            return new()
                   {
                       Success = (ModuleState) finalState == ModuleState.Ready,
                       Data    = new[] {(ModuleState) finalState}
                   };
        }
    }
}
