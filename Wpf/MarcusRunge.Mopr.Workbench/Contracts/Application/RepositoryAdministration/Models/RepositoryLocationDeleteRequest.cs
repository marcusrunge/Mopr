namespace MarcusRunge.Mopr.Workbench.Contracts.Application.RepositoryAdministration.Models;

/// <summary>
/// Describes the deletion of one persisted repository-location configuration.
/// </summary>
public sealed record RepositoryLocationDeleteRequest(int Id, byte[] RowVersion);