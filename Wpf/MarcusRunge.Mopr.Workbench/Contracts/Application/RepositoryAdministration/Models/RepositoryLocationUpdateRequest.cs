namespace MarcusRunge.Mopr.Workbench.Contracts.Application.RepositoryAdministration.Models;

/// <summary>
/// Describes an administrative update of one persisted repository location.
/// </summary>
public sealed record RepositoryLocationUpdateRequest(int Id, string Name, string RootPath, bool IsEnabled, bool IsDefault, byte[] RowVersion);
