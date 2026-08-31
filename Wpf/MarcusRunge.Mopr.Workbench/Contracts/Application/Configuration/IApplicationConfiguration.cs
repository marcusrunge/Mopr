namespace MarcusRunge.Mopr.Workbench.Contracts.Application.Configuration
{
    /// <summary>
    /// Defines the machine-wide MOPR application configuration.
    /// </summary>
    public interface IApplicationConfiguration
    {
        /// <summary>
        /// Gets the database configuration.
        /// </summary>
        IDatabaseConfiguration Database { get; }

        /// <summary>
        /// Gets a value indicating whether the machine-wide setup was completed.
        /// </summary>
        bool IsSetupComplete { get; }

        /// <summary>
        /// Gets the application-wide repository behavior.
        /// </summary>
        IRepositoryConfiguration Repository { get; }

        /// <summary>
        /// Gets the machine-wide security behavior.
        /// </summary>
        ISecurityConfiguration Security { get; }

        /// <summary>
        /// Gets the version of the setup contract used to create this configuration.
        /// </summary>
        int SetupVersion { get; }
    }
}