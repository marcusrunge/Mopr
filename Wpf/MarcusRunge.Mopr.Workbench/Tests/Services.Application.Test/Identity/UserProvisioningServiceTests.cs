using MarcusRunge.Mopr.Workbench.Contracts.Application.Administration.Services;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Identity;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Identity.Models;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Identity.Services;
using MarcusRunge.Mopr.Workbench.Contracts.Enums;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Identity;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Entities;
using Moq;

namespace MarcusRunge.Mopr.Workbench.Services.Application.Test.Identity
{
    public sealed class UserProvisioningServiceTests
    {
        private const int UserId = 73;
        private const string FirstName = "Marcus";
        private const string LastName = "Runge";
        private const string LoginName = @"DOMAIN\User";
        private const string OtherSecurityIdentifier = "S-1-5-21-9000000000-8000000000-7000000000-1001";
        private const string SecurityIdentifier = "S-1-5-21-1000000000-2000000000-3000000000-1001";
        private const string ShortName = "MR";

        [Fact]
        public async Task ProvisionAsync_WhenAdministrativeAuthorizationIsMissing_ReturnsAuthorizationRequiredWithoutAccessingIdentityOrPersistence()
        {
            var context = CreateContext(isElevatedAdministrator: false);

            var result = await context.UserProvisioningService.ProvisionAsync(CreateRequest(), TestContext.Current.CancellationToken);
            var currentUser = await context.CurrentUserContext.GetCurrentUserAsync(TestContext.Current.CancellationToken);

            Assert.Equal(UserProvisioningStatus.AdministrativeAuthorizationRequired, result.Status);
            Assert.False(result.IsSuccessful);
            Assert.Null(result.OperatingSystemIdentity);
            Assert.Null(result.User);
            Assert.Null(currentUser);

            context.AdministrativeAuthorizationService.VerifyGet(service => service.IsElevatedAdministrator, Times.Once);
            context.OperatingSystemIdentityProvider.Verify(provider => provider.GetCurrentIdentityAsync(It.IsAny<CancellationToken>()), Times.Never);
            context.Persistence.VerifyGet(persistence => persistence.User, Times.Never);
            context.VerifyNoUserLookup();
            context.VerifyUserNotAdded();
            context.VerifyUserNotUpdated();
        }

        [Fact]
        public async Task ProvisionAsync_WhenAdministrativeAuthorizationIsMissing_PreservesExistingCurrentUser()
        {
            var persistentUser = CreatePersistentUser(isActive: true);
            var context = CreateContext(persistentUser, isElevatedAdministrator: false);

            var signInResult = await context.UserSignInService.SignInAsync(TestContext.Current.CancellationToken);
            var signedInUser = await context.CurrentUserContext.GetCurrentUserAsync(TestContext.Current.CancellationToken);

            Assert.Equal(UserSignInStatus.SignedIn, signInResult.Status);
            Assert.NotNull(signedInUser);

            var provisioningResult = await context.UserProvisioningService.ProvisionAsync(CreateRequest(), TestContext.Current.CancellationToken);
            var currentUserAfterProvisioning = await context.CurrentUserContext.GetCurrentUserAsync(TestContext.Current.CancellationToken);

            Assert.Equal(UserProvisioningStatus.AdministrativeAuthorizationRequired, provisioningResult.Status);
            Assert.Same(signedInUser, currentUserAfterProvisioning);

            context.AdministrativeAuthorizationService.VerifyGet(service => service.IsElevatedAdministrator, Times.Once);
            context.VerifyUserNotAdded();
            context.VerifyUserNotUpdated();
        }

        [Fact]
        public async Task ProvisionAsync_WhenAdministrativeAuthorizationServiceIsUnavailable_ReturnsAuthorizationRequiredWithoutAccessingDependencies()
        {
            var context = CreateContext(administrativeAuthorizationServiceAvailable: false);

            var result = await context.UserProvisioningService.ProvisionAsync(CreateRequest(), TestContext.Current.CancellationToken);
            var currentUser = await context.CurrentUserContext.GetCurrentUserAsync(TestContext.Current.CancellationToken);

            Assert.Equal(UserProvisioningStatus.AdministrativeAuthorizationRequired, result.Status);
            Assert.False(result.IsSuccessful);
            Assert.Null(result.OperatingSystemIdentity);
            Assert.Null(result.User);
            Assert.Null(currentUser);

            context.OperatingSystemIdentityProvider.Verify(provider => provider.GetCurrentIdentityAsync(It.IsAny<CancellationToken>()), Times.Never);
            context.Persistence.VerifyGet(persistence => persistence.User, Times.Never);
            context.VerifyNoUserLookup();
            context.VerifyUserNotAdded();
            context.VerifyUserNotUpdated();
        }

