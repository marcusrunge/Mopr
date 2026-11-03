using MarcusRunge.Base;
using MarcusRunge.Mopr.Workbench.Services.Application.Models.Import;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Security.Services;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Import;
using MarcusRunge.Mopr.Workbench.Services.Application.Enums;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Entities;
using MarcusRunge.Mopr.Workbench.Services.Repository.Enums;
using System.IO;
using RepositoryContract = MarcusRunge.Mopr.Workbench.Services.Repository.Contracts.IRepository;
using RepositoryDicomImportRequest = MarcusRunge.Mopr.Workbench.Services.Repository.Models.DicomImportRequest;
using RepositoryDicomImportResult = MarcusRunge.Mopr.Workbench.Services.Repository.Models.DicomImportResult;
using RepositoryDicomImportProgress = MarcusRunge.Mopr.Workbench.Services.Repository.Models.DicomImportProgress;

namespace MarcusRunge.Mopr.Workbench.Services.Application.Implementations.Import
{
    /// <summary>
    /// Coordinates application prerequisites and delegates DICOM processing to the existing atomic repository importer.
    /// </summary>
    internal sealed class DicomImportService() : CreateableBindableBase<IDicomImportService, DicomImportService, IImportServiceBase>, IDicomImportService
    {
        private IImportServiceBase? _base;

        private IAuditIdentityProvider AuditIdentityProvider => Base.ApplicationBase?.AuditIdentityProvider ?? throw new InvalidOperationException("The audit identity provider has not been initialized.");

        private IImportServiceBase Base => _base ?? throw new InvalidOperationException("The DICOM import service has not been initialized.");

        private IPersistence Persistence => Base.ApplicationBase?.Persistence ?? throw new InvalidOperationException("Persistence has not been initialized.");

        private RepositoryContract Repository => Base.ApplicationBase?.Repository ?? throw new InvalidOperationException("The repository service has not been initialized.");

        private IDicomImportSourceResolver SourceResolver => Base.DicomImportSourceResolver ?? throw new InvalidOperationException("The DICOM import source resolver has not been initialized.");

        /// <inheritdoc/>
        public Task<DicomImportResult> ImportAsync(DicomImportRequest request, CancellationToken cancellationToken = default) => ImportAsync(request, null, cancellationToken);

        /// <inheritdoc/>
        public async Task<DicomImportResult> ImportAsync(DicomImportRequest request, IProgress<DicomImportProgress>? progress, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (string.IsNullOrWhiteSpace(request.SourcePath))
                {
                    return DicomImportResult.WithoutImport(DicomImportStatus.SourceMissing);
                }

                var source = await SourceResolver.ResolveAsync(request, cancellationToken).ConfigureAwait(false);
                if (!source.Exists || !source.IsReady)
                {
                    return DicomImportResult.WithoutImport(DicomImportStatus.SourceUnavailable);
                }

                if (!source.IsRepositoryImportSupported || !TryMapSourceType(source.SourceType, out var repositorySourceType))
                {
                    return DicomImportResult.WithoutImport(DicomImportStatus.SourceTypeUnsupported);
                }

                var repositoryLocationRepository = Persistence.RepositoryLocation;
                if (repositoryLocationRepository is null)
                {
                    return DicomImportResult.WithoutImport(DicomImportStatus.RepositoryUnavailable);
                }

                var repositoryLocation = await repositoryLocationRepository.GetDefaultAsync(cancellationToken).ConfigureAwait(false);
                if (repositoryLocation is null)
                {
                    return DicomImportResult.WithoutImport(DicomImportStatus.DefaultRepositoryMissing);
                }

                if (!IsRepositoryAvailable(repositoryLocation))
                {
                    return DicomImportResult.WithoutImport(DicomImportStatus.RepositoryUnavailable);
                }

                var auditUserId = await AuditIdentityProvider.GetCurrentUserIdAsync(cancellationToken).ConfigureAwait(false);
                if (auditUserId is null or <= 0)
                {
                    return DicomImportResult.WithoutImport(DicomImportStatus.AuditIdentityUnavailable);
                }

                var repositoryImporter = Repository.ImportService;
                if (repositoryImporter is null)
                {
                    return DicomImportResult.WithoutImport(DicomImportStatus.RepositoryUnavailable);
                }

                // The resolver supplies the effective readable path and preserves the
                // source origin. Atomic file handling, persistence and compensation
                // remain exclusively inside the repository importer.
                var repositoryRequest = new RepositoryDicomImportRequest
                {
                    AllowOverwrite = request.AllowOverwrite,
                    CreatedByUserId = auditUserId.Value,
                    RepositoryLocationId = repositoryLocation.Id,
                    SourcePath = source.EffectiveSourcePath,
                    SourceType = repositorySourceType
                };

                IProgress<RepositoryDicomImportProgress>? repositoryProgress = progress is null ? null : new Progress<RepositoryDicomImportProgress>(value => progress.Report(new DicomImportProgress(value.TotalFiles, value.ProcessedFiles, value.ImportedFiles, value.ExistingFiles, value.SkippedFiles, value.FailedFiles)));
                /*
                 * Repository import combines asynchronous operations with synchronous DICOM and
                 * file-system work. The explicit worker boundary prevents any implementation
                 * detail below this application use case from blocking the WPF dispatcher.
                 */
                var repositoryResult = await repositoryImporter.ImportAsync(repositoryRequest, repositoryProgress, cancellationToken).ConfigureAwait(false);
                return MapResult(repositoryResult);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return DicomImportResult.WithoutImport(DicomImportStatus.Canceled);
            }
            catch (Exception exception)
            {
                return DicomImportResult.Failed(exception);
            }
        }

