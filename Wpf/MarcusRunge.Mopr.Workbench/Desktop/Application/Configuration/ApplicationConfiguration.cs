using MarcusRunge.Mopr.Workbench.Contracts.Application.Configuration;

namespace MarcusRunge.Mopr.Workbench.Application.Configuration
{
    /// <summary>
    /// Represents the machine-wide MOPR application configuration.
    /// </summary>
    public sealed class ApplicationConfiguration : IApplicationConfiguration
    {
        /// <summary>
        /// Gets the current setup-contract version.
        /// </summary>
        public const int CurrentSetupVersion = 1;

        /// <inheritdoc/>
        public IDatabaseConfiguration Database => DatabaseConfiguration;

        /// <summary>
        /// Gets or sets the serializable database configuration.
        /// </summary>
        public DatabaseConfiguration DatabaseConfiguration { get; set; } = new();

        /// <inheritdoc/>
        public bool IsSetupComplete { get; set; }

        /// <inheritdoc/>
        public IRepositoryConfiguration Repository => RepositoryConfiguration;

        /// <summary>
        /// Gets or sets the serializable repository configuration.
        /// </summary>
        public RepositoryConfiguration RepositoryConfiguration { get; set; } = new();

        /// <inheritdoc/>
        public ISecurityConfiguration Security => SecurityConfiguration;

        /// <summary>
        /// Gets or sets the serializable security configuration.
        /// </summary>
        public SecurityConfiguration SecurityConfiguration { get; set; } = new();

        /// <inheritdoc/>
        public int SetupVersion { get; set; } = CurrentSetupVersion;
    }
}