        [Fact]
        public async Task ProvisionAsync_WhenRequestIsValid_CreatesAndPublishesPersistentUserWithSecurityIdentifier()
        {
            var context = CreateContext();
            User? createdUser = null;

            context.UserRepository
                .Setup(repository => repository.AddAsync(It.IsAny<User>(), TestContext.Current.CancellationToken))
                .Callback<User, CancellationToken>((user, _) =>
                {
                    user.Id = UserId;
                    createdUser = user;
                })
                .Returns(Task.CompletedTask);

            var result = await context.UserProvisioningService.ProvisionAsync(CreateRequest(), TestContext.Current.CancellationToken);
            var currentUser = await context.CurrentUserContext.GetCurrentUserAsync(TestContext.Current.CancellationToken);

            Assert.Equal(UserProvisioningStatus.Completed, result.Status);
            Assert.True(result.IsSuccessful);
            Assert.Empty(result.ValidationIssues);
            Assert.NotNull(result.OperatingSystemIdentity);
            Assert.Equal(LoginName, result.OperatingSystemIdentity.LoginName);
            Assert.Equal(SecurityIdentifier, result.OperatingSystemIdentity.SecurityIdentifier);
            Assert.NotNull(result.User);
            Assert.Same(result.User, currentUser);
            Assert.Equal(UserId, currentUser?.Id);
            Assert.Equal(LoginName, currentUser?.LoginName);
            Assert.Equal(SecurityIdentifier, currentUser?.SecurityIdentifier);
            Assert.Equal(FirstName, currentUser?.FirstName);
            Assert.Equal(LastName, currentUser?.LastName);
            Assert.Equal(ShortName, currentUser?.ShortName);
            Assert.Equal($"{FirstName} {LastName}", currentUser?.DisplayName);
            Assert.True(currentUser?.IsActive);
            Assert.NotNull(createdUser);
            Assert.Equal(SecurityIdentifier, createdUser.SecurityIdentifier);

            context.VerifySidLookupOnce();
            context.VerifyLoginNameLookupOnce();
            context.UserRepository.Verify(repository => repository.AddAsync(It.Is<User>(user => user.LoginName == LoginName && user.SecurityIdentifier == SecurityIdentifier && user.FirstName == FirstName && user.LastName == LastName && user.ShortName == ShortName && user.IsActive), TestContext.Current.CancellationToken), Times.Once);
        }

        [Fact]
        public async Task ProvisionAsync_WhenValuesContainWhitespace_NormalizesBeforePersistence()
        {
            var context = CreateContext();

            context.UserRepository
                .Setup(repository => repository.AddAsync(It.IsAny<User>(), TestContext.Current.CancellationToken))
                .Callback<User, CancellationToken>((user, _) => user.Id = UserId)
                .Returns(Task.CompletedTask);

            var request = new UserProvisioningRequest("  Marcus   Alexander  ", "  Runge  ", "  M   R  ");

            var result = await context.UserProvisioningService.ProvisionAsync(request, TestContext.Current.CancellationToken);

            Assert.Equal(UserProvisioningStatus.Completed, result.Status);
            Assert.NotNull(result.User);
            Assert.Equal("Marcus Alexander", result.User.FirstName);
            Assert.Equal("Runge", result.User.LastName);
            Assert.Equal("M R", result.User.ShortName);
            Assert.Equal(SecurityIdentifier, result.User.SecurityIdentifier);

            context.UserRepository.Verify(repository => repository.AddAsync(It.Is<User>(user => user.FirstName == "Marcus Alexander" && user.LastName == "Runge" && user.ShortName == "M R" && user.SecurityIdentifier == SecurityIdentifier), TestContext.Current.CancellationToken), Times.Once);
        }

        [Fact]
        public async Task ProvisionAsync_WhenRequiredValuesAreMissing_ReturnsAllValidationIssues()
        {
            var context = CreateContext();
            var request = new UserProvisioningRequest(" ", null, "\t");

            var result = await context.UserProvisioningService.ProvisionAsync(request, TestContext.Current.CancellationToken);

            Assert.Equal(UserProvisioningStatus.ValidationFailed, result.Status);
            Assert.False(result.IsSuccessful);
            Assert.Null(result.OperatingSystemIdentity);
            Assert.Null(result.User);
            Assert.Contains(UserProvisioningValidationIssue.FirstNameRequired, result.ValidationIssues);
            Assert.Contains(UserProvisioningValidationIssue.LastNameRequired, result.ValidationIssues);
            Assert.Contains(UserProvisioningValidationIssue.ShortNameRequired, result.ValidationIssues);
            Assert.Equal(3, result.ValidationIssues.Count);

            context.AdministrativeAuthorizationService.VerifyGet(service => service.IsElevatedAdministrator, Times.Never);
            context.OperatingSystemIdentityProvider.Verify(provider => provider.GetCurrentIdentityAsync(It.IsAny<CancellationToken>()), Times.Never);
            context.VerifyNoUserLookup();
            context.VerifyUserNotAdded();
        }

