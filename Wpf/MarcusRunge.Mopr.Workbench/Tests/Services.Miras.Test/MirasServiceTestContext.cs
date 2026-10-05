using MarcusRunge.Mopr.Workbench.Contracts.Application.Administration.Services;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Configuration.Services;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Security.Services;
using MarcusRunge.Mopr.Workbench.Services.Miras.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Models;
using MarcusRunge.Mopr.Workbench.Services.Repository.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Repository.Models;
using Moq;

namespace MarcusRunge.Mopr.Workbench.Services.Miras.Test
{
    internal sealed class MirasServiceTestContext : IDisposable
    {
        private bool _disposed;

        public MirasServiceTestContext()
        {
            AdministrativeAuthorizationService = new Mock<IAdministrativeAuthorizationService>(MockBehavior.Strict);
            Persistence = new Mock<IPersistence>(MockBehavior.Strict);
            PersistenceIntegrityService = new Mock<IPersistenceIntegrityService>(MockBehavior.Strict);
            Repository = new Mock<IRepository>(MockBehavior.Strict);
            RepositoryLocationValidationService = new Mock<IRepositoryLocationValidationService>(MockBehavior.Strict);
            RepositoryRepairService = new Mock<IDicomRepositoryRepairService>(MockBehavior.Strict);
            SystemAuditIdentityProvider = new Mock<ISystemAuditIdentityProvider>(MockBehavior.Strict);

            Persistence.SetupGet(value => value.Integrity).Returns(PersistenceIntegrityService.Object);
            Repository.SetupGet(value => value.RepositoryRepairService).Returns(RepositoryRepairService.Object);

            ApplicationLifetime = new TestApplicationLifetime();

            var factory = new MirasFactory(ApplicationLifetime, Persistence.Object, Repository.Object, AdministrativeAuthorizationService.Object, RepositoryLocationValidationService.Object, SystemAuditIdentityProvider.Object);
            Operations = factory.Create().Operations ?? throw new InvalidOperationException("The MIRAS service was not initialized.");
        }

        public Mock<IAdministrativeAuthorizationService> AdministrativeAuthorizationService { get; }

        public TestApplicationLifetime ApplicationLifetime { get; }

        public IOperations Operations { get; }

        public Mock<IPersistence> Persistence { get; }

        public Mock<IPersistenceIntegrityService> PersistenceIntegrityService { get; }

        public Mock<IRepository> Repository { get; }

        public Mock<IRepositoryLocationValidationService> RepositoryLocationValidationService { get; }

        public Mock<IDicomRepositoryRepairService> RepositoryRepairService { get; }

        public Mock<ISystemAuditIdentityProvider> SystemAuditIdentityProvider { get; }

        public void ConfigurePersistenceResult(PersistenceIntegrityResult result)
        {
            ArgumentNullException.ThrowIfNull(result);
            PersistenceIntegrityService.Setup(service => service.VerifyAsync(It.Is<PersistenceIntegrityRequest>(request => IsSafePersistenceRequest(request)), It.IsAny<CancellationToken>())).ReturnsAsync(result);
        }

        public void ConfigureRepositoryResult(DicomRepositoryRepairResult result)
        {
            ArgumentNullException.ThrowIfNull(result);
            RepositoryRepairService.Setup(service => service.RepairAsync(It.Is<DicomRepositoryRepairRequest>(request => IsSafeRepositoryRequest(request)), It.IsAny<CancellationToken>())).ReturnsAsync(result);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            ApplicationLifetime.Dispose();
            GC.SuppressFinalize(this);
        }

        public void VerifyPersistenceCalledOnce() => PersistenceIntegrityService.Verify(service => service.VerifyAsync(It.Is<PersistenceIntegrityRequest>(request => IsSafePersistenceRequest(request)), It.IsAny<CancellationToken>()), Times.Once);

        public void VerifyRepositoryCalledOnce() => RepositoryRepairService.Verify(service => service.RepairAsync(It.Is<DicomRepositoryRepairRequest>(request => IsSafeRepositoryRequest(request)), It.IsAny<CancellationToken>()), Times.Once);

        public void VerifyRepositoryNotCalled() => RepositoryRepairService.Verify(service => service.RepairAsync(It.IsAny<DicomRepositoryRepairRequest>(), It.IsAny<CancellationToken>()), Times.Never);

        private static bool IsSafePersistenceRequest(PersistenceIntegrityRequest request) => request.VerifyAuditReferences && request.VerifyRelationships && request.VerifyRequiredValues && request.VerifyUniqueValues;

        private static bool IsSafeRepositoryRequest(DicomRepositoryRepairRequest request) => request.VerifyFiles && !request.RepairMissingFiles && request.RepositoryLocationId == null;
    }
}