        /// <inheritdoc/>
        protected override void OnCreate(IImportServiceBase @base) => _base = @base ?? throw new ArgumentNullException(nameof(@base));

        /// <inheritdoc/>
        protected override Task OnCreateAsync(IImportServiceBase @base, CancellationToken cancellationToken) => Task.CompletedTask;

        private static bool IsRepositoryAvailable(RepositoryLocation repositoryLocation) => repositoryLocation.Id > 0 && repositoryLocation.IsEnabled && !string.IsNullOrWhiteSpace(repositoryLocation.RootPath) && Directory.Exists(repositoryLocation.RootPath);

        private static DicomImportResult MapResult(RepositoryDicomImportResult result)
        {
            ArgumentNullException.ThrowIfNull(result);
            var status = result.FailedFiles > 0 || result.Errors.Count > 0 ? DicomImportStatus.CompletedWithErrors : result.SkippedFiles > 0 ? DicomImportStatus.CompletedWithSkippedFiles : DicomImportStatus.Completed;
            return new DicomImportResult(status: status, discoveredFiles: result.DiscoveredFiles, validDicomFiles: result.ValidDicomFiles, importableFiles: result.ImportableFiles, importedFiles: result.ImportedFiles, existingFiles: result.ExistingFiles, skippedFiles: result.SkippedFiles, failedFiles: result.FailedFiles, technicalErrors: result.Errors);
        }

        private static bool TryMapSourceType(DicomImportSourceType sourceType, out ImportSourceType repositorySourceType)
        {
            repositorySourceType = sourceType switch
            {
                DicomImportSourceType.LocalDirectory => ImportSourceType.Directory,
                DicomImportSourceType.UsbDrive => ImportSourceType.UsbDrive,
                DicomImportSourceType.RemovableDrive => ImportSourceType.UsbDrive,
                DicomImportSourceType.ExternalDrive => ImportSourceType.Directory,
                DicomImportSourceType.SdCard => ImportSourceType.UsbDrive,
                DicomImportSourceType.CdRom => ImportSourceType.CdRom,
                DicomImportSourceType.Dvd => ImportSourceType.Dvd,
                DicomImportSourceType.NetworkShare => ImportSourceType.NetworkShare,
                DicomImportSourceType.MappedNetworkDrive => ImportSourceType.NetworkShare,
                DicomImportSourceType.VirtualDrive => ImportSourceType.Directory,
                _ => ImportSourceType.Unknown
            };

            return repositorySourceType != ImportSourceType.Unknown;
        }
    }
}