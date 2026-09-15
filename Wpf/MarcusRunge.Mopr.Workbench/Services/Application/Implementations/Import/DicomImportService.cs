using MarcusRunge.Base;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Import.Models;
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

        /// <inheritdoc/>
        public async Task<DicomImportResult> ImportAsync(DicomImportRequest request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (string.IsNullOrWhiteSpace(request.SourcePath))
                {
                    return DicomImportResult.WithoutImport(DicomImportStatus.SourceMissing);
                }

                var repositorySourceType = MapSourceType(request.SourceType);
                if (repositorySourceType is null)
                {
                    return DicomImportResult.WithoutImport(DicomImportStatus.SourceTypeUnsupported);
                }

                if (!Directory.Exists(request.SourcePath))
                {
                    return DicomImportResult.WithoutImport(DicomImportStatus.SourceUnavailable);
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

                // The application adapter validates and classifies the selected source.
                // Atomic file handling, DICOM validation, persistence and compensation
                // remain exclusively inside the existing repository importer.
                var repositoryRequest = new RepositoryDicomImportRequest
                {
                    AllowOverwrite = request.AllowOverwrite,
                    CreatedByUserId = auditUserId.Value,
                    RepositoryLocationId = repositoryLocation.Id,
                    SourcePath = request.SourcePath,
                    SourceType = repositorySourceType.Value
                };

                var repositoryResult = await repositoryImporter.ImportAsync(repositoryRequest, cancellationToken).ConfigureAwait(false);
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
        public Task<DicomImportResult> ImportDirectoryAsync(DicomImportRequest request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            var directoryRequest = new DicomImportRequest(request.SourcePath, DicomImportSourceType.LocalDirectory, request.AllowOverwrite);
            return ImportAsync(directoryRequest, cancellationToken);
        }

        /// <inheritdoc/>
        protected override void OnCreate(IImportServiceBase @base) => _base = @base ?? throw new ArgumentNullException(nameof(@base));

        /// <inheritdoc/>
        protected override Task OnCreateAsync(IImportServiceBase @base, CancellationToken cancellationToken) => Task.CompletedTask;

        private static bool IsRepositoryAvailable(RepositoryLocation repositoryLocation) => repositoryLocation.Id > 0 && repositoryLocation.IsEnabled && !string.IsNullOrWhiteSpace(repositoryLocation.RootPath) && Directory.Exists(repositoryLocation.RootPath);

        private static ImportSourceType? MapSourceType(DicomImportSourceType sourceType) => sourceType switch
        {
            DicomImportSourceType.AutoDetect => ImportSourceType.Directory,
            DicomImportSourceType.LocalDirectory => ImportSourceType.Directory,
            _ => null
        };

        private static DicomImportResult MapResult(RepositoryDicomImportResult result)
        {
            ArgumentNullException.ThrowIfNull(result);
            var status = result.FailedFiles > 0 || result.Errors.Count > 0 ? DicomImportStatus.CompletedWithErrors : result.SkippedFiles > 0 ? DicomImportStatus.CompletedWithSkippedFiles : DicomImportStatus.Completed;
            return new DicomImportResult(status, result.DiscoveredFiles, result.ValidDicomFiles, result.ImportableFiles, result.ImportedFiles, result.SkippedFiles, result.FailedFiles, result.Errors);
        }
    }
}