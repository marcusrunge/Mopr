using System;
using System.IO;

namespace MarcusRunge.Mopr.Workbench.Application.Configuration
{
    /// <summary>
    /// Provides configuration paths below the shared Windows program-data directory.
    /// </summary>
    internal sealed class MachineConfigurationPathProvider : IMachineConfigurationPathProvider
    {
        private const string ApplicationDirectoryName = "MOPR";
        private const string ConfigurationDirectoryName = "Configuration";
        private const string ConfigurationFileName = "application.json";

        public MachineConfigurationPathProvider() : this(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData))
        {
        }

        internal MachineConfigurationPathProvider(string programDataPath)
        {
            if (string.IsNullOrWhiteSpace(programDataPath))
            {
                throw new ArgumentException("The program-data path must not be empty.", nameof(programDataPath));
            }

            ConfigurationDirectoryPath = Path.Combine(programDataPath, ApplicationDirectoryName, ConfigurationDirectoryName);
            ConfigurationFilePath = Path.Combine(ConfigurationDirectoryPath, ConfigurationFileName);
        }

        /// <inheritdoc/>
        public string ConfigurationDirectoryPath { get; }

        /// <inheritdoc/>
        public string ConfigurationFilePath { get; }
    }
}