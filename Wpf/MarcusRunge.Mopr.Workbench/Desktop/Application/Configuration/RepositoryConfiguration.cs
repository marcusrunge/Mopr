using MarcusRunge.Mopr.Workbench.Contracts.Application.Configuration;

namespace MarcusRunge.Mopr.Workbench.Application.Configuration
{
    /// <summary>
    /// Provides application-wide repository behavior settings.
    /// </summary>
    public sealed class RepositoryConfiguration : IRepositoryConfiguration
    {
        /// <inheritdoc/>
        public bool AutomaticallyRepairPaths { get; set; } = true;

        /// <inheritdoc/>
        public bool VerifyRepositoryOnStartup { get; set; } = true;
    }
}