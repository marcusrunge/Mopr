using MarcusRunge.Mopr.Workbench.Contracts.Application.Identity;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Identity.Models;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Identity.Services;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Identity;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Entities;
using Moq;

namespace MarcusRunge.Mopr.Workbench.Services.Application.Test.Identity
{
    public sealed class UserSignInServiceTests
    {
        private const int UserId = 73;
        private const string FirstName = "Marcus";
        private const string LastName = "Runge";
        private const string LoginName = @"DOMAIN\User";
        private const string OtherSecurityIdentifier = "S-1-5-21-9000000000-8000000000-7000000000-1001";
        private const string PreviousLoginName = @"DOMAIN\PreviousUser";
        private const string SecurityIdentifier = "S-1-5-21-1000000000-2000000000-3000000000-1001";
        private const string ShortName = "MR";

        [Fact]
        public async Task SignInAsync_WhenActivePersistentUserExists_ReturnsSignedInAndPublishesCurrentUser()
        {
            var context = CreateContext(CreatePersistentUser(isActive: true));

            var result = await context.UserSignInService.SignInAsync(TestContext.Current.CancellationToken);
            var currentUser = await context.CurrentUserContext.GetCurrentUserAsync(TestContext.Current.CancellationToken);

            Assert.Equal(UserSignInStatus.SignedIn, result.Status);
            Assert.True(result.IsSignedIn);
            Assert.NotNull(result.OperatingSystemIdentity);
            Assert.Equal(LoginName, result.OperatingSystemIdentity.LoginName);
            Assert.Equal(SecurityIdentifier, result.OperatingSystemIdentity.SecurityIdentifier);
            Assert.NotNull(result.User);
            Assert.NotNull(currentUser);
            Assert.Same(result.User, currentUser);
            Assert.Equal(UserId, currentUser.Id);
            Assert.Equal(LoginName, currentUser.LoginName);
            Assert.Equal(SecurityIdentifier, currentUser.SecurityIdentifier);
            Assert.Equal(FirstName, currentUser.FirstName);
            Assert.Equal(LastName, currentUser.LastName);
            Assert.Equal(ShortName, currentUser.ShortName);
            Assert.Equal($"{FirstName} {LastName}", currentUser.DisplayName);
            Assert.True(currentUser.IsActive);

            context.OperatingSystemIdentityProvider.Verify(provider => provider.GetCurrentIdentityAsync(TestContext.Current.CancellationToken), Times.Once);
            context.VerifySidLookupOnce();
            context.VerifyLoginNameLookupNever();
            context.VerifyUserNotUpdated();
        }

        [Fact]
        public async Task SignInAsync_WhenOperatingSystemIdentityIsUnknown_ReturnsIdentityUnavailable()
        {
            var context = CreateContext();

            context.OperatingSystemIdentityProvider
                .Setup(provider => provider.GetCurrentIdentityAsync(TestContext.Current.CancellationToken))
                .ReturnsAsync((OperatingSystemIdentity?)null);

            var result = await context.UserSignInService.SignInAsync(TestContext.Current.CancellationToken);
            var currentUser = await context.CurrentUserContext.GetCurrentUserAsync(TestContext.Current.CancellationToken);

            Assert.Equal(UserSignInStatus.OperatingSystemIdentityUnavailable, result.Status);
            Assert.False(result.IsSignedIn);
            Assert.Null(result.OperatingSystemIdentity);
            Assert.Null(result.User);
            Assert.Null(currentUser);

            context.VerifyNoUserLookup();
        }

        [Fact]
        public async Task SignInAsync_WhenOperatingSystemIdentityHasNoSecurityIdentifier_ReturnsIdentityUnavailable()
        {
            var context = CreateContext(setupDefaultLookup: false);

            context.OperatingSystemIdentityProvider
                .Setup(provider => provider.GetCurrentIdentityAsync(TestContext.Current.CancellationToken))
                .ReturnsAsync(new OperatingSystemIdentity(LoginName));

            var result = await context.UserSignInService.SignInAsync(TestContext.Current.CancellationToken);
            var currentUser = await context.CurrentUserContext.GetCurrentUserAsync(TestContext.Current.CancellationToken);

            Assert.Equal(UserSignInStatus.OperatingSystemIdentityUnavailable, result.Status);
            Assert.False(result.IsSignedIn);
            Assert.Null(result.User);
            Assert.Null(currentUser);

            context.VerifyNoUserLookup();
        }

