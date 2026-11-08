namespace MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Studies
{
    /// <summary>
    /// Defines the application services for imported DICOM studies and series.
    /// </summary>
    public interface IStudiesService
    {
        /// <summary>
        /// Gets the service that resolves imported series to validated repository files.
        /// </summary>
        IImportedSeriesLoadService? ImportedSeriesLoader { get; }

        /// <summary>
        /// Gets the read-only imported-study catalog service.
        /// </summary>
        IStudyCatalogService? StudyCatalog { get; }
    }
}