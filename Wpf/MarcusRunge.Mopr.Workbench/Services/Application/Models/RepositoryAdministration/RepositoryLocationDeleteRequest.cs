namespace MarcusRunge.Mopr.Workbench.Services.Application.Models.RepositoryAdministration;
/// <summary>Describes the deletion of one persisted repository-location configuration.</summary>
public sealed record RepositoryLocationDeleteRequest(int Id, byte[] RowVersion);
