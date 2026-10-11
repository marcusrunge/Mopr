namespace MarcusRunge.Mopr.Workbench.Contracts.Application.RepositoryAdministration.Models;

/// <summary>
/// Describes a new persisted repository location.
/// </summary>
public sealed record RepositoryLocationCreateRequest(string Name, string RootPath, bool IsEnabled, bool IsDefault);
