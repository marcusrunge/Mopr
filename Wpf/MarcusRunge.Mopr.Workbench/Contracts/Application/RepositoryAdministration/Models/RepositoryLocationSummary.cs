namespace MarcusRunge.Mopr.Workbench.Contracts.Application.RepositoryAdministration.Models
{
    /// <summary>
    /// Describes one persisted DICOM repository location for administrative display.
    /// </summary>
    public sealed record RepositoryLocationSummary(int Id, string Name, string RootPath, bool IsEnabled, bool IsDefault);
}
