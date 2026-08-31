namespace MarcusRunge.Mopr.Workbench.Application.Configuration
{
    /// <summary>
    /// Provides the machine-wide MOPR configuration paths.
    /// </summary>
    internal interface IMachineConfigurationPathProvider
    {
        /// <summary>
        /// Gets the directory containing the machine-wide configuration.
        /// </summary>
        string ConfigurationDirectoryPath { get; }

        /// <summary>
        /// Gets the machine-wide application configuration file path.
        /// </summary>
        string ConfigurationFilePath { get; }
    }
}