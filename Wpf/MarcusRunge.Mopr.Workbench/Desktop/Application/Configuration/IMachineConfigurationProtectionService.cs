namespace MarcusRunge.Mopr.Workbench.Application.Configuration
{
    /// <summary>
    /// Protects machine-wide MOPR configuration paths with Windows access-control rules.
    /// </summary>
    internal interface IMachineConfigurationProtectionService
    {
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
    }
}