        [Fact]
        public async Task ProvisionAsync_WhenValuesExceedPersistenceLimits_ReturnsLengthValidationIssues()
        {
            var context = CreateContext();
            var request = new UserProvisioningRequest(new string('F', 257), new string('L', 257), new string('S', 65));

            var result = await context.UserProvisioningService.ProvisionAsync(request, TestContext.Current.CancellationToken);

            Assert.Equal(UserProvisioningStatus.ValidationFailed, result.Status);
            Assert.Contains(UserProvisioningValidationIssue.FirstNameTooLong, result.ValidationIssues);
            Assert.Contains(UserProvisioningValidationIssue.LastNameTooLong, result.ValidationIssues);
            Assert.Contains(UserProvisioningValidationIssue.ShortNameTooLong, result.ValidationIssues);
            Assert.Equal(3, result.ValidationIssues.Count);

            context.AdministrativeAuthorizationService.VerifyGet(service => service.IsElevatedAdministrator, Times.Never);
            context.OperatingSystemIdentityProvider.Verify(provider => provider.GetCurrentIdentityAsync(It.IsAny<CancellationToken>()), Times.Never);
            context.VerifyNoUserLookup();
        }

        [Fact]
        public async Task ProvisionAsync_WhenShortNameContainsControlCharacter_ReturnsValidationFailure()
        {
            var context = CreateContext();
            var request = new UserProvisioningRequest(FirstName, LastName, "M\u0001R");

            var result = await context.UserProvisioningService.ProvisionAsync(request, TestContext.Current.CancellationToken);

            Assert.Equal(UserProvisioningStatus.ValidationFailed, result.Status);
            Assert.Contains(UserProvisioningValidationIssue.ShortNameInvalid, result.ValidationIssues);

            context.AdministrativeAuthorizationService.VerifyGet(service => service.IsElevatedAdministrator, Times.Never);
            context.VerifyNoUserLookup();
            context.VerifyUserNotAdded();
        }

        [Fact]
        public async Task ProvisionAsync_WhenOperatingSystemIdentityIsUnavailable_DoesNotCreateUser()
        {
            var context = CreateContext();

            context.OperatingSystemIdentityProvider
                .Setup(provider => provider.GetCurrentIdentityAsync(TestContext.Current.CancellationToken))
                .ReturnsAsync((OperatingSystemIdentity?)null);

            var result = await context.UserProvisioningService.ProvisionAsync(CreateRequest(), TestContext.Current.CancellationToken);
            var currentUser = await context.CurrentUserContext.GetCurrentUserAsync(TestContext.Current.CancellationToken);

            Assert.Equal(UserProvisioningStatus.OperatingSystemIdentityUnavailable, result.Status);
            Assert.False(result.IsSuccessful);
            Assert.Null(result.OperatingSystemIdentity);
            Assert.Null(result.User);
            Assert.Null(currentUser);

            context.AdministrativeAuthorizationService.VerifyGet(service => service.IsElevatedAdministrator, Times.Once);
            context.VerifyNoUserLookup();
            context.VerifyUserNotAdded();
        }

        [Fact]
        public async Task ProvisionAsync_WhenOperatingSystemIdentityHasNoSecurityIdentifier_DoesNotCreateUser()
        {
            var context = CreateContext();

            context.OperatingSystemIdentityProvider
                .Setup(provider => provider.GetCurrentIdentityAsync(TestContext.Current.CancellationToken))
                .ReturnsAsync(new OperatingSystemIdentity(LoginName));

            var result = await context.UserProvisioningService.ProvisionAsync(CreateRequest(), TestContext.Current.CancellationToken);

            Assert.Equal(UserProvisioningStatus.OperatingSystemIdentityUnavailable, result.Status);
            Assert.False(result.IsSuccessful);
            Assert.Null(result.User);

            context.VerifyNoUserLookup();
            context.VerifyUserNotAdded();
        }

