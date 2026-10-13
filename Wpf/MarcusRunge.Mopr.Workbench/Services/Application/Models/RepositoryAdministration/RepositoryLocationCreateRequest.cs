namespace MarcusRunge.Mopr.Workbench.Services.Application.Models.RepositoryAdministration;
/// <summary>Describes a new persisted repository location.</summary>
public sealed record RepositoryLocationCreateRequest(string Name, string RootPath, bool IsEnabled, bool IsDefault);
