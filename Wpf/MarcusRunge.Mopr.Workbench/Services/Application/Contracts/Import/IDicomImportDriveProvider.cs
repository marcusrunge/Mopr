using MarcusRunge.Mopr.Workbench.Services.Application.Models.Import;
using System.Runtime.CompilerServices;

namespace MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Import
{
    /// <summary>
    /// Provides operating-system drive information required for import-source selection and classification.
    /// </summary>
    public interface IDicomImportDriveProvider
    {
        /// <summary>
        /// Enumerates drives visible to the application process without blocking the caller.
        /// </summary>
        /// <param name="cancellationToken">Stops the current enumeration.</param>
        /// <returns>
        /// An asynchronous stream of drive descriptors. A descriptor is returned as soon as the corresponding
        /// drive has been inspected.
        /// </returns>
        IAsyncEnumerable<DicomImportDriveInfo> GetDrivesAsync([EnumeratorCancellation] CancellationToken cancellationToken);
    }
}