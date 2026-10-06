using MarcusRunge.Mopr.Workbench.Contracts.Application.Administration.Services;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Configuration.Services;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Security.Services;
using MarcusRunge.Mopr.Workbench.Contracts.Models.Configuration;
using MarcusRunge.Mopr.Workbench.Services.Miras.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Miras.Models;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Entities;
using MarcusRunge.Mopr.Workbench.Services.Repository.Contracts;
using Moq;

namespace MarcusRunge.Mopr.Workbench.Services.Miras.Test
{
    internal sealed class RepositoryInfrastructureRecoveryServiceTestContext : IDisposable
    {
        internal const int RepositoryLocationId = 41;
        internal const int SystemUserId = 73;
        internal const string NormalizedRepositoryPath = @"D:\MOPR\Repository";
        internal const string RequestedRepositoryPath = @"D:\MOPR\Repository\";

        private bool _disposed;
        private int _nextRepositoryLocationId = RepositoryLocationId;

        internal RepositoryInfrastructureRecoveryServiceTestContext(bool isElevatedAdministrator = true)
        {
            AdministrativeAuthorizationService = new Mock<IAdministrativeAuthorizationService>(MockBehavior.Strict);
            Persistence = new Mock<IPersistence>(MockBehavior.Strict);
            Repository = new Mock<IRepository>(MockBehavior.Strict);
            RepositoryLocationRepository = new Mock<IRepositoryLocationRepository>(MockBehavior.Strict);
            RepositoryLocationValidationService = new Mock<IRepositoryLocationValidationService>(MockBehavior.Strict);
            SystemAuditIdentityProvider = new Mock<ISystemAuditIdentityProvider>(MockBehavior.Strict);

            AdministrativeAuthorizationService.SetupGet(service => service.IsElevatedAdministrator).Returns(isElevatedAdministrator);
            Persistence.SetupGet(persistence => persistence.RepositoryLocation).Returns(RepositoryLocationRepository.Object);

            ApplicationLifetime = new TestApplicationLifetime();

            var factory = new MirasFactory(ApplicationLifetime, Persistence.Object, Repository.Object, AdministrativeAuthorizationService.Object, RepositoryLocationValidationService.Object, SystemAuditIdentityProvider.Object);
            Miras = factory.Create();
            RecoveryService = Miras.RepositoryInfrastructureRecovery ?? throw new InvalidOperationException("The MIRAS repository-infrastructure recovery service was not initialized.");
            Miras.ExceptionThrown += OnExceptionThrown;
        }

        internal Mock<IAdministrativeAuthorizationService> AdministrativeAuthorizationService { get; }

        internal TestApplicationLifetime ApplicationLifetime { get; }

        internal Exception? ExceptionThrown { get; private set; }

        internal IMiras Miras { get; }

        internal Mock<IPersistence> Persistence { get; }

        internal IRepositoryInfrastructureRecoveryService RecoveryService { get; }

        internal Mock<IRepository> Repository { get; }

        internal RepositoryLocation? SavedRepositoryLocation { get; private set; }

        internal Mock<IRepositoryLocationRepository> RepositoryLocationRepository { get; }

        internal Mock<IRepositoryLocationValidationService> RepositoryLocationValidationService { get; }

        internal Mock<ISystemAuditIdentityProvider> SystemAuditIdentityProvider { get; }

        internal void ConfigureExistingLocations(params RepositoryLocation[] repositoryLocations)
        {
            ArgumentNullException.ThrowIfNull(repositoryLocations);
            RepositoryLocationRepository.Setup(repository => repository.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(repositoryLocations);
        }

        internal void ConfigureInvalidRepositoryPath() => RepositoryLocationValidationService.Setup(service => service.ValidateAsync(RequestedRepositoryPath, It.IsAny<CancellationToken>())).ReturnsAsync(new RepositoryLocationValidationResult
        {
            Exists = false,
            IsReadable = false,
            IsWritable = false,
            NormalizedPath = RequestedRepositoryPath
        });

        internal void ConfigureRepositorySave(Exception? exception = null) => RepositoryLocationRepository.Setup(repository => repository.AddAsync(It.IsAny<RepositoryLocation>(), It.IsAny<CancellationToken>())).Callback<RepositoryLocation, CancellationToken>((repositoryLocation, cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (exception is not null)
            {
                throw exception;
            }

            repositoryLocation.Id = _nextRepositoryLocationId++;
            SavedRepositoryLocation = repositoryLocation;
        }).Returns(Task.CompletedTask);

        internal void ConfigureSystemAuditIdentity(int systemUserId = SystemUserId) => SystemAuditIdentityProvider.Setup(provider => provider.GetOrCreateUserIdAsync(It.IsAny<CancellationToken>())).ReturnsAsync(systemUserId);

        internal void ConfigureValidRepositoryPath() => RepositoryLocationValidationService.Setup(service => service.ValidateAsync(RequestedRepositoryPath, It.IsAny<CancellationToken>())).ReturnsAsync(new RepositoryLocationValidationResult
        {
            Exists = true,
            IsReadable = true,
            IsWritable = true,
            NormalizedPath = NormalizedRepositoryPath
        });

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            Miras.ExceptionThrown -= OnExceptionThrown;
            ApplicationLifetime.Dispose();
            GC.SuppressFinalize(this);
        }

        internal static RepositoryInfrastructureRecoveryRequest CreateRequest() => new(RequestedRepositoryPath);

        internal void VerifyRepositoryLocationNotRead() => RepositoryLocationRepository.Verify(repository => repository.GetAllAsync(It.IsAny<CancellationToken>()), Times.Never);

        internal void VerifyRepositoryLocationNotSaved() => RepositoryLocationRepository.Verify(repository => repository.AddAsync(It.IsAny<RepositoryLocation>(), It.IsAny<CancellationToken>()), Times.Never);

        internal void VerifyRepositoryLocationReadOnce() => RepositoryLocationRepository.Verify(repository => repository.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);

        internal void VerifyRepositoryLocationSavedOnce() => RepositoryLocationRepository.Verify(repository => repository.AddAsync(It.IsAny<RepositoryLocation>(), It.IsAny<CancellationToken>()), Times.Once);

        internal void VerifyRepositoryPathNotValidated() => RepositoryLocationValidationService.Verify(service => service.ValidateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);

        internal void VerifyRepositoryPathValidatedOnce() => RepositoryLocationValidationService.Verify(service => service.ValidateAsync(RequestedRepositoryPath, It.IsAny<CancellationToken>()), Times.Once);

        internal void VerifySystemUserNotRequested() => SystemAuditIdentityProvider.Verify(provider => provider.GetOrCreateUserIdAsync(It.IsAny<CancellationToken>()), Times.Never);

        internal void VerifySystemUserRequestedOnce() => SystemAuditIdentityProvider.Verify(provider => provider.GetOrCreateUserIdAsync(It.IsAny<CancellationToken>()), Times.Once);

        private void OnExceptionThrown(Exception exception) => ExceptionThrown = exception;
    }
}