        [Fact]
        public async Task SignInAsync_WhenPersistentUserDoesNotExist_ReturnsUserUnknown()
        {
            var context = CreateContext(persistentUser: null);

            var result = await context.UserSignInService.SignInAsync(TestContext.Current.CancellationToken);
            var currentUser = await context.CurrentUserContext.GetCurrentUserAsync(TestContext.Current.CancellationToken);

            Assert.Equal(UserSignInStatus.UserUnknown, result.Status);
            Assert.False(result.IsSignedIn);
            Assert.NotNull(result.OperatingSystemIdentity);
            Assert.Equal(LoginName, result.OperatingSystemIdentity.LoginName);
            Assert.Equal(SecurityIdentifier, result.OperatingSystemIdentity.SecurityIdentifier);
            Assert.Null(result.User);
            Assert.Null(currentUser);

            context.VerifySidLookupOnce();
            context.VerifyLoginNameLookupOnce();
            context.VerifyUserNotUpdated();
        }

        [Fact]
        public async Task SignInAsync_WhenPersistentUserIsDisabled_ReturnsUserDisabledWithoutPublishingUser()
        {
            var context = CreateContext(CreatePersistentUser(isActive: false));

            var result = await context.UserSignInService.SignInAsync(TestContext.Current.CancellationToken);
            var currentUser = await context.CurrentUserContext.GetCurrentUserAsync(TestContext.Current.CancellationToken);

            Assert.Equal(UserSignInStatus.UserDisabled, result.Status);
            Assert.False(result.IsSignedIn);
            Assert.NotNull(result.User);
            Assert.Equal(UserId, result.User.Id);
            Assert.Equal(SecurityIdentifier, result.User.SecurityIdentifier);
            Assert.False(result.User.IsActive);
            Assert.Null(currentUser);

            context.VerifySidLookupOnce();
            context.VerifyLoginNameLookupNever();
            context.VerifyUserNotUpdated();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task SignInAsync_WhenPersistentUserIdIsInvalid_ReturnsInvalidPersistentUserId(int userId)
        {
            var persistentUser = CreatePersistentUser(isActive: true);
            persistentUser.Id = userId;

            var context = CreateContext(persistentUser);

            var result = await context.UserSignInService.SignInAsync(TestContext.Current.CancellationToken);
            var currentUser = await context.CurrentUserContext.GetCurrentUserAsync(TestContext.Current.CancellationToken);

            Assert.Equal(UserSignInStatus.InvalidPersistentUserId, result.Status);
            Assert.False(result.IsSignedIn);
            Assert.NotNull(result.OperatingSystemIdentity);
            Assert.Equal(SecurityIdentifier, result.OperatingSystemIdentity.SecurityIdentifier);
            Assert.Null(result.User);
            Assert.Null(currentUser);

            context.VerifySidLookupOnce();
            context.VerifyLoginNameLookupNever();
            context.VerifyUserNotUpdated();
        }

        [Fact]
        public async Task SignInAsync_WhenPersistenceUserRepositoryIsUnavailable_ReturnsPersistenceUnavailable()
        {
            var context = CreateContext(CreatePersistentUser(isActive: true), userRepositoryAvailable: false);

            var result = await context.UserSignInService.SignInAsync(TestContext.Current.CancellationToken);
            var currentUser = await context.CurrentUserContext.GetCurrentUserAsync(TestContext.Current.CancellationToken);

            Assert.Equal(UserSignInStatus.PersistenceUnavailable, result.Status);
            Assert.False(result.IsSignedIn);
            Assert.NotNull(result.OperatingSystemIdentity);
            Assert.Equal(LoginName, result.OperatingSystemIdentity.LoginName);
            Assert.Equal(SecurityIdentifier, result.OperatingSystemIdentity.SecurityIdentifier);
            Assert.Null(result.User);
            Assert.Null(currentUser);

            context.VerifyNoUserLookup();
        }

        [Theory]
        [InlineData(null, LastName, ShortName)]
        [InlineData("", LastName, ShortName)]
        [InlineData(" ", LastName, ShortName)]
        [InlineData(FirstName, null, ShortName)]
        [InlineData(FirstName, "", ShortName)]
        [InlineData(FirstName, " ", ShortName)]
        [InlineData(FirstName, LastName, null)]
        [InlineData(FirstName, LastName, "")]
        [InlineData(FirstName, LastName, " ")]
        public async Task SignInAsync_WhenPersistentUserNamesAreInvalid_ReturnsFailedWithoutPublishingUser(string? firstName, string? lastName, string? shortName)
        {
            var persistentUser = CreatePersistentUser(isActive: true);
            persistentUser.FirstName = firstName;
            persistentUser.LastName = lastName;
            persistentUser.ShortName = shortName;

            var context = CreateContext(persistentUser);

            var result = await context.UserSignInService.SignInAsync(TestContext.Current.CancellationToken);
            var currentUser = await context.CurrentUserContext.GetCurrentUserAsync(TestContext.Current.CancellationToken);

            Assert.Equal(UserSignInStatus.Failed, result.Status);
            Assert.False(result.IsSignedIn);
            Assert.Null(result.User);
            Assert.Null(currentUser);
            Assert.NotEmpty(context.Exceptions);

            context.VerifySidLookupOnce();
            context.VerifyLoginNameLookupNever();
            context.VerifyUserNotUpdated();
        }

        [Fact]
        public async Task SignInAsync_WhenPersistedNamesContainOuterWhitespace_PublishesNormalizedUser()
        {
            var persistentUser = CreatePersistentUser(isActive: true);
            persistentUser.FirstName = $"  {FirstName}  ";
            persistentUser.LastName = $"  {LastName}  ";
            persistentUser.ShortName = $"  {ShortName}  ";

            var context = CreateContext(persistentUser);

            var result = await context.UserSignInService.SignInAsync(TestContext.Current.CancellationToken);

            Assert.Equal(UserSignInStatus.SignedIn, result.Status);
            Assert.NotNull(result.User);
            Assert.Equal(FirstName, result.User.FirstName);
            Assert.Equal(LastName, result.User.LastName);
            Assert.Equal(ShortName, result.User.ShortName);
            Assert.Equal(SecurityIdentifier, result.User.SecurityIdentifier);

            context.VerifySidLookupOnce();
            context.VerifyLoginNameLookupNever();
        }

        [Fact]
        public async Task SignInAsync_WhenRepositorySidLookupFails_ReturnsFailedWithoutPublishingUser()
        {
            var lookupException = new InvalidOperationException("Technical persistence failure.");
            var context = CreateContext(setupDefaultLookup: false);

            context.UserRepository
                .Setup(repository => repository.GetBySecurityIdentifierAsync(SecurityIdentifier, TestContext.Current.CancellationToken))
                .ThrowsAsync(lookupException);

            var result = await context.UserSignInService.SignInAsync(TestContext.Current.CancellationToken);
            var currentUser = await context.CurrentUserContext.GetCurrentUserAsync(TestContext.Current.CancellationToken);

            Assert.Equal(UserSignInStatus.Failed, result.Status);
            Assert.False(result.IsSignedIn);
            Assert.Null(result.User);
            Assert.Null(currentUser);
            Assert.Contains(lookupException, context.Exceptions);

            context.VerifySidLookupOnce();
            context.VerifyLoginNameLookupNever();
        }

        [Fact]
        public async Task SignInAsync_WhenAlreadySignedInAndNextUserIsUnknown_ClearsPreviousContext()
        {
            var persistentUser = CreatePersistentUser(isActive: true);
            var context = CreateContext(setupDefaultLookup: false);

            context.UserRepository
                .SetupSequence(repository => repository.GetBySecurityIdentifierAsync(SecurityIdentifier, TestContext.Current.CancellationToken))
                .ReturnsAsync(persistentUser)
                .ReturnsAsync((User?)null);

            context.UserRepository
                .Setup(repository => repository.GetByLoginNameAsync(LoginName, TestContext.Current.CancellationToken))
                .ReturnsAsync((User?)null);

            var firstResult = await context.UserSignInService.SignInAsync(TestContext.Current.CancellationToken);

            Assert.Equal(UserSignInStatus.SignedIn, firstResult.Status);

            var firstCurrentUser = await context.CurrentUserContext.GetCurrentUserAsync(TestContext.Current.CancellationToken);

            Assert.NotNull(firstCurrentUser);

            var secondResult = await context.UserSignInService.SignInAsync(TestContext.Current.CancellationToken);
            var secondCurrentUser = await context.CurrentUserContext.GetCurrentUserAsync(TestContext.Current.CancellationToken);

            Assert.Equal(UserSignInStatus.UserUnknown, secondResult.Status);
            Assert.Null(secondCurrentUser);

            context.UserRepository.Verify(repository => repository.GetBySecurityIdentifierAsync(SecurityIdentifier, TestContext.Current.CancellationToken), Times.Exactly(2));
            context.VerifyLoginNameLookupOnce();
        }

        [Fact]
        public async Task SignInAsync_WhenCanceledBeforeResolution_DoesNotAccessOperatingSystemIdentity()
        {
            var context = CreateContext();
            using var cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);

            await cancellationSource.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => context.UserSignInService.SignInAsync(cancellationSource.Token));

