using MarcusRunge.Mopr.Workbench.Contracts.Enums;

namespace MarcusRunge.Mopr.Workbench.Contracts.Models.Configuration;

/// <summary>
/// Describes the structured outcome of an administrative database-configuration update.
/// </summary>
public sealed class DatabaseConfigurationUpdateResult
{
    private DatabaseConfigurationUpdateResult(DatabaseConfigurationUpdateStatus status) => Status = status;

    /// <summary>
    /// Gets a value indicating whether the database configuration was updated successfully.
    /// </summary>
    public bool IsSuccess => Status == DatabaseConfigurationUpdateStatus.Completed;

    /// <summary>
    /// Gets the structured update status.
    /// </summary>
    public DatabaseConfigurationUpdateStatus Status { get; }

    /// <summary>
    /// Creates a result indicating that the current machine configuration could not be loaded.
    /// </summary>
    public static DatabaseConfigurationUpdateResult ConfigurationUnavailable() => new(DatabaseConfigurationUpdateStatus.ConfigurationUnavailable);

    /// <summary>
    /// Creates a generic failure result.
    /// </summary>
    public static DatabaseConfigurationUpdateResult Failed() => new(DatabaseConfigurationUpdateStatus.Failed);

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static DatabaseConfigurationUpdateResult Success() => new(DatabaseConfigurationUpdateStatus.Completed);

    /// <summary>
    /// Creates an unauthorized result.
    /// </summary>
    public static DatabaseConfigurationUpdateResult Unauthorized() => new(DatabaseConfigurationUpdateStatus.Unauthorized);

    /// <summary>
    /// Creates a validation-failure result.
    /// </summary>
    public static DatabaseConfigurationUpdateResult ValidationFailed() => new(DatabaseConfigurationUpdateStatus.ValidationFailed);
}
