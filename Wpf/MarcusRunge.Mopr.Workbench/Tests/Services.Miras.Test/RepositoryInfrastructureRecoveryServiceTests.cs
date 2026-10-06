using MarcusRunge.Mopr.Workbench.Contracts.Models.Configuration;
using MarcusRunge.Mopr.Workbench.Services.Miras.Enums;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Entities;
using Moq;

namespace MarcusRunge.Mopr.Workbench.Services.Miras.Test
{
    public sealed class RepositoryInfrastructureRecoveryServiceTests
    {
        [Fact]
        public async Task RecoverAsync_WhenAdministrativeAuthorizationIsMissing_ReturnsAdministrativeAuthorizationRequiredWithoutAccessingInfrastructure()
        {
            using var context = new RepositoryInfrastructureRecoveryServiceTestContext(isElevatedAdministrator: false);

            var result = await context.RecoveryService.RecoverAsync(RepositoryInfrastructureRecoveryServiceTestContext.CreateRequest(), TestContext.Current.CancellationToken);

            Assert.False(result.IsSuccessful);
            Assert.Equal(RepositoryInfrastructureRecoveryStatus.AdministrativeAuthorizationRequired, result.Status);
            Assert.Null(result.RepositoryLocationId);
            Assert.Null(result.SystemUserId);
            Assert.Null(result.TechnicalException);

            context.VerifyRepositoryPathNotValidated();
            context.VerifyRepositoryLocationNotRead();
            context.VerifyRepositoryLocationNotSaved();
            context.VerifySystemUserNotRequested();
        }

        [Fact]
        public async Task RecoverAsync_WhenRepositoryPathIsInvalid_ReturnsRepositoryValidationFailedWithoutCreatingSystemUser()
        {
            using var context = new RepositoryInfrastructureRecoveryServiceTestContext();
            context.ConfigureInvalidRepositoryPath();

            var result = await context.RecoveryService.RecoverAsync(RepositoryInfrastructureRecoveryServiceTestContext.CreateRequest(), TestContext.Current.CancellationToken);

            Assert.False(result.IsSuccessful);
            Assert.Equal(RepositoryInfrastructureRecoveryStatus.RepositoryValidationFailed, result.Status);
            Assert.Equal(RepositoryInfrastructureRecoveryServiceTestContext.RequestedRepositoryPath, result.NormalizedRepositoryPath);
            Assert.Null(result.RepositoryLocationId);
            Assert.Null(result.SystemUserId);
            Assert.Null(result.TechnicalException);

            context.VerifyRepositoryPathValidatedOnce();
            context.VerifyRepositoryLocationNotRead();
            context.VerifyRepositoryLocationNotSaved();
            context.VerifySystemUserNotRequested();
        }

        [Fact]
        public async Task RecoverAsync_WhenRepositoryLocationAlreadyExists_DoesNotOverwriteExistingConfiguration()
        {
            var existingLocation = new RepositoryLocation
            {
                Id = 17,
                CreatedByUserId = 3,
                IsDefault = true,
                IsEnabled = true,
                Name = "Existing repository",
                RootPath = RepositoryInfrastructureRecoveryServiceTestContext.NormalizedRepositoryPath
            };

            using var context = new RepositoryInfrastructureRecoveryServiceTestContext();
            context.ConfigureValidRepositoryPath();
            context.ConfigureExistingLocations(existingLocation);

            var result = await context.RecoveryService.RecoverAsync(RepositoryInfrastructureRecoveryServiceTestContext.CreateRequest(), TestContext.Current.CancellationToken);

            Assert.False(result.IsSuccessful);
            Assert.Equal(RepositoryInfrastructureRecoveryStatus.RepositoryAlreadyConfigured, result.Status);
            Assert.Null(result.RepositoryLocationId);
            Assert.Null(result.SystemUserId);
            Assert.Null(result.TechnicalException);

            context.VerifyRepositoryPathValidatedOnce();
            context.VerifyRepositoryLocationReadOnce();
            context.VerifyRepositoryLocationNotSaved();
            context.VerifySystemUserNotRequested();
        }

