using MarcusRunge.Mopr.Workbench.Contracts.Application;

namespace MarcusRunge.Mopr.Workbench.Application
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