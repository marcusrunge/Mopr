namespace MarcusRunge.Mopr.Workbench.Contracts.Models.Configuration;

/// <summary>
/// Describes an administrative update of the machine-wide database configuration.
/// </summary>
/// <param name="ConnectionString">The normalized database connection string to persist.</param>
public sealed record DatabaseConfigurationUpdateRequest(string ConnectionString);
