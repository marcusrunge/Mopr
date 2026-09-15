using MarcusRunge.Mopr.Workbench.Contracts.Application.Import.Models;

namespace MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Import
{
    /// <summary>
    /// Resolves and validates the effective source of a DICOM import operation.
    /// </summary>
    internal interface IDicomImportSourceResolver
    {
        /// <summary>
        /// Resolves the selected import source without modifying, mounting or connecting it.
        /// </summary>
        /// <param name="request">The application import request.</param>
        /// <param name="cancellationToken">Cancels the resolution operation.</param>
        /// <returns>The structured source resolution.</returns>
        Task<DicomImportSourceResolution> ResolveAsync(DicomImportRequest request, CancellationToken cancellationToken = default);
    }
}