        [Fact]
        public async Task ProvisionAsync_WhenPersistenceUserRepositoryIsUnavailable_ReturnsPersistenceUnavailable()
        {
            var context = CreateContext(userRepositoryAvailable: false);

            var result = await context.UserProvisioningService.ProvisionAsync(CreateRequest(), TestContext.Current.CancellationToken);
            var currentUser = await context.CurrentUserContext.GetCurrentUserAsync(TestContext.Current.CancellationToken);

            Assert.Equal(UserProvisioningStatus.PersistenceUnavailable, result.Status);
            Assert.False(result.IsSuccessful);
            Assert.NotNull(result.OperatingSystemIdentity);
            Assert.Equal(SecurityIdentifier, result.OperatingSystemIdentity.SecurityIdentifier);
            Assert.Null(result.User);
            Assert.Null(currentUser);
        }

        [Fact]
        public async Task ProvisionAsync_WhenActiveUserExistsBySecurityIdentifier_ReturnsExistingUserWithoutLoginNameFallback()
        {
            var existingUser = CreatePersistentUser(isActive: true);
            var context = CreateContext(existingUser);

            var result = await context.UserProvisioningService.ProvisionAsync(CreateRequest(), TestContext.Current.CancellationToken);
            var currentUser = await context.CurrentUserContext.GetCurrentUserAsync(TestContext.Current.CancellationToken);

            Assert.Equal(UserProvisioningStatus.UserAlreadyExists, result.Status);
            Assert.False(result.IsSuccessful);
            Assert.NotNull(result.User);
            Assert.Same(result.User, currentUser);
            Assert.Equal(UserId, currentUser?.Id);
            Assert.Equal(SecurityIdentifier, currentUser?.SecurityIdentifier);
            Assert.True(currentUser?.IsActive);

            context.VerifySidLookupOnce();
            context.VerifyLoginNameLookupNever();
            context.VerifyUserNotAdded();
            context.VerifyUserNotUpdated();
        }

        [Fact]
        public async Task ProvisionAsync_WhenDisabledUserExistsBySecurityIdentifier_ReturnsUserDisabledWithoutPublishingContext()
        {
            var existingUser = CreatePersistentUser(isActive: false);
            var context = CreateContext(existingUser);

            var result = await context.UserProvisioningService.ProvisionAsync(CreateRequest(), TestContext.Current.CancellationToken);
            var currentUser = await context.CurrentUserContext.GetCurrentUserAsync(TestContext.Current.CancellationToken);

            Assert.Equal(UserProvisioningStatus.UserDisabled, result.Status);
            Assert.False(result.IsSuccessful);
            Assert.NotNull(result.User);
            Assert.Equal(SecurityIdentifier, result.User.SecurityIdentifier);
            Assert.False(result.User.IsActive);
            Assert.Null(currentUser);

            context.VerifySidLookupOnce();
            context.VerifyLoginNameLookupNever();
            context.VerifyUserNotAdded();
            context.VerifyUserNotUpdated();
        }

        [Fact]
        public async Task ProvisionAsync_WhenLegacyUserExistsByLoginName_AssignsSecurityIdentifierAndPublishesContext()
        {
            var legacyUser = CreatePersistentUser(isActive: true, securityIdentifier: null);
            var context = CreateContext(legacyUser);

            context.UserRepository
                .Setup(repository => repository.UpdateAsync(legacyUser, TestContext.Current.CancellationToken))
                .Returns(Task.CompletedTask);

            var result = await context.UserProvisioningService.ProvisionAsync(CreateRequest(), TestContext.Current.CancellationToken);
            var currentUser = await context.CurrentUserContext.GetCurrentUserAsync(TestContext.Current.CancellationToken);

            Assert.Equal(UserProvisioningStatus.UserAlreadyExists, result.Status);
            Assert.Equal(SecurityIdentifier, legacyUser.SecurityIdentifier);
            Assert.Equal(SecurityIdentifier, result.User?.SecurityIdentifier);
            Assert.Equal(SecurityIdentifier, currentUser?.SecurityIdentifier);
            Assert.Same(result.User, currentUser);

            context.VerifySidLookupOnce();
            context.VerifyLoginNameLookupOnce();
            context.UserRepository.Verify(repository => repository.UpdateAsync(legacyUser, TestContext.Current.CancellationToken), Times.Once);
            context.VerifyUserNotAdded();
        }

        [Fact]
        public async Task ProvisionAsync_WhenLoginNameBelongsToDifferentSecurityIdentifier_DoesNotReassignUser()
        {
            var differentlyAssignedUser = CreatePersistentUser(isActive: true, securityIdentifier: OtherSecurityIdentifier);
            var context = CreateContext(setupDefaultLookup: false);

            context.ConfigureSidLookup(null);
            context.ConfigureLoginNameLookup(differentlyAssignedUser);
            context.UserRepository.Setup(repository => repository.HasPersonalUsersAsync(TestContext.Current.CancellationToken)).ReturnsAsync(true);

            var result = await context.UserProvisioningService.ProvisionAsync(CreateRequest(), TestContext.Current.CancellationToken);
            var currentUser = await context.CurrentUserContext.GetCurrentUserAsync(TestContext.Current.CancellationToken);

            Assert.Equal(UserProvisioningStatus.Failed, result.Status);
            Assert.Equal(OtherSecurityIdentifier, differentlyAssignedUser.SecurityIdentifier);
            Assert.Null(result.User);
            Assert.Null(currentUser);

            context.VerifySidLookupOnce();
            context.VerifyLoginNameLookupOnce();
            context.VerifyUserNotAdded();
            context.VerifyUserNotUpdated();
        }

