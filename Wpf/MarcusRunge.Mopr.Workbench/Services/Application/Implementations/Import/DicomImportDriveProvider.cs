using MarcusRunge.Base;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Import;
using MarcusRunge.Mopr.Workbench.Services.Application.Models.Import;
using System.IO;
using System.Runtime.CompilerServices;

namespace MarcusRunge.Mopr.Workbench.Services.Application.Implementations.Import
{
    /// <summary>
    /// Provides import-relevant drive information from the operating system.
    /// </summary>
    internal sealed class DicomImportDriveProvider : CreateableBindableBase<IDicomImportDriveProvider, DicomImportDriveProvider, IImportServiceBase>, IDicomImportDriveProvider
    {
        /// <inheritdoc/>
        public async IAsyncEnumerable<DicomImportDriveInfo> GetDrivesAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var drives = await Task.Run(DriveInfo.GetDrives, cancellationToken).ConfigureAwait(false);

            foreach (var drive in drives.OrderBy(drive => drive.Name, StringComparer.OrdinalIgnoreCase))
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Each drive is inspected independently on a worker thread. This prevents inaccessible network,
                // optical or removable drives from blocking the WPF dispatcher and allows completed results to
                // reach the UI immediately.
                var driveInfo = await Task.Run(() => CreateDriveInfo(drive), cancellationToken).ConfigureAwait(false);

                cancellationToken.ThrowIfCancellationRequested();
                yield return driveInfo;
            }
        }

        /// <inheritdoc/>
        protected override void OnCreate(IImportServiceBase @base)
        {
        }

        /// <inheritdoc/>
        protected override Task OnCreateAsync(IImportServiceBase @base, CancellationToken cancellationToken) => Task.CompletedTask;

        private static DicomImportDriveInfo CreateDriveInfo(DriveInfo drive)
        {
            var isReady = false;

            try
            {
                isReady = drive.IsReady;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                // A physically present drive remains visible even when Windows cannot currently access its medium.
                isReady = false;
            }

            return new DicomImportDriveInfo(drive.Name, drive.DriveType, isReady);
        }
    }
}