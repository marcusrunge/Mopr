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
        private IAuditIdentityProvider AuditIdentityProvider => Base.ApplicationBase?.AuditIdentityProvider ?? throw new ArgumentNullException(nameof(IAuditIdentityProvider));
        private IImportServiceBase Base => _base ?? throw new InvalidOperationException("Service has not been initialized.");
        private IPersistence Persistence => Base.ApplicationBase?.Persistence ?? throw new ArgumentNullException(nameof(IPersistence));
        private RepositoryContract Repository => Base.ApplicationBase?.Repository ?? throw new ArgumentNullException(nameof(RepositoryContract));

        /// <inheritdoc/>
        public async Task<DicomImportResult> ImportDirectoryAsync(DicomImportRequest request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (string.IsNullOrWhiteSpace(request.SourceDirectoryPath))
                {
                    return DicomImportResult.WithoutImport(DicomImportStatus.SourceMissing);
                }

                if (!Directory.Exists(request.SourceDirectoryPath))
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

                // This service supplies validated coordination data only. File discovery,
                // DICOM validation, copying, persistence and compensation remain exclusively
                // inside the existing atomic repository importer.
                var repositoryRequest = new RepositoryDicomImportRequest
                {
                    AllowOverwrite = request.AllowOverwrite,
                    CreatedByUserId = auditUserId.Value,
                    RepositoryLocationId = repositoryLocation.Id,
                    SourcePath = request.SourceDirectoryPath,
                    SourceType = ImportSourceType.Directory
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

        protected override void OnCreate(IImportServiceBase @base) => _base = @base ?? throw new ArgumentNullException(nameof(@base));

        protected override Task OnCreateAsync(IImportServiceBase @base, CancellationToken cancellationToken)
        {
            _base = @base;
            return Task.CompletedTask;
        }

        private static bool IsRepositoryAvailable(RepositoryLocation repositoryLocation) => repositoryLocation.Id > 0 && repositoryLocation.IsEnabled && !string.IsNullOrWhiteSpace(repositoryLocation.RootPath) && Directory.Exists(repositoryLocation.RootPath);

        private static DicomImportResult MapResult(RepositoryDicomImportResult result)
        {
            ArgumentNullException.ThrowIfNull(result);
            var status = result.FailedFiles > 0 || result.Errors.Count > 0 ? DicomImportStatus.CompletedWithErrors : result.SkippedFiles > 0 ? DicomImportStatus.CompletedWithSkippedFiles : DicomImportStatus.Completed;
            return new DicomImportResult(status, result.DiscoveredFiles, result.ValidDicomFiles, result.ImportableFiles, result.ImportedFiles, result.SkippedFiles, result.FailedFiles, result.Errors);
        }
    }
}