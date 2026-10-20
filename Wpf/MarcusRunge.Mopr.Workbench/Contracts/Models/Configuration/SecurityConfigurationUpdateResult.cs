using MarcusRunge.Mopr.Workbench.Contracts.Enums;

namespace MarcusRunge.Mopr.Workbench.Contracts.Models.Configuration;

/// <summary>
/// Describes the structured outcome of an administrative security-configuration update.
/// </summary>
public sealed class SecurityConfigurationUpdateResult
{
    private SecurityConfigurationUpdateResult(SecurityConfigurationUpdateStatus status) => Status = status;

    /// <summary>
    /// Gets a value indicating whether the security configuration was updated successfully.
    /// </summary>
    public bool IsSuccess => Status == SecurityConfigurationUpdateStatus.Completed;

    /// <summary>
    /// Gets the structured update status.
    /// </summary>
    public SecurityConfigurationUpdateStatus Status { get; }

    /// <summary>
    /// Creates a result indicating that the current machine configuration could not be loaded.
    /// </summary>
    public static SecurityConfigurationUpdateResult ConfigurationUnavailable() => new(SecurityConfigurationUpdateStatus.ConfigurationUnavailable);

    /// <summary>
    /// Creates a generic failure result.
    /// </summary>
    public static SecurityConfigurationUpdateResult Failed() => new(SecurityConfigurationUpdateStatus.Failed);

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static SecurityConfigurationUpdateResult Success() => new(SecurityConfigurationUpdateStatus.Completed);

    /// <summary>
    /// Creates an unauthorized result.
    /// </summary>
    public static SecurityConfigurationUpdateResult Unauthorized() => new(SecurityConfigurationUpdateStatus.Unauthorized);

    /// <summary>
    /// Creates a validation-failure result.
    /// </summary>
    public static SecurityConfigurationUpdateResult ValidationFailed() => new(SecurityConfigurationUpdateStatus.ValidationFailed);
}