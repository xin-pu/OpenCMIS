using OpenCMIS.Protocol.Abstractions;
using OpenCMIS.Shared;
using OpenCMIS.Transport.Abstractions;

namespace OpenCMIS.Protocol.Core
{
    /// <summary>
    ///     Factory for discovering and switching CMIS Applications on a device.
    /// </summary>
    public class CmisApplicationFactory
    {
        private const byte AppSelectPage    = 0x01;
        private const byte AppSelectReg     = 0x80;
        private const byte AppSupportedReg  = 0x84;

        private readonly IRegisterAccess _registerAccess;

        // Predefined CMIS 5.2 Applications
        private static readonly Dictionary<byte, CmisApplication> KnownApplications = new()
        {
            [0x01] = new CmisApplication(0x01, "Application 1", "Standard application for 100G modules (SR4, CWDM4, LR4)"),
            [0x02] = new CmisApplication(0x02, "Application 2", "Standard application for 200G modules (FR4, LR4)"),
            [0x03] = new CmisApplication(0x03, "Application 3", "Standard application for 400G modules (FR4, LR4, SR8)"),
            [0x04] = new CmisApplication(0x04, "Application 4", "Standard application for 800G modules"),
            [0x10] = new CmisApplication(0x10, "Custom App 1", "Vendor-specific custom application 1"),
            [0x11] = new CmisApplication(0x11, "Custom App 2", "Vendor-specific custom application 2"),
            [0x12] = new CmisApplication(0x12, "Custom App 3", "Vendor-specific custom application 3"),
        };

        /// <summary>
        ///     Initializes a new instance of the CmisApplicationFactory class.
        /// </summary>
        /// <param name="registerAccess">The register access interface.</param>
        public CmisApplicationFactory(IRegisterAccess registerAccess)
        {
            _registerAccess = registerAccess ?? throw new CmisException(CmisErrorCode.InvalidParameterValue, nameof(registerAccess));
        }

        /// <summary>
        ///     Gets the currently active application on the device.
        /// </summary>
        /// <returns>The current application, or null if unknown.</returns>
        public async Task<CmisApplication?> GetCurrentApplicationAsync()
        {
            try
            {
                var appCode = await _registerAccess.ReadByteAsync(AppSelectPage, AppSelectReg);
                return KnownApplications.GetValueOrDefault(appCode);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        ///     Gets the list of applications supported by the device.
        /// </summary>
        /// <returns>A collection of supported applications.</returns>
        public async Task<IEnumerable<CmisApplication>> GetSupportedApplicationsAsync()
        {
            var supported = new List<CmisApplication>();

            try
            {
                // Read supported applications bitmask (4 bytes)
                var maskBytes = await _registerAccess.ReadBlockAsync(AppSelectPage, AppSupportedReg, 4);
                var mask = (uint)(maskBytes[0] | (maskBytes[1] << 8) | (maskBytes[2] << 16) | (maskBytes[3] << 24));

                // Check each known application against the mask
                foreach (var app in KnownApplications.Values)
                {
                    if (app.AppCode < 32 && (mask & (1U << app.AppCode)) != 0)
                        supported.Add(app);
                }

                // If no known apps found, try reading the current app at least
                if (supported.Count == 0)
                {
                    var current = await GetCurrentApplicationAsync();
                    if (current != null)
                        supported.Add(current);
                }
            }
            catch
            {
                // Device may not support application enumeration
            }

            return supported;
        }

        /// <summary>
        ///     Switches the device to the specified application.
        /// </summary>
        /// <param name="appCode">The application code to switch to.</param>
        public async Task SwitchApplicationAsync(byte appCode)
        {
            // Write the application code to the select register
            await _registerAccess.WriteByteAsync(AppSelectPage, AppSelectReg, appCode);

            // Wait for the switch to complete
            await Task.Delay(50);

            // Verify the switch was successful
            var currentApp = await _registerAccess.ReadByteAsync(AppSelectPage, AppSelectReg);
            if (currentApp != appCode)
                throw new CmisException(CmisErrorCode.CommandExecutionFailed, appCode, currentApp);
        }

        /// <summary>
        ///     Gets all known (predefined) CMIS applications.
        /// </summary>
        /// <returns>A collection of all known applications.</returns>
        public static IEnumerable<CmisApplication> GetKnownApplications()
        {
            return KnownApplications.Values;
        }
    }
}
