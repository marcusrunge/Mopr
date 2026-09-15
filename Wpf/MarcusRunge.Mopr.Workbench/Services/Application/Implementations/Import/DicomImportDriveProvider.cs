using MarcusRunge.Base;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Import;
using System.IO;

namespace MarcusRunge.Mopr.Workbench.Services.Application.Implementations.Import
{
    /// <summary>
    /// Provides import-relevant drive information from the operating system.
    /// </summary>
    internal sealed class DicomImportDriveProvider : CreateableBindableBase<IDicomImportDriveProvider, DicomImportDriveProvider, IImportServiceBase>, IDicomImportDriveProvider
    {
        /// <inheritdoc/>
        public IReadOnlyList<DicomImportDriveInfo> GetDrives()
        {
            var drives = DriveInfo.GetDrives();
            var result = new List<DicomImportDriveInfo>(drives.Length);

            foreach (var drive in drives)
            {
                // Drive readiness is queried separately because removable and optical
                // drives may exist without currently containing an accessible medium.
                var isReady = false;

                try
                {
                    isReady = drive.IsReady;
                }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
                {
                    isReady = false;
                }

                result.Add(new DicomImportDriveInfo(drive.Name, drive.DriveType, isReady));
            }

            return result;
        }

        /// <inheritdoc/>
        protected override void OnCreate(IImportServiceBase @base)
        {
        }

        /// <inheritdoc/>
        protected override Task OnCreateAsync(IImportServiceBase @base, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}