        [Fact]
        public async Task ProvisionAsync_WhenExistingSidUserHasChangedLoginName_SynchronizesLoginName()
        {
            const string previousLoginName = @"DOMAIN\PreviousName";

            var existingUser = CreatePersistentUser(isActive: true);
            existingUser.LoginName = previousLoginName;

            var context = CreateContext(existingUser);

            context.UserRepository
                .Setup(repository => repository.UpdateAsync(existingUser, TestContext.Current.CancellationToken))
                .Returns(Task.CompletedTask);

            var result = await context.UserProvisioningService.ProvisionAsync(CreateRequest(), TestContext.Current.CancellationToken);

            Assert.Equal(UserProvisioningStatus.UserAlreadyExists, result.Status);
            Assert.Equal(LoginName, existingUser.LoginName);
            Assert.Equal(SecurityIdentifier, existingUser.SecurityIdentifier);
            Assert.Equal(LoginName, result.User?.LoginName);

            context.VerifySidLookupOnce();
            context.VerifyLoginNameLookupNever();
            context.UserRepository.Verify(repository => repository.UpdateAsync(existingUser, TestContext.Current.CancellationToken), Times.Once);
        }

        [Fact]
        public async Task ProvisionAsync_WhenExistingIdentitySynchronizationFails_ReturnsFailedWithoutPublishingContext()
        {
            var legacyUser = CreatePersistentUser(isActive: true, securityIdentifier: null);
            var synchronizationException = new InvalidOperationException("Identity synchronization failed.");
            var context = CreateContext(legacyUser);

            context.UserRepository
                .Setup(repository => repository.UpdateAsync(legacyUser, TestContext.Current.CancellationToken))
                .ThrowsAsync(synchronizationException);

            var result = await context.UserProvisioningService.ProvisionAsync(CreateRequest(), TestContext.Current.CancellationToken);
            var currentUser = await context.CurrentUserContext.GetCurrentUserAsync(TestContext.Current.CancellationToken);

            Assert.Equal(UserProvisioningStatus.Failed, result.Status);
            Assert.Null(result.User);
            Assert.Null(currentUser);
            Assert.Contains(synchronizationException, context.Exceptions);

            context.VerifyUserNotAdded();
        }

        [Fact]
        public async Task ProvisionAsync_WhenConcurrentCreationWins_UsesPersistedConcurrentUserBySecurityIdentifier()
        {
            var concurrentUser = CreatePersistentUser(isActive: true);
            var sidLookupCount = 0;
            var context = CreateContext(setupDefaultLookup: false);

            context.UserRepository
                .Setup(repository => repository.GetBySecurityIdentifierAsync(SecurityIdentifier, TestContext.Current.CancellationToken))
                .ReturnsAsync(() => ++sidLookupCount == 1 ? null : concurrentUser);

            context.ConfigureLoginNameLookup(null);

            context.UserRepository
                .Setup(repository => repository.AddAsync(It.IsAny<User>(), TestContext.Current.CancellationToken))
                .ThrowsAsync(new InvalidOperationException("Unique constraint violation."));

            var result = await context.UserProvisioningService.ProvisionAsync(CreateRequest(), TestContext.Current.CancellationToken);
            var currentUser = await context.CurrentUserContext.GetCurrentUserAsync(TestContext.Current.CancellationToken);

            Assert.Equal(UserProvisioningStatus.UserAlreadyExists, result.Status);
            Assert.NotNull(result.User);
            Assert.Same(result.User, currentUser);
            Assert.Equal(UserId, currentUser?.Id);
            Assert.Equal(2, sidLookupCount);

            context.UserRepository.Verify(repository => repository.GetBySecurityIdentifierAsync(SecurityIdentifier, TestContext.Current.CancellationToken), Times.Exactly(2));
            context.VerifyLoginNameLookupOnce();
            context.UserRepository.Verify(repository => repository.AddAsync(It.IsAny<User>(), TestContext.Current.CancellationToken), Times.Once);
        }

