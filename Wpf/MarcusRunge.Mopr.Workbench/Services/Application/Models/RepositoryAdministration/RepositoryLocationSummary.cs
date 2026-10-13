namespace MarcusRunge.Mopr.Workbench.Services.Application.Models.RepositoryAdministration;
/// <summary>Describes one persisted DICOM repository location for administrative display and editing.</summary>
public sealed record RepositoryLocationSummary(int Id, string Name, string RootPath, bool IsEnabled, bool IsDefault, byte[] RowVersion);
