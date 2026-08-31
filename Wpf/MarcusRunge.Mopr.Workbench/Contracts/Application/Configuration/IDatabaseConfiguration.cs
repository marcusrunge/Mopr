namespace MarcusRunge.Mopr.Workbench.Contracts.Application.Configuration
{
    /// <summary>
    /// Defines the machine-wide database configuration used by MOPR.
    /// </summary>
    public interface IDatabaseConfiguration
    {
        /// <summary>
        /// Gets the SQL Server connection string.
        /// </summary>
        string ConnectionString { get; }
    }
}