        [Fact]
        public async Task ProvisionAsync_WhenConcurrentCreationCreatesDisabledUser_ReturnsUserDisabledWithoutPublishingContext()
        {
            var concurrentUser = CreatePersistentUser(isActive: false);
            var sidLookupCount = 0;
            var context = CreateContext(setupDefaultLookup: false);

            context.UserRepository
                .Setup(repository => repository.GetBySecurityIdentifierAsync(SecurityIdentifier, TestContext.Current.CancellationToken))
                .ReturnsAsync(() => ++sidLookupCount == 1 ? null : concurrentUser);

            context.ConfigureLoginNameLookup(null);

            context.UserRepository
                .Setup(repository => repository.AddAsync(It.IsAny<User>(), TestContext.Current.CancellationToken))
                .ThrowsAsync(new InvalidOperationException("Unique constraint violation."));

            var result = await context.UserProvisioningService.ProvisionAsync(CreateRequest(), TestContext.Current.CancellationToken);
            var currentUser = await context.CurrentUserContext.GetCurrentUserAsync(TestContext.Current.CancellationToken);

            Assert.Equal(UserProvisioningStatus.UserDisabled, result.Status);
            Assert.NotNull(result.User);
            Assert.False(result.User.IsActive);
            Assert.Null(currentUser);
            Assert.Equal(2, sidLookupCount);

            context.UserRepository.Verify(repository => repository.GetBySecurityIdentifierAsync(SecurityIdentifier, TestContext.Current.CancellationToken), Times.Exactly(2));
            context.VerifyLoginNameLookupOnce();
            context.UserRepository.Verify(repository => repository.AddAsync(It.IsAny<User>(), TestContext.Current.CancellationToken), Times.Once);
        }

        [Fact]
        public async Task ProvisionAsync_WhenCreationFailsWithoutConcurrentUser_ReturnsFailed()
        {
            var context = CreateContext(setupDefaultLookup: false);
            var sidLookupCount = 0;
            var loginNameLookupCount = 0;

            context.UserRepository
                .Setup(repository => repository.GetBySecurityIdentifierAsync(SecurityIdentifier, TestContext.Current.CancellationToken))
                .ReturnsAsync(() =>
                {
                    sidLookupCount++;
                    return null;
                });

            context.UserRepository
                .Setup(repository => repository.GetByLoginNameAsync(LoginName, TestContext.Current.CancellationToken))
                .ReturnsAsync(() =>
                {
                    loginNameLookupCount++;
                    return null;
                });

            context.UserRepository
                .Setup(repository => repository.AddAsync(It.IsAny<User>(), TestContext.Current.CancellationToken))
                .ThrowsAsync(new InvalidOperationException("Technical persistence failure."));

            var result = await context.UserProvisioningService.ProvisionAsync(CreateRequest(), TestContext.Current.CancellationToken);
            var currentUser = await context.CurrentUserContext.GetCurrentUserAsync(TestContext.Current.CancellationToken);

            Assert.Equal(UserProvisioningStatus.Failed, result.Status);
            Assert.False(result.IsSuccessful);
            Assert.Null(result.User);
            Assert.Null(currentUser);
            Assert.Equal(2, sidLookupCount);
            Assert.Equal(2, loginNameLookupCount);
        }

        [Fact]
        public async Task ProvisionAsync_WhenCreatedUserHasNoPersistentId_ReturnsFailedWithoutPublishingContext()
        {
            var context = CreateContext();

            context.UserRepository
                .Setup(repository => repository.AddAsync(It.IsAny<User>(), TestContext.Current.CancellationToken))
                .Returns(Task.CompletedTask);

            var result = await context.UserProvisioningService.ProvisionAsync(CreateRequest(), TestContext.Current.CancellationToken);
            var currentUser = await context.CurrentUserContext.GetCurrentUserAsync(TestContext.Current.CancellationToken);

            Assert.Equal(UserProvisioningStatus.Failed, result.Status);
            Assert.False(result.IsSuccessful);
            Assert.Null(result.User);
            Assert.Null(currentUser);
        }

        [Fact]
        public async Task ProvisionAsync_WhenCanceledBeforeValidation_PropagatesCancellationWithoutAccessingDependencies()
        {
            var context = CreateContext();
            using var cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);

            await cancellationSource.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => context.UserProvisioningService.ProvisionAsync(CreateRequest(), cancellationSource.Token));

