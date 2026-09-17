using MarcusRunge.Mopr.Workbench.Contracts.Application.Identity.Models;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Identity.Services;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Import.Models;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Identity;
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

            OperatingSystemIdentityProvider = new Mock<IOperatingSystemIdentityProvider>(MockBehavior.Strict);
            Factory = new ApplicationFactory(Persistence.Object, Repository.Object, OperatingSystemIdentityProvider.Object);
            Application = Factory.Create();

            var identityService = Application.IdentityService ?? throw new InvalidOperationException("The identity service has not been initialized.");
            var identityServiceBase = identityService as IIdentityServiceBase ?? throw new InvalidOperationException("The internal identity-service contract is not available.");

            CurrentUserContextManager = identityServiceBase.CurrentUserContextManager ?? throw new InvalidOperationException("The current-user context manager has not been initialized.");
            Service = Application.ImportService?.DicomImportService ?? throw new InvalidOperationException("The DICOM import service has not been initialized.");

            Reset();
        }

        public int AuditUserId => _auditUserId;

        public Contracts.IApplication Application { get; }

        internal ICurrentUserContextManager CurrentUserContextManager { get; }

        public ApplicationFactory Factory { get; }

        public Mock<IOperatingSystemIdentityProvider> OperatingSystemIdentityProvider { get; }

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

            Persistence.Reset();
            Repository.Reset();
            RepositoryImportService.Reset();
            RepositoryLocationRepository.Reset();

            EnsureDirectoryExists(SourceDirectoryPath);
            EnsureDirectoryExists(RepositoryDirectoryPath);

            var defaultRepositoryLocation = new RepositoryLocation { Id = RepositoryLocationId, IsDefault = true, IsEnabled = true, RootPath = RepositoryDirectoryPath };

            RepositoryLocationRepository.Setup(repository => repository.GetDefaultAsync(It.IsAny<CancellationToken>())).ReturnsAsync(defaultRepositoryLocation);
            Persistence.SetupGet(persistence => persistence.RepositoryLocation).Returns(repositoryLocationRepositoryAvailable ? RepositoryLocationRepository.Object : null);
            Repository.SetupGet(repository => repository.ImportService).Returns(repositoryImportServiceAvailable ? RepositoryImportService.Object : null);

            SetAuditUserId(AuditUserId);
            SetImportResult(new RepositoryImportResult());
        }

        public void SetAuditUserId(int? auditUserId)
        {
            _auditUserId = auditUserId ?? 0;

            if (auditUserId is null or <= 0)
            {
                CurrentUserContextManager.ClearAsync(CancellationToken.None).GetAwaiter().GetResult();
                return;
            }

            var user = new CurrentUser(auditUserId.Value, @"DOMAIN\ImportUser", "Import", "User", "IU", isActive: true);
            CurrentUserContextManager.SetCurrentUserAsync(user, CancellationToken.None).GetAwaiter().GetResult();
        }

        public void SetDefaultRepositoryLocation(RepositoryLocation? repositoryLocation) => RepositoryLocationRepository.Setup(repository => repository.GetDefaultAsync(It.IsAny<CancellationToken>())).ReturnsAsync(repositoryLocation);

        public void SetImportResult(RepositoryImportResult result)
        {
            ArgumentNullException.ThrowIfNull(result);

            RepositoryImportService.Setup(service => service.ImportAsync(It.IsAny<RepositoryImportRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(result);
        }

        public void VerifyDefaultRepositoryRequestedOnce(CancellationToken cancellationToken) => RepositoryLocationRepository.Verify(repository => repository.GetDefaultAsync(cancellationToken), Times.Once);

        public void VerifyNoPrerequisiteServicesCalled() => Persistence.VerifyGet(persistence => persistence.RepositoryLocation, Times.Never);

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