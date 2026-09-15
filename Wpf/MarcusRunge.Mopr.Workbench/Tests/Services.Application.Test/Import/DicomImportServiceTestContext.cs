using MarcusRunge.Mopr.Workbench.Contracts.Application.Import.Models;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Security.Services;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Import;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Entities;
using Moq;
using RepositoryContract = MarcusRunge.Mopr.Workbench.Services.Repository.Contracts.IRepository;
using RepositoryImportRequest = MarcusRunge.Mopr.Workbench.Services.Repository.Models.DicomImportRequest;
using RepositoryImportResult = MarcusRunge.Mopr.Workbench.Services.Repository.Models.DicomImportResult;
using RepositoryImportService = MarcusRunge.Mopr.Workbench.Services.Repository.Contracts.IDicomImportService;

namespace MarcusRunge.Mopr.Workbench.Services.Application.Test.Import
{
    public sealed class DicomImportServiceTestContext : IDisposable
    {
        private int _auditUserId;
        private bool _disposed;
        private int _repositoryLocationId;

        public DicomImportServiceTestContext()
        {
            SourceDirectoryPath = Path.Combine(Path.GetTempPath(), $"mopr-import-source-{Guid.NewGuid():N}");
            RepositoryDirectoryPath = Path.Combine(Path.GetTempPath(), $"mopr-import-repository-{Guid.NewGuid():N}");

            EnsureDirectoryExists(SourceDirectoryPath);
            EnsureDirectoryExists(RepositoryDirectoryPath);

            // Every context owns an independent application graph. The import services
            // use the graph-owned ImportService instance as their stable scoped identity.
            Factory = new ApplicationFactory(AuditIdentityProvider.Object, Persistence.Object, Repository.Object);
            Application = Factory.Create();
            Service = Application.ImportService?.DicomImportService ?? throw new InvalidOperationException("The DICOM import service has not been initialized.");

            Reset();
        }

        public int AuditUserId => _auditUserId;

        public Mock<IAuditIdentityProvider> AuditIdentityProvider { get; } = new(MockBehavior.Strict);

        public Contracts.IApplication Application { get; }

        public ApplicationFactory Factory { get; }

        public Mock<IPersistence> Persistence { get; } = new(MockBehavior.Strict);

        public string RepositoryDirectoryPath { get; }

        public Mock<RepositoryContract> Repository { get; } = new(MockBehavior.Strict);

        public Mock<RepositoryImportService> RepositoryImportService { get; } = new(MockBehavior.Strict);

        public int RepositoryLocationId => _repositoryLocationId;

        public Mock<IRepositoryLocationRepository> RepositoryLocationRepository { get; } = new(MockBehavior.Strict);

        public IDicomImportService Service { get; }

        public string SourceDirectoryPath { get; }

        public DicomImportRequest CreateRequest(bool allowOverwrite = false) => new(SourceDirectoryPath, allowOverwrite);

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            TryDeleteDirectory(SourceDirectoryPath);
            TryDeleteDirectory(RepositoryDirectoryPath);
            GC.SuppressFinalize(this);
        }

        public void Reset(bool repositoryLocationRepositoryAvailable = true, bool repositoryImportServiceAvailable = true, int auditUserId = 41, int repositoryLocationId = 23)
        {
            _auditUserId = auditUserId;
            _repositoryLocationId = repositoryLocationId;

            AuditIdentityProvider.Reset();
            Persistence.Reset();
            Repository.Reset();
            RepositoryImportService.Reset();
            RepositoryLocationRepository.Reset();

            EnsureDirectoryExists(SourceDirectoryPath);
            EnsureDirectoryExists(RepositoryDirectoryPath);

            var defaultRepositoryLocation = new RepositoryLocation
            {
                Id = RepositoryLocationId,
                IsDefault = true,
                IsEnabled = true,
                RootPath = RepositoryDirectoryPath
            };

            RepositoryLocationRepository
                .Setup(repository => repository.GetDefaultAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(defaultRepositoryLocation);

            Persistence
                .SetupGet(persistence => persistence.RepositoryLocation)
                .Returns(repositoryLocationRepositoryAvailable ? RepositoryLocationRepository.Object : null);

            AuditIdentityProvider
                .Setup(provider => provider.GetCurrentUserIdAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(AuditUserId);

            Repository
                .SetupGet(repository => repository.ImportService)
                .Returns(repositoryImportServiceAvailable ? RepositoryImportService.Object : null);

            SetImportResult(new RepositoryImportResult());
        }

        public void SetAuditUserId(int? auditUserId) => AuditIdentityProvider
            .Setup(provider => provider.GetCurrentUserIdAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(auditUserId);

        public void SetDefaultRepositoryLocation(RepositoryLocation? repositoryLocation) => RepositoryLocationRepository
            .Setup(repository => repository.GetDefaultAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(repositoryLocation);

        public void SetImportResult(RepositoryImportResult result)
        {
            ArgumentNullException.ThrowIfNull(result);

            RepositoryImportService
                .Setup(service => service.ImportAsync(It.IsAny<RepositoryImportRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(result);
        }

        public void VerifyAuditIdentityNotRequested() => AuditIdentityProvider.Verify(provider => provider.GetCurrentUserIdAsync(It.IsAny<CancellationToken>()), Times.Never);

        public void VerifyAuditIdentityRequestedOnce(CancellationToken cancellationToken) => AuditIdentityProvider.Verify(provider => provider.GetCurrentUserIdAsync(cancellationToken), Times.Once);

        public void VerifyDefaultRepositoryRequestedOnce(CancellationToken cancellationToken) => RepositoryLocationRepository.Verify(repository => repository.GetDefaultAsync(cancellationToken), Times.Once);

        public void VerifyNoPrerequisiteServicesCalled()
        {
            Persistence.VerifyGet(persistence => persistence.RepositoryLocation, Times.Never);
            VerifyAuditIdentityNotRequested();
        }

        public void VerifyRepositoryImporterCalledOnce() => RepositoryImportService.Verify(service => service.ImportAsync(It.IsAny<RepositoryImportRequest>(), It.IsAny<CancellationToken>()), Times.Once);

        public void VerifyRepositoryImporterNotCalled() => RepositoryImportService.Verify(service => service.ImportAsync(It.IsAny<RepositoryImportRequest>(), It.IsAny<CancellationToken>()), Times.Never);

        private static void EnsureDirectoryExists(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
        }

        private static void TryDeleteDirectory(string directoryPath)
        {
            try
            {
                if (Directory.Exists(directoryPath))
                {
                    Directory.Delete(directoryPath, recursive: true);
                }
            }
            catch
            {
                // Test cleanup must not hide the original test result.
            }
        }
    }
}