            context.AdministrativeAuthorizationService.VerifyGet(service => service.IsElevatedAdministrator, Times.Never);
            context.OperatingSystemIdentityProvider.Verify(provider => provider.GetCurrentIdentityAsync(It.IsAny<CancellationToken>()), Times.Never);
            context.VerifyNoUserLookup();
            context.VerifyUserNotAdded();
        }

        [Fact]
        public async Task ProvisionAsync_WhenCreationIsCanceled_PropagatesCancellation()
        {
            var context = CreateContext(setupDefaultLookup: false);
            using var cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);

            context.OperatingSystemIdentityProvider
                .Setup(provider => provider.GetCurrentIdentityAsync(cancellationSource.Token))
                .ReturnsAsync(new OperatingSystemIdentity(LoginName, SecurityIdentifier));

            context.UserRepository
                .Setup(repository => repository.GetBySecurityIdentifierAsync(SecurityIdentifier, cancellationSource.Token))
                .ReturnsAsync((User?)null);

            context.UserRepository
                .Setup(repository => repository.GetByLoginNameAsync(LoginName, cancellationSource.Token))
                .ReturnsAsync((User?)null);

            context.UserRepository
                .Setup(repository => repository.HasPersonalUsersAsync(cancellationSource.Token))
                .ReturnsAsync(false);

            context.UserRepository
                .Setup(repository => repository.AddAsync(It.IsAny<User>(), cancellationSource.Token))
                .Returns((User _, CancellationToken token) =>
                {
                    cancellationSource.Cancel();
                    return Task.FromCanceled(token);
                });

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => context.UserProvisioningService.ProvisionAsync(CreateRequest(), cancellationSource.Token));

            var currentUser = await context.CurrentUserContext.GetCurrentUserAsync(TestContext.Current.CancellationToken);

            Assert.Null(currentUser);
        }

        [Fact]
        public async Task ProvisionAsync_WhenPreviousUserExistsAndProvisioningFails_ClearsPreviousContext()
        {
            var existingUser = CreatePersistentUser(isActive: true);
            var context = CreateContext(existingUser);

            var signInResult = await context.UserSignInService.SignInAsync(TestContext.Current.CancellationToken);

            Assert.Equal(UserSignInStatus.SignedIn, signInResult.Status);

            context.ConfigureSidLookup(null);
            context.ConfigureLoginNameLookup(null);

            context.UserRepository
                .Setup(repository => repository.AddAsync(It.IsAny<User>(), TestContext.Current.CancellationToken))
                .ThrowsAsync(new InvalidOperationException("Technical persistence failure."));

            var result = await context.UserProvisioningService.ProvisionAsync(CreateRequest(), TestContext.Current.CancellationToken);
            var currentUser = await context.CurrentUserContext.GetCurrentUserAsync(TestContext.Current.CancellationToken);

            Assert.Equal(UserProvisioningStatus.Failed, result.Status);
            Assert.Null(currentUser);
        }

        [Fact]
        public async Task ProvisionAsync_WhenAnotherPersonalUserExists_ReturnsFailedWithoutCreatingCurrentIdentity()
        {
            var context = CreateContext();

            context.UserRepository
                .Setup(repository => repository.HasPersonalUsersAsync(TestContext.Current.CancellationToken))
                .ReturnsAsync(true);

            var result = await context.UserProvisioningService.ProvisionAsync(CreateRequest(), TestContext.Current.CancellationToken);
            var currentUser = await context.CurrentUserContext.GetCurrentUserAsync(TestContext.Current.CancellationToken);

            Assert.Equal(UserProvisioningStatus.Failed, result.Status);
            Assert.False(result.IsSuccessful);
            Assert.NotNull(result.OperatingSystemIdentity);
            Assert.Equal(SecurityIdentifier, result.OperatingSystemIdentity.SecurityIdentifier);
            Assert.Null(result.User);
            Assert.Null(currentUser);

            context.VerifySidLookupOnce();
            context.VerifyLoginNameLookupOnce();
            context.UserRepository.Verify(repository => repository.HasPersonalUsersAsync(TestContext.Current.CancellationToken), Times.Once);
            context.VerifyUserNotAdded();
        }

        private static UserProvisioningTestContext CreateContext(User? existingUser = null, bool userRepositoryAvailable = true, bool setupDefaultLookup = true, bool isElevatedAdministrator = true, bool administrativeAuthorizationServiceAvailable = true) => new(existingUser, userRepositoryAvailable, setupDefaultLookup, isElevatedAdministrator, administrativeAuthorizationServiceAvailable);

        private static User CreatePersistentUser(bool isActive, string? securityIdentifier = SecurityIdentifier) => new()
        {
            Id = UserId,
            FirstName = FirstName,
            IsActive = isActive,
            LastName = LastName,
            LoginName = LoginName,
            SecurityIdentifier = securityIdentifier,
            ShortName = ShortName
        };

        private static UserProvisioningRequest CreateRequest() => new(FirstName, LastName, ShortName);

        private sealed class UserProvisioningTestContext
        {
            public UserProvisioningTestContext(User? existingUser, bool userRepositoryAvailable, bool setupDefaultLookup, bool isElevatedAdministrator, bool administrativeAuthorizationServiceAvailable)
            {
                AdministrativeAuthorizationService = new Mock<IAdministrativeAuthorizationService>(MockBehavior.Strict);
                AdministrativeAuthorizationService.SetupGet(service => service.IsElevatedAdministrator).Returns(isElevatedAdministrator);

                OperatingSystemIdentityProvider = new Mock<IOperatingSystemIdentityProvider>(MockBehavior.Strict);
                OperatingSystemIdentityProvider.Setup(provider => provider.GetCurrentIdentityAsync(TestContext.Current.CancellationToken)).ReturnsAsync(new OperatingSystemIdentity(LoginName, SecurityIdentifier));

                UserRepository = new Mock<IUserRepository>(MockBehavior.Strict);

                if (setupDefaultLookup)
                {
                    ConfigureDefaultLookups(existingUser);
                }

                /*
                 * The default scenario contains only the technical system identity.
                 * Tests for another personal user override this setup explicitly.
                 */
                UserRepository.Setup(repository => repository.HasPersonalUsersAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);

                Persistence = new Mock<IPersistence>(MockBehavior.Strict);
                Persistence.SetupGet(persistence => persistence.User).Returns(userRepositoryAvailable ? UserRepository.Object : null);

                Factory = administrativeAuthorizationServiceAvailable
                    ? new ApplicationFactory(AdministrativeAuthorizationService.Object, Persistence.Object, repository: null, OperatingSystemIdentityProvider.Object)
                    : new ApplicationFactory(Persistence.Object, repository: null, OperatingSystemIdentityProvider.Object);

                Application = Factory.Create();
                UserProvisioningService = Application.IdentityService?.UserProvisioningService ?? throw new InvalidOperationException("The user-provisioning service is not available.");
                UserSignInService = Application.IdentityService.UserSignInService ?? throw new InvalidOperationException("The user sign-in service is not available.");
                CurrentUserContext = Application.IdentityService.CurrentUserContext ?? throw new InvalidOperationException("The current-user context is not available.");

                Application.ExceptionThrown += exception => Exceptions.Add(exception);
            }

            public Mock<IAdministrativeAuthorizationService> AdministrativeAuthorizationService { get; }

            public IApplication Application { get; }

            public ICurrentUserContext CurrentUserContext { get; }

            public List<Exception> Exceptions { get; } = [];

            public ApplicationFactory Factory { get; }

            public Mock<IOperatingSystemIdentityProvider> OperatingSystemIdentityProvider { get; }

            public Mock<IPersistence> Persistence { get; }

            public Mock<IUserRepository> UserRepository { get; }

            public IUserProvisioningService UserProvisioningService { get; }

            public IUserSignInService UserSignInService { get; }

            public void ConfigureLoginNameLookup(User? user) => UserRepository.Setup(repository => repository.GetByLoginNameAsync(LoginName, TestContext.Current.CancellationToken)).ReturnsAsync(user);

            public void ConfigureSidLookup(User? user) => UserRepository.Setup(repository => repository.GetBySecurityIdentifierAsync(SecurityIdentifier, TestContext.Current.CancellationToken)).ReturnsAsync(user);

            public void VerifyLoginNameLookupNever() => UserRepository.Verify(repository => repository.GetByLoginNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);

            public void VerifyLoginNameLookupOnce() => UserRepository.Verify(repository => repository.GetByLoginNameAsync(LoginName, TestContext.Current.CancellationToken), Times.Once);

            public void VerifyNoUserLookup()
            {
                UserRepository.Verify(repository => repository.GetBySecurityIdentifierAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
                UserRepository.Verify(repository => repository.GetByLoginNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            }

            public void VerifySidLookupOnce() => UserRepository.Verify(repository => repository.GetBySecurityIdentifierAsync(SecurityIdentifier, TestContext.Current.CancellationToken), Times.Once);

            public void VerifyUserNotAdded() => UserRepository.Verify(repository => repository.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);

            public void VerifyUserNotUpdated() => UserRepository.Verify(repository => repository.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);

            private void ConfigureDefaultLookups(User? existingUser)
            {
                var userBySid = string.Equals(existingUser?.SecurityIdentifier, SecurityIdentifier, StringComparison.Ordinal) ? existingUser : null;
                var userByLoginName = existingUser is not null && string.IsNullOrWhiteSpace(existingUser.SecurityIdentifier) ? existingUser : null;

                ConfigureSidLookup(userBySid);
                ConfigureLoginNameLookup(userByLoginName);
            }
        }
    }
}