        [Fact]
        public async Task RecoverAsync_WhenInfrastructureIsMissing_CreatesEnabledDefaultLocationWithSystemAuditIdentity()
        {
            using var context = new RepositoryInfrastructureRecoveryServiceTestContext();
            context.ConfigureValidRepositoryPath();
            context.ConfigureExistingLocations();
            context.ConfigureSystemAuditIdentity();
            context.ConfigureRepositorySave();

            var result = await context.RecoveryService.RecoverAsync(RepositoryInfrastructureRecoveryServiceTestContext.CreateRequest(), TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccessful);
            Assert.Equal(RepositoryInfrastructureRecoveryStatus.Completed, result.Status);
            Assert.Equal(RepositoryInfrastructureRecoveryServiceTestContext.RepositoryLocationId, result.RepositoryLocationId);
            Assert.Equal(RepositoryInfrastructureRecoveryServiceTestContext.SystemUserId, result.SystemUserId);
            Assert.Equal(RepositoryInfrastructureRecoveryServiceTestContext.NormalizedRepositoryPath, result.NormalizedRepositoryPath);
            Assert.Null(result.TechnicalException);

            var repositoryLocation = Assert.IsType<RepositoryLocation>(context.SavedRepositoryLocation);

            Assert.Equal(RepositoryInfrastructureRecoveryServiceTestContext.RepositoryLocationId, repositoryLocation.Id);
            Assert.Equal(RepositoryInfrastructureRecoveryServiceTestContext.SystemUserId, repositoryLocation.CreatedByUserId);
            Assert.Equal(RepositoryInfrastructureRecoveryServiceTestContext.NormalizedRepositoryPath, repositoryLocation.RootPath);
            Assert.Equal("Default DICOM repository", repositoryLocation.Name);
            Assert.True(repositoryLocation.IsDefault);
            Assert.True(repositoryLocation.IsEnabled);

            context.VerifyRepositoryPathValidatedOnce();
            context.VerifyRepositoryLocationReadOnce();
            context.VerifySystemUserRequestedOnce();
            context.VerifyRepositoryLocationSavedOnce();
        }

        [Fact]
        public async Task RecoverAsync_WhenSystemAuditIdentityHasInvalidId_ReturnsFailedWithoutSavingRepositoryLocation()
        {
            using var context = new RepositoryInfrastructureRecoveryServiceTestContext();
            context.ConfigureValidRepositoryPath();
            context.ConfigureExistingLocations();
            context.ConfigureSystemAuditIdentity(systemUserId: 0);

            var result = await context.RecoveryService.RecoverAsync(RepositoryInfrastructureRecoveryServiceTestContext.CreateRequest(), TestContext.Current.CancellationToken);

            Assert.False(result.IsSuccessful);
            Assert.Equal(RepositoryInfrastructureRecoveryStatus.Failed, result.Status);
            Assert.IsType<InvalidOperationException>(result.TechnicalException);
            Assert.Same(result.TechnicalException, context.ExceptionThrown);

            context.VerifyRepositoryPathValidatedOnce();
            context.VerifyRepositoryLocationReadOnce();
            context.VerifySystemUserRequestedOnce();
            context.VerifyRepositoryLocationNotSaved();
        }

        [Fact]
        public async Task RecoverAsync_WhenRepositoryLocationCannotBeSaved_ReturnsFailedAndPublishesTechnicalException()
        {
            var saveException = new InvalidOperationException("The repository location could not be saved.");

            using var context = new RepositoryInfrastructureRecoveryServiceTestContext();
            context.ConfigureValidRepositoryPath();
            context.ConfigureExistingLocations();
            context.ConfigureSystemAuditIdentity();
            context.ConfigureRepositorySave(saveException);

            var result = await context.RecoveryService.RecoverAsync(RepositoryInfrastructureRecoveryServiceTestContext.CreateRequest(), TestContext.Current.CancellationToken);

            Assert.False(result.IsSuccessful);
            Assert.Equal(RepositoryInfrastructureRecoveryStatus.Failed, result.Status);
            Assert.Same(saveException, result.TechnicalException);
            Assert.Same(saveException, context.ExceptionThrown);
            Assert.Null(context.SavedRepositoryLocation);

            context.VerifyRepositoryPathValidatedOnce();
            context.VerifyRepositoryLocationReadOnce();
            context.VerifySystemUserRequestedOnce();
            context.VerifyRepositoryLocationSavedOnce();
        }

        [Fact]
        public async Task RecoverAsync_WhenCanceledBeforeStart_PropagatesCancellationWithoutAccessingInfrastructure()
        {
            using var context = new RepositoryInfrastructureRecoveryServiceTestContext();
            using var cancellation = new CancellationTokenSource();

            await cancellation.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => context.RecoveryService.RecoverAsync(RepositoryInfrastructureRecoveryServiceTestContext.CreateRequest(), cancellation.Token));

            context.VerifyRepositoryPathNotValidated();
            context.VerifyRepositoryLocationNotRead();
            context.VerifyRepositoryLocationNotSaved();
            context.VerifySystemUserNotRequested();
        }

        [Fact]
        public async Task RecoverAsync_WhenCanceledDuringRepositoryValidation_PropagatesCancellationWithoutMutation()
        {
            using var context = new RepositoryInfrastructureRecoveryServiceTestContext();
            using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);

            context.RepositoryLocationValidationService.Setup(service => service.ValidateAsync(RepositoryInfrastructureRecoveryServiceTestContext.RequestedRepositoryPath, cancellation.Token)).Callback(() => cancellation.Cancel()).ReturnsAsync(new RepositoryLocationValidationResult
            {
                Exists = true,
                IsReadable = true,
                IsWritable = true,
                NormalizedPath = RepositoryInfrastructureRecoveryServiceTestContext.NormalizedRepositoryPath
            });

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => context.RecoveryService.RecoverAsync(RepositoryInfrastructureRecoveryServiceTestContext.CreateRequest(), cancellation.Token));

            context.VerifyRepositoryLocationNotRead();
            context.VerifyRepositoryLocationNotSaved();
            context.VerifySystemUserNotRequested();
        }
    }
}