using MarcusRunge.Mopr.Workbench.Application.Security;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Import.Enums;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Import.Models;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Import.Services;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Security.Services;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Entities;
using MarcusRunge.Mopr.Workbench.Services.Repository.Enums;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using RepositoryContract = MarcusRunge.Mopr.Workbench.Services.Repository.Contracts.IRepository;
using RepositoryDicomImportRequest = MarcusRunge.Mopr.Workbench.Services.Repository.Models.DicomImportRequest;
using RepositoryDicomImportResult = MarcusRunge.Mopr.Workbench.Services.Repository.Models.DicomImportResult;

namespace MarcusRunge.Mopr.Workbench.Application.Import
{
    /// <summary>
    /// Coordinates application prerequisites and delegates DICOM processing to the existing atomic repository importer.
    /// </summary>
    internal sealed class DicomImportApplicationService(IPersistence persistence, RepositoryContract repository, IAuditIdentityProvider auditIdentityProvider) : IDicomImportApplicationService
    {
        private readonly IAuditIdentityProvider _auditIdentityProvider = auditIdentityProvider ?? throw new ArgumentNullException(nameof(auditIdentityProvider));
        private readonly IPersistence _persistence = persistence ?? throw new ArgumentNullException(nameof(persistence));
        private readonly RepositoryContract _repository = repository ?? throw new ArgumentNullException(nameof(repository));

        /// <inheritdoc/>
        public async Task<DicomImportApplicationResult> ImportDirectoryAsync(DicomImportApplicationRequest request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (string.IsNullOrWhiteSpace(request.SourceDirectoryPath))
                {
                    return DicomImportApplicationResult.WithoutImport(DicomImportApplicationStatus.SourceMissing);
                }

                if (!Directory.Exists(request.SourceDirectoryPath))
                {
                    return DicomImportApplicationResult.WithoutImport(DicomImportApplicationStatus.SourceUnavailable);
                }

                var repositoryLocationRepository = _persistence.RepositoryLocation;
                if (repositoryLocationRepository is null)
                {
                    return DicomImportApplicationResult.WithoutImport(DicomImportApplicationStatus.RepositoryUnavailable);
                }

                var repositoryLocation = await repositoryLocationRepository.GetDefaultAsync(cancellationToken).ConfigureAwait(false);
                if (repositoryLocation is null)
                {
                    return DicomImportApplicationResult.WithoutImport(DicomImportApplicationStatus.DefaultRepositoryMissing);
                }

                if (!IsRepositoryAvailable(repositoryLocation))
                {
                    return DicomImportApplicationResult.WithoutImport(DicomImportApplicationStatus.RepositoryUnavailable);
                }

                var auditUserId = await _auditIdentityProvider.GetCurrentUserIdAsync(cancellationToken).ConfigureAwait(false);
                if (auditUserId is null or <= 0)
                {
                    return DicomImportApplicationResult.WithoutImport(DicomImportApplicationStatus.AuditIdentityUnavailable);
                }

                var repositoryImporter = _repository.ImportService;
                if (repositoryImporter is null)
                {
                    return DicomImportApplicationResult.WithoutImport(DicomImportApplicationStatus.RepositoryUnavailable);
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
                return DicomImportApplicationResult.WithoutImport(DicomImportApplicationStatus.Canceled);
            }
            catch (Exception exception)
            {
                return DicomImportApplicationResult.Failed(exception);
            }
        }

        private static bool IsRepositoryAvailable(RepositoryLocation repositoryLocation) => repositoryLocation.Id > 0 && repositoryLocation.IsEnabled && !string.IsNullOrWhiteSpace(repositoryLocation.RootPath) && Directory.Exists(repositoryLocation.RootPath);

        private static DicomImportApplicationResult MapResult(RepositoryDicomImportResult result)
        {
            ArgumentNullException.ThrowIfNull(result);

            var status = result.FailedFiles > 0 || result.Errors.Count > 0
                ? DicomImportApplicationStatus.CompletedWithErrors
                : result.SkippedFiles > 0
                    ? DicomImportApplicationStatus.CompletedWithSkippedFiles
                    : DicomImportApplicationStatus.Completed;

            return new DicomImportApplicationResult(status, result.DiscoveredFiles, result.ValidDicomFiles, result.ImportableFiles, result.ImportedFiles, result.SkippedFiles, result.FailedFiles, result.Errors);
        }
    }
}