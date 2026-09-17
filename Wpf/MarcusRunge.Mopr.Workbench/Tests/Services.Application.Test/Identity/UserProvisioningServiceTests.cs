using MarcusRunge.Mopr.Workbench.Contracts.Application.Identity;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Identity.Models;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Identity.Services;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Security.Services;
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
        private const string ShortName = "MR";

        [Fact]
        public async Task ProvisionAsync_WhenRequestIsValid_CreatesAndPublishesPersistentUser()
        {
            var context = CreateContext();
            context.UserRepository
                .Setup(x => x.AddAsync(It.IsAny<User>(), TestContext.Current.CancellationToken))
                .Callback<User, CancellationToken>((user, _) => user.Id = UserId)
                .Returns(Task.CompletedTask);

            UserProvisioningResult result = await context.UserProvisioningService.ProvisionAsync(
                CreateRequest(),
                TestContext.Current.CancellationToken);

            CurrentUser? currentUser = await context.CurrentUserContext
                .GetCurrentUserAsync(TestContext.Current.CancellationToken);

            Assert.Equal(UserProvisioningStatus.Completed, result.Status);
            Assert.True(result.IsSuccessful);
            Assert.Empty(result.ValidationIssues);
            Assert.NotNull(result.OperatingSystemIdentity);
            Assert.Equal(LoginName, result.OperatingSystemIdentity.LoginName);
            Assert.NotNull(result.User);
            Assert.Same(result.User, currentUser);
            Assert.Equal(UserId, currentUser.Id);
            Assert.Equal(LoginName, currentUser.LoginName);
            Assert.Equal(FirstName, currentUser.FirstName);
            Assert.Equal(LastName, currentUser.LastName);
            Assert.Equal(ShortName, currentUser.ShortName);
            Assert.Equal($"{FirstName} {LastName}", currentUser.DisplayName);
            Assert.True(currentUser.IsActive);

            context.UserRepository.Verify(
                x => x.GetByLoginNameAsync(LoginName, TestContext.Current.CancellationToken),
                Times.Once);

            context.UserRepository.Verify(
                x => x.AddAsync(
                    It.Is<User>(user =>
                        user.LoginName == LoginName &&
                        user.FirstName == FirstName &&
                        user.LastName == LastName &&
                        user.ShortName == ShortName &&
                        user.IsActive),
                    TestContext.Current.CancellationToken),
                Times.Once);
        }

        [Fact]
        public async Task ProvisionAsync_WhenValuesContainWhitespace_NormalizesBeforePersistence()
        {
            var context = CreateContext();
            context.UserRepository
                .Setup(x => x.AddAsync(It.IsAny<User>(), TestContext.Current.CancellationToken))
                .Callback<User, CancellationToken>((user, _) => user.Id = UserId)
                .Returns(Task.CompletedTask);

            var request = new UserProvisioningRequest(
                $"  Marcus   Alexander  ",
                $"  Runge  ",
                $"  M   R  ");

            UserProvisioningResult result = await context.UserProvisioningService.ProvisionAsync(
                request,
                TestContext.Current.CancellationToken);

            Assert.Equal(UserProvisioningStatus.Completed, result.Status);
            Assert.NotNull(result.User);
            Assert.Equal("Marcus Alexander", result.User.FirstName);
            Assert.Equal("Runge", result.User.LastName);
            Assert.Equal("M R", result.User.ShortName);

            context.UserRepository.Verify(
                x => x.AddAsync(
                    It.Is<User>(user =>
                        user.FirstName == "Marcus Alexander" &&
                        user.LastName == "Runge" &&
                        user.ShortName == "M R"),
                    TestContext.Current.CancellationToken),
                Times.Once);
        }

        [Fact]
        public async Task ProvisionAsync_WhenRequiredValuesAreMissing_ReturnsAllValidationIssues()
        {
            var context = CreateContext();
            var request = new UserProvisioningRequest(" ", null, "\t");

            UserProvisioningResult result = await context.UserProvisioningService.ProvisionAsync(
                request,
                TestContext.Current.CancellationToken);

            Assert.Equal(UserProvisioningStatus.ValidationFailed, result.Status);
            Assert.False(result.IsSuccessful);
            Assert.Null(result.OperatingSystemIdentity);
            Assert.Null(result.User);
            Assert.Contains(UserProvisioningValidationIssue.FirstNameRequired, result.ValidationIssues);
            Assert.Contains(UserProvisioningValidationIssue.LastNameRequired, result.ValidationIssues);
            Assert.Contains(UserProvisioningValidationIssue.ShortNameRequired, result.ValidationIssues);
            Assert.Equal(3, result.ValidationIssues.Count);

            context.OperatingSystemIdentityProvider.Verify(
                x => x.GetCurrentIdentityAsync(It.IsAny<CancellationToken>()),
                Times.Never);

            context.UserRepository.Verify(
                x => x.GetByLoginNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never);

            context.UserRepository.Verify(
                x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ProvisionAsync_WhenValuesExceedPersistenceLimits_ReturnsLengthValidationIssues()
        {
            var context = CreateContext();
            var request = new UserProvisioningRequest(
                new string('F', 257),
                new string('L', 257),
                new string('S', 65));

            UserProvisioningResult result = await context.UserProvisioningService.ProvisionAsync(
                request,
                TestContext.Current.CancellationToken);

            Assert.Equal(UserProvisioningStatus.ValidationFailed, result.Status);
            Assert.Contains(UserProvisioningValidationIssue.FirstNameTooLong, result.ValidationIssues);
            Assert.Contains(UserProvisioningValidationIssue.LastNameTooLong, result.ValidationIssues);
            Assert.Contains(UserProvisioningValidationIssue.ShortNameTooLong, result.ValidationIssues);
            Assert.Equal(3, result.ValidationIssues.Count);

            context.OperatingSystemIdentityProvider.Verify(
                x => x.GetCurrentIdentityAsync(It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ProvisionAsync_WhenShortNameContainsControlCharacter_ReturnsValidationFailure()
        {
            var context = CreateContext();
            var request = new UserProvisioningRequest(FirstName, LastName, "M\u0001R");

            UserProvisioningResult result = await context.UserProvisioningService.ProvisionAsync(
                request,
                TestContext.Current.CancellationToken);

            Assert.Equal(UserProvisioningStatus.ValidationFailed, result.Status);
            Assert.Contains(UserProvisioningValidationIssue.ShortNameInvalid, result.ValidationIssues);

            context.UserRepository.Verify(
                x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ProvisionAsync_WhenOperatingSystemIdentityIsUnavailable_DoesNotCreateUser()
        {
            var context = CreateContext();
            context.OperatingSystemIdentityProvider
                .Setup(x => x.GetCurrentIdentityAsync(TestContext.Current.CancellationToken))
                .ReturnsAsync((OperatingSystemIdentity?)null);

            UserProvisioningResult result = await context.UserProvisioningService.ProvisionAsync(
                CreateRequest(),
                TestContext.Current.CancellationToken);

            CurrentUser? currentUser = await context.CurrentUserContext
                .GetCurrentUserAsync(TestContext.Current.CancellationToken);

            Assert.Equal(UserProvisioningStatus.OperatingSystemIdentityUnavailable, result.Status);
            Assert.False(result.IsSuccessful);
            Assert.Null(result.OperatingSystemIdentity);
            Assert.Null(result.User);
            Assert.Null(currentUser);

            context.UserRepository.Verify(
                x => x.GetByLoginNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never);

            context.UserRepository.Verify(
                x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ProvisionAsync_WhenPersistenceUserRepositoryIsUnavailable_ReturnsPersistenceUnavailable()
        {
            var context = CreateContext(userRepositoryAvailable: false);

            UserProvisioningResult result = await context.UserProvisioningService.ProvisionAsync(
                CreateRequest(),
                TestContext.Current.CancellationToken);

            CurrentUser? currentUser = await context.CurrentUserContext
                .GetCurrentUserAsync(TestContext.Current.CancellationToken);

            Assert.Equal(UserProvisioningStatus.PersistenceUnavailable, result.Status);
            Assert.False(result.IsSuccessful);
            Assert.NotNull(result.OperatingSystemIdentity);
            Assert.Null(result.User);
            Assert.Null(currentUser);
        }

        [Fact]
        public async Task ProvisionAsync_WhenActiveUserAlreadyExists_ReturnsExistingUserAndPublishesContext()
        {
            var existingUser = CreatePersistentUser(isActive: true);
            var context = CreateContext(existingUser);

            UserProvisioningResult result = await context.UserProvisioningService.ProvisionAsync(
                CreateRequest(),
                TestContext.Current.CancellationToken);

            CurrentUser? currentUser = await context.CurrentUserContext
                .GetCurrentUserAsync(TestContext.Current.CancellationToken);

            Assert.Equal(UserProvisioningStatus.UserAlreadyExists, result.Status);
            Assert.False(result.IsSuccessful);
            Assert.NotNull(result.User);
            Assert.Same(result.User, currentUser);
            Assert.Equal(UserId, currentUser.Id);
            Assert.True(currentUser.IsActive);

            context.UserRepository.Verify(
                x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ProvisionAsync_WhenDisabledUserAlreadyExists_ReturnsExistingUserWithoutPublishingContext()
        {
            var existingUser = CreatePersistentUser(isActive: false);
            var context = CreateContext(existingUser);

            UserProvisioningResult result = await context.UserProvisioningService.ProvisionAsync(
                CreateRequest(),
                TestContext.Current.CancellationToken);

            CurrentUser? currentUser = await context.CurrentUserContext
                .GetCurrentUserAsync(TestContext.Current.CancellationToken);

            Assert.Equal(UserProvisioningStatus.UserAlreadyExists, result.Status);
            Assert.False(result.IsSuccessful);
            Assert.NotNull(result.User);
            Assert.False(result.User.IsActive);
            Assert.Null(currentUser);

            context.UserRepository.Verify(
                x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ProvisionAsync_WhenConcurrentCreationWins_UsesPersistedConcurrentUser()
        {
            var concurrentUser = CreatePersistentUser(isActive: true);
            var lookupCount = 0;
            var context = CreateContext(setupDefaultLookup: false);

            context.UserRepository
                .Setup(x => x.GetByLoginNameAsync(LoginName, TestContext.Current.CancellationToken))
                .ReturnsAsync(() => ++lookupCount == 1 ? null : concurrentUser);

            context.UserRepository
                .Setup(x => x.AddAsync(It.IsAny<User>(), TestContext.Current.CancellationToken))
                .ThrowsAsync(new InvalidOperationException("Unique constraint violation."));

            UserProvisioningResult result = await context.UserProvisioningService.ProvisionAsync(
                CreateRequest(),
                TestContext.Current.CancellationToken);

            CurrentUser? currentUser = await context.CurrentUserContext
                .GetCurrentUserAsync(TestContext.Current.CancellationToken);

            Assert.Equal(UserProvisioningStatus.UserAlreadyExists, result.Status);
            Assert.NotNull(result.User);
            Assert.Same(result.User, currentUser);
            Assert.Equal(UserId, currentUser.Id);
            Assert.Equal(2, lookupCount);

            context.UserRepository.Verify(
                x => x.GetByLoginNameAsync(LoginName, TestContext.Current.CancellationToken),
                Times.Exactly(2));

            context.UserRepository.Verify(
                x => x.AddAsync(It.IsAny<User>(), TestContext.Current.CancellationToken),
                Times.Once);
        }

        [Fact]
        public async Task ProvisionAsync_WhenCreationFailsWithoutConcurrentUser_ReturnsFailed()
        {
            var context = CreateContext(setupDefaultLookup: false);
            var lookupCount = 0;

            context.UserRepository
                .Setup(x => x.GetByLoginNameAsync(LoginName, TestContext.Current.CancellationToken))
                .ReturnsAsync(() =>
                {
                    lookupCount++;
                    return null;
                });

            context.UserRepository
                .Setup(x => x.AddAsync(It.IsAny<User>(), TestContext.Current.CancellationToken))
                .ThrowsAsync(new InvalidOperationException("Technical persistence failure."));

            UserProvisioningResult result = await context.UserProvisioningService.ProvisionAsync(
                CreateRequest(),
                TestContext.Current.CancellationToken);

            CurrentUser? currentUser = await context.CurrentUserContext
                .GetCurrentUserAsync(TestContext.Current.CancellationToken);

            Assert.Equal(UserProvisioningStatus.Failed, result.Status);
            Assert.False(result.IsSuccessful);
            Assert.Null(result.User);
            Assert.Null(currentUser);
            Assert.Equal(2, lookupCount);
        }

        [Fact]
        public async Task ProvisionAsync_WhenCreatedUserHasNoPersistentId_ReturnsFailedWithoutPublishingContext()
        {
            var context = CreateContext();
            context.UserRepository
                .Setup(x => x.AddAsync(It.IsAny<User>(), TestContext.Current.CancellationToken))
                .Returns(Task.CompletedTask);

            UserProvisioningResult result = await context.UserProvisioningService.ProvisionAsync(
                CreateRequest(),
                TestContext.Current.CancellationToken);

            CurrentUser? currentUser = await context.CurrentUserContext
                .GetCurrentUserAsync(TestContext.Current.CancellationToken);

            Assert.Equal(UserProvisioningStatus.Failed, result.Status);
            Assert.False(result.IsSuccessful);
            Assert.Null(result.User);
            Assert.Null(currentUser);
        }

        [Fact]
        public async Task ProvisionAsync_WhenCanceledBeforeValidation_PropagatesCancellationWithoutAccessingDependencies()
        {
            var context = CreateContext();
            using var cancellation = new CancellationTokenSource();
            await cancellation.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => context.UserProvisioningService.ProvisionAsync(CreateRequest(), cancellation.Token));

            context.OperatingSystemIdentityProvider.Verify(
                x => x.GetCurrentIdentityAsync(It.IsAny<CancellationToken>()),
                Times.Never);

            context.UserRepository.Verify(
                x => x.GetByLoginNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never);

            context.UserRepository.Verify(
                x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ProvisionAsync_WhenCreationIsCanceled_PropagatesCancellation()
        {
            var context = CreateContext();
            using var cancellation = new CancellationTokenSource();

            context.OperatingSystemIdentityProvider
                .Setup(x => x.GetCurrentIdentityAsync(cancellation.Token))
                .ReturnsAsync(new OperatingSystemIdentity(LoginName));

            context.UserRepository
                .Setup(x => x.GetByLoginNameAsync(LoginName, cancellation.Token))
                .ReturnsAsync((User?)null);

            context.UserRepository
                .Setup(x => x.AddAsync(It.IsAny<User>(), cancellation.Token))
                .Returns((User _, CancellationToken token) =>
                {
                    // Cancellation occurs only after validation, identity
                    // resolution and the initial uniqueness lookup succeeded.
                    cancellation.Cancel();
                    return Task.FromCanceled(token);
                });

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => context.UserProvisioningService.ProvisionAsync(CreateRequest(), cancellation.Token));

            CurrentUser? currentUser = await context.CurrentUserContext
                .GetCurrentUserAsync(TestContext.Current.CancellationToken);

            Assert.Null(currentUser);
        }

        [Fact]
        public async Task ProvisionAsync_WhenPreviousUserExistsAndProvisioningFails_ClearsPreviousContext()
        {
            var existingUser = CreatePersistentUser(isActive: true);
            var context = CreateContext(existingUser);

            UserSignInResult signInResult = await context.UserSignInService
                .SignInAsync(TestContext.Current.CancellationToken);

            Assert.Equal(UserSignInStatus.SignedIn, signInResult.Status);

            context.UserRepository
                .Setup(x => x.GetByLoginNameAsync(LoginName, TestContext.Current.CancellationToken))
                .ReturnsAsync((User?)null);

            context.UserRepository
                .Setup(x => x.AddAsync(It.IsAny<User>(), TestContext.Current.CancellationToken))
                .ThrowsAsync(new InvalidOperationException("Technical persistence failure."));

            UserProvisioningResult result = await context.UserProvisioningService.ProvisionAsync(
                CreateRequest(),
                TestContext.Current.CancellationToken);

            CurrentUser? currentUser = await context.CurrentUserContext
                .GetCurrentUserAsync(TestContext.Current.CancellationToken);

            Assert.Equal(UserProvisioningStatus.Failed, result.Status);
            Assert.Null(currentUser);
        }

        private static UserProvisioningTestContext CreateContext(
            User? existingUser = null,
            bool userRepositoryAvailable = true,
            bool setupDefaultLookup = true) =>
            new(existingUser, userRepositoryAvailable, setupDefaultLookup);

        private static User CreatePersistentUser(bool isActive) => new()
        {
            Id = UserId,
            FirstName = FirstName,
            LastName = LastName,
            LoginName = LoginName,
            ShortName = ShortName,
            IsActive = isActive
        };

        private static UserProvisioningRequest CreateRequest() =>
            new(FirstName, LastName, ShortName);

        private sealed class UserProvisioningTestContext
        {
            public UserProvisioningTestContext(
                User? existingUser,
                bool userRepositoryAvailable,
                bool setupDefaultLookup)
            {
                OperatingSystemIdentityProvider = new Mock<IOperatingSystemIdentityProvider>(MockBehavior.Strict);
                OperatingSystemIdentityProvider
                    .Setup(x => x.GetCurrentIdentityAsync(TestContext.Current.CancellationToken))
                    .ReturnsAsync(new OperatingSystemIdentity(LoginName));

                UserRepository = new Mock<IUserRepository>(MockBehavior.Strict);

                if (userRepositoryAvailable && setupDefaultLookup)
                {
                    UserRepository
                        .Setup(x => x.GetByLoginNameAsync(LoginName, TestContext.Current.CancellationToken))
                        .ReturnsAsync(existingUser);
                }

                Persistence = new Mock<IPersistence>(MockBehavior.Strict);
                Persistence
                    .SetupGet(x => x.User)
                    .Returns(userRepositoryAvailable ? UserRepository.Object : (IUserRepository?)null);

                Factory = new ApplicationFactory(
                    auditIdentityProvider: Mock.Of<IAuditIdentityProvider>(),
                    persistence: Persistence.Object,
                    repository: null,
                    operatingSystemIdentityProvider: OperatingSystemIdentityProvider.Object);

                Application = Factory.Create();

                UserProvisioningService = Application.IdentityService?.UserProvisioningService
                    ?? throw new InvalidOperationException("The user-provisioning service is not available.");

                UserSignInService = Application.IdentityService.UserSignInService
                    ?? throw new InvalidOperationException("The user sign-in service is not available.");

                CurrentUserContext = Application.IdentityService.CurrentUserContext
                    ?? throw new InvalidOperationException("The current-user context is not available.");
            }

            public IApplication Application { get; }

            public ICurrentUserContext CurrentUserContext { get; }

            public ApplicationFactory Factory { get; }

            public Mock<IOperatingSystemIdentityProvider> OperatingSystemIdentityProvider { get; }

            public Mock<IPersistence> Persistence { get; }

            public Mock<IUserRepository> UserRepository { get; }

            public IUserProvisioningService UserProvisioningService { get; }

            public IUserSignInService UserSignInService { get; }
        }
    }
}