namespace MarcusRunge.Mopr.Workbench.Application.Configuration
{
    /// <summary>
    /// Protects machine-wide MOPR configuration data and storage paths.
    /// </summary>
    internal interface IMachineConfigurationProtectionService
    {
        /// <summary>
        /// Protects configuration data for the current Windows machine.
        /// </summary>
        /// <param name="unprotectedData">The configuration data to protect.</param>
        /// <returns>The protected configuration data.</returns>
        byte[] ProtectData(byte[] unprotectedData);

        /// <summary>
        /// Applies the required access-control rules to the configuration directory.
        /// </summary>
        /// <param name="directoryPath">The configuration directory path.</param>
        void ProtectDirectory(string directoryPath);

        /// <summary>
        /// Applies the required access-control rules to the configuration file.
        /// </summary>
        /// <param name="filePath">The configuration file path.</param>
        void ProtectFile(string filePath);

        /// <summary>
        /// Unprotects configuration data for the current Windows machine.
        /// </summary>
        /// <param name="protectedData">The protected configuration data.</param>
        /// <returns>The unprotected configuration data.</returns>
        byte[] UnprotectData(byte[] protectedData);
    }
}