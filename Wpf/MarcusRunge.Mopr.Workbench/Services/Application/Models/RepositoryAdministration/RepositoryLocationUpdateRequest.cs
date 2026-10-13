namespace MarcusRunge.Mopr.Workbench.Services.Application.Models.RepositoryAdministration;
/// <summary>Describes an administrative update of one persisted repository location.</summary>
public sealed record RepositoryLocationUpdateRequest(int Id, string Name, string RootPath, bool IsEnabled, bool IsDefault, byte[] RowVersion);
