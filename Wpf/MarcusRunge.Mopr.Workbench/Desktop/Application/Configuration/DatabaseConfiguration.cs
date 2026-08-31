using MarcusRunge.Mopr.Workbench.Contracts.Application.Configuration;

namespace MarcusRunge.Mopr.Workbench.Application.Configuration
{
    /// <summary>
    /// Represents the machine-wide SQL Server configuration used by MOPR.
    /// </summary>
    public sealed class DatabaseConfiguration : IDatabaseConfiguration
    {
        /// <inheritdoc/>
        public string ConnectionString { get; set; } = string.Empty;
    }
}