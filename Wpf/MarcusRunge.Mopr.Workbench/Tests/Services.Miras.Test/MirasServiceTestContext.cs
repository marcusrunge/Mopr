using MarcusRunge.Mopr.Workbench.Contracts.Miras;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Models;
using MarcusRunge.Mopr.Workbench.Services.Repository.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Repository.Models;
using Moq;

namespace MarcusRunge.Mopr.Workbench.Services.Miras.Test
{
    internal sealed class MirasServiceTestContext : IDisposable
    {
        public MirasServiceTestContext()
        {
            Persistence = new Mock<IPersistence>(MockBehavior.Strict);
            PersistenceIntegrityService = new Mock<IPersistenceIntegrityService>(MockBehavior.Strict);
            Repository = new Mock<IRepository>(MockBehavior.Strict);
            RepositoryRepairService = new Mock<IDicomRepositoryRepairService>(MockBehavior.Strict);

            Persistence.SetupGet(value => value.Integrity).Returns(PersistenceIntegrityService.Object);
            Repository.SetupGet(value => value.RepositoryRepairService).Returns(RepositoryRepairService.Object);

            ApplicationLifetime = new TestApplicationLifetime();

            var factory = new MirasFactory(ApplicationLifetime, Persistence.Object, Repository.Object);
            Service = factory.Create().MirasService ?? throw new InvalidOperationException("The MIRAS service was not initialized.");
        }

        public TestApplicationLifetime ApplicationLifetime { get; }

        public Mock<IPersistence> Persistence { get; }

        public Mock<IPersistenceIntegrityService> PersistenceIntegrityService { get; }

        public Mock<IRepository> Repository { get; }

        public Mock<IDicomRepositoryRepairService> RepositoryRepairService { get; }

        public IMirasService Service { get; }

        public void ConfigurePersistenceResult(PersistenceIntegrityResult result) => PersistenceIntegrityService.Setup(service => service.VerifyAsync(It.Is<PersistenceIntegrityRequest>(request => request.VerifyAuditReferences && request.VerifyRelationships && request.VerifyRequiredValues && request.VerifyUniqueValues), It.IsAny<CancellationToken>())).ReturnsAsync(result);

        public void ConfigureRepositoryResult(DicomRepositoryRepairResult result) => RepositoryRepairService.Setup(service => service.RepairAsync(It.Is<DicomRepositoryRepairRequest>(request => request.VerifyFiles && !request.RepairMissingFiles && request.RepositoryLocationId == null), It.IsAny<CancellationToken>())).ReturnsAsync(result);

        public void Dispose()
        {
            ApplicationLifetime.Dispose();
            GC.SuppressFinalize(this);
        }

        public void VerifyPersistenceCalledOnce() => PersistenceIntegrityService.Verify(service => service.VerifyAsync(It.Is<PersistenceIntegrityRequest>(request => request.VerifyAuditReferences && request.VerifyRelationships && request.VerifyRequiredValues && request.VerifyUniqueValues), It.IsAny<CancellationToken>()), Times.Once);

        public void VerifyRepositoryCalledOnce() => RepositoryRepairService.Verify(service => service.RepairAsync(It.Is<DicomRepositoryRepairRequest>(request => request.VerifyFiles && !request.RepairMissingFiles && request.RepositoryLocationId == null), It.IsAny<CancellationToken>()), Times.Once);

        public void VerifyRepositoryNotCalled() => RepositoryRepairService.Verify(service => service.RepairAsync(It.IsAny<DicomRepositoryRepairRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}