            context.OperatingSystemIdentityProvider.Verify(provider => provider.GetCurrentIdentityAsync(It.IsAny<CancellationToken>()), Times.Never);
            context.VerifyNoUserLookup();
        }

        [Fact]
        public async Task SignInAsync_WhenIdentityResolutionIsCanceled_PropagatesCancellation()
        {
            var context = CreateContext();

            context.OperatingSystemIdentityProvider
                .Setup(provider => provider.GetCurrentIdentityAsync(TestContext.Current.CancellationToken))
                .ThrowsAsync(new OperationCanceledException(TestContext.Current.CancellationToken));

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => context.UserSignInService.SignInAsync(TestContext.Current.CancellationToken));

            context.VerifyNoUserLookup();
        }

        [Fact]
        public async Task SignInAsync_WhenRepositoryLookupIsCanceled_PropagatesCancellation()
        {
            var context = CreateContext(setupDefaultLookup: false);
            using var cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);

            context.OperatingSystemIdentityProvider
                .Setup(provider => provider.GetCurrentIdentityAsync(cancellationSource.Token))
                .ReturnsAsync(new OperatingSystemIdentity(LoginName, SecurityIdentifier));

            context.UserRepository
                .Setup(repository => repository.GetBySecurityIdentifierAsync(SecurityIdentifier, cancellationSource.Token))
                .Returns((string _, CancellationToken cancellationToken) =>
                {
                    /*
                     * Cancellation is requested from inside the repository boundary
                     * after identity resolution and before login-name fallback.
                     */
                    cancellationSource.Cancel();
                    return Task.FromCanceled<User?>(cancellationToken);
                });

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => context.UserSignInService.SignInAsync(cancellationSource.Token));

            context.OperatingSystemIdentityProvider.Verify(provider => provider.GetCurrentIdentityAsync(cancellationSource.Token), Times.Once);
            context.UserRepository.Verify(repository => repository.GetBySecurityIdentifierAsync(SecurityIdentifier, cancellationSource.Token), Times.Once);
            context.VerifyLoginNameLookupNever();

            var currentUser = await context.CurrentUserContext.GetCurrentUserAsync(TestContext.Current.CancellationToken);

            Assert.Null(currentUser);
        }

        [Fact]
        public async Task SeparateApplicationFactories_DoNotShareCurrentUserContext()
        {
            var firstContext = CreateContext(CreatePersistentUser(isActive: true));
            var secondContext = CreateContext(persistentUser: null);

            var firstResult = await firstContext.UserSignInService.SignInAsync(TestContext.Current.CancellationToken);
            var firstUser = await firstContext.CurrentUserContext.GetCurrentUserAsync(TestContext.Current.CancellationToken);
            var secondUser = await secondContext.CurrentUserContext.GetCurrentUserAsync(TestContext.Current.CancellationToken);

            Assert.Equal(UserSignInStatus.SignedIn, firstResult.Status);
            Assert.NotNull(firstUser);
            Assert.Equal(SecurityIdentifier, firstUser.SecurityIdentifier);
            Assert.Null(secondUser);
            Assert.NotSame(firstContext.Application, secondContext.Application);
            Assert.NotSame(firstContext.CurrentUserContext, secondContext.CurrentUserContext);
        }

        [Fact]
        public async Task SignInAsync_WhenLegacyUserExistsByLoginName_AssignsSecurityIdentifier()
        {
            var legacyUser = CreatePersistentUser(isActive: true, securityIdentifier: null);
            var context = CreateContext(legacyUser);

            context.UserRepository
                .Setup(repository => repository.UpdateAsync(legacyUser, TestContext.Current.CancellationToken))
                .Returns(Task.CompletedTask);

            var result = await context.UserSignInService.SignInAsync(TestContext.Current.CancellationToken);
            var currentUser = await context.CurrentUserContext.GetCurrentUserAsync(TestContext.Current.CancellationToken);

            Assert.Equal(UserSignInStatus.SignedIn, result.Status);
            Assert.Equal(SecurityIdentifier, legacyUser.SecurityIdentifier);
            Assert.Equal(SecurityIdentifier, result.User?.SecurityIdentifier);
            Assert.Equal(SecurityIdentifier, currentUser?.SecurityIdentifier);

            context.VerifySidLookupOnce();
            context.VerifyLoginNameLookupOnce();
            context.UserRepository.Verify(repository => repository.UpdateAsync(legacyUser, TestContext.Current.CancellationToken), Times.Once);
        }

        [Fact]
        public async Task SignInAsync_WhenLoginNameChangedButSecurityIdentifierMatches_UpdatesLoginName()
        {
            var persistentUser = CreatePersistentUser(isActive: true, loginName: PreviousLoginName);
            var context = CreateContext(persistentUser);

            context.UserRepository
                .Setup(repository => repository.UpdateAsync(persistentUser, TestContext.Current.CancellationToken))
                .Returns(Task.CompletedTask);

            var result = await context.UserSignInService.SignInAsync(TestContext.Current.CancellationToken);

            Assert.Equal(UserSignInStatus.SignedIn, result.Status);
            Assert.NotNull(result.User);
            Assert.Equal(LoginName, persistentUser.LoginName);
            Assert.Equal(LoginName, result.User.LoginName);
            Assert.Equal(SecurityIdentifier, result.User.SecurityIdentifier);

            context.VerifySidLookupOnce();
            context.VerifyLoginNameLookupNever();
            context.UserRepository.Verify(repository => repository.UpdateAsync(persistentUser, TestContext.Current.CancellationToken), Times.Once);
        }

        [Fact]
        public async Task SignInAsync_WhenLoginNameBelongsToAnotherSecurityIdentifier_ReturnsUserUnknownWithoutUpdatingUser()
        {
            var differentlyAssignedUser = CreatePersistentUser(isActive: true, securityIdentifier: OtherSecurityIdentifier);
            var context = CreateContext(setupDefaultLookup: false);

            context.ConfigureSidLookup(null);
            context.ConfigureLoginNameLookup(differentlyAssignedUser);

            var result = await context.UserSignInService.SignInAsync(TestContext.Current.CancellationToken);
            var currentUser = await context.CurrentUserContext.GetCurrentUserAsync(TestContext.Current.CancellationToken);

            Assert.Equal(UserSignInStatus.UserUnknown, result.Status);
            Assert.False(result.IsSignedIn);
            Assert.Null(result.User);
            Assert.Null(currentUser);
            Assert.Equal(OtherSecurityIdentifier, differentlyAssignedUser.SecurityIdentifier);

            context.VerifySidLookupOnce();
            context.VerifyLoginNameLookupOnce();
            context.VerifyUserNotUpdated();
        }

        [Fact]
        public async Task SignInAsync_WhenIdentitySynchronizationFails_ReturnsFailedWithoutPublishingUser()
        {
            var legacyUser = CreatePersistentUser(isActive: true, securityIdentifier: null);
            var updateException = new InvalidOperationException("Identity synchronization failed.");
            var context = CreateContext(legacyUser);

            context.UserRepository
                .Setup(repository => repository.UpdateAsync(legacyUser, TestContext.Current.CancellationToken))
                .ThrowsAsync(updateException);

            var result = await context.UserSignInService.SignInAsync(TestContext.Current.CancellationToken);
            var currentUser = await context.CurrentUserContext.GetCurrentUserAsync(TestContext.Current.CancellationToken);

            Assert.Equal(UserSignInStatus.Failed, result.Status);
            Assert.False(result.IsSignedIn);
            Assert.Null(result.User);
            Assert.Null(currentUser);
            Assert.Contains(updateException, context.Exceptions);

            context.UserRepository.Verify(repository => repository.UpdateAsync(legacyUser, TestContext.Current.CancellationToken), Times.Once);
        }

        private static UserSignInTestContext CreateContext(User? persistentUser = null, bool userRepositoryAvailable = true, bool setupDefaultLookup = true) => new(persistentUser, userRepositoryAvailable, setupDefaultLookup);

        private static User CreatePersistentUser(bool isActive, string loginName = LoginName, string? securityIdentifier = SecurityIdentifier) => new()
        {
            Id = UserId,
            FirstName = FirstName,
            IsActive = isActive,
            LastName = LastName,
            LoginName = loginName,
            SecurityIdentifier = securityIdentifier,
            ShortName = ShortName
        };

        private sealed class UserSignInTestContext
        {
            public UserSignInTestContext(User? persistentUser, bool userRepositoryAvailable, bool setupDefaultLookup)
            {
                OperatingSystemIdentityProvider = new Mock<IOperatingSystemIdentityProvider>(MockBehavior.Strict);

                OperatingSystemIdentityProvider
                    .Setup(provider => provider.GetCurrentIdentityAsync(TestContext.Current.CancellationToken))
                    .ReturnsAsync(new OperatingSystemIdentity(LoginName, SecurityIdentifier));

                UserRepository = new Mock<IUserRepository>(MockBehavior.Strict);

                if (userRepositoryAvailable && setupDefaultLookup)
                {
                    ConfigureDefaultLookups(persistentUser);
                }

                Persistence = new Mock<IPersistence>(MockBehavior.Strict);
                Persistence.SetupGet(instance => instance.User).Returns(userRepositoryAvailable ? UserRepository.Object : null);

                Factory = new ApplicationFactory(Persistence.Object, repository: null, OperatingSystemIdentityProvider.Object);
                Application = Factory.Create();
                UserSignInService = Application.IdentityService?.UserSignInService ?? throw new InvalidOperationException("The user sign-in service is not available.");
                CurrentUserContext = Application.IdentityService.CurrentUserContext ?? throw new InvalidOperationException("The current-user context is not available.");

                Application.ExceptionThrown += exception => Exceptions.Add(exception);
            }

            public IApplication Application { get; }

            public ICurrentUserContext CurrentUserContext { get; }

            public List<Exception> Exceptions { get; } = [];

            public ApplicationFactory Factory { get; }

            public Mock<IOperatingSystemIdentityProvider> OperatingSystemIdentityProvider { get; }

            public Mock<IPersistence> Persistence { get; }

            public Mock<IUserRepository> UserRepository { get; }

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

            public void VerifyUserNotUpdated() => UserRepository.Verify(repository => repository.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);

            private void ConfigureDefaultLookups(User? persistentUser)
            {
                var userBySid = string.Equals(persistentUser?.SecurityIdentifier, SecurityIdentifier, StringComparison.Ordinal) ? persistentUser : null;
                var userByLoginName = persistentUser is not null && string.IsNullOrWhiteSpace(persistentUser.SecurityIdentifier) ? persistentUser : null;

                ConfigureSidLookup(userBySid);
                ConfigureLoginNameLookup(userByLoginName);
            }
        }
    }
}