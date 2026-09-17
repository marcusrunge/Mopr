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
    public sealed class UserSignInServiceTests
    {
        private const int UserId = 73;
        private const string FirstName = "Marcus";
        private const string LastName = "Runge";
        private const string LoginName = @"DOMAIN\User";
        private const string ShortName = "MR";

        [Fact]
        public async Task SignInAsync_WhenActivePersistentUserExists_ReturnsSignedInAndPublishesCurrentUser()
        {
            var context = CreateContext(CreatePersistentUser(isActive: true));

            UserSignInResult result = await context.UserSignInService.SignInAsync(TestContext.Current.CancellationToken);
            CurrentUser? currentUser = await context.CurrentUserContext.GetCurrentUserAsync(TestContext.Current.CancellationToken);

            Assert.Equal(UserSignInStatus.SignedIn, result.Status);
            Assert.True(result.IsSignedIn);
            Assert.NotNull(result.User);
            Assert.NotNull(currentUser);
            Assert.Same(result.User, currentUser);
            Assert.Equal(UserId, currentUser.Id);
            Assert.Equal(LoginName, currentUser.LoginName);
            Assert.Equal(FirstName, currentUser.FirstName);
            Assert.Equal(LastName, currentUser.LastName);
            Assert.Equal(ShortName, currentUser.ShortName);
            Assert.Equal($"{FirstName} {LastName}", currentUser.DisplayName);
            Assert.True(currentUser.IsActive);

            context.OperatingSystemIdentityProvider.Verify(x => x.GetCurrentIdentityAsync(TestContext.Current.CancellationToken), Times.Once);

            context.UserRepository.Verify(x => x.GetByLoginNameAsync(LoginName, TestContext.Current.CancellationToken), Times.Once);
        }

        [Fact]
        public async Task SignInAsync_WhenOperatingSystemIdentityIsUnknown_ReturnsIdentityUnavailable()
        {
            var context = CreateContext();
            context.OperatingSystemIdentityProvider.Setup(x => x.GetCurrentIdentityAsync(TestContext.Current.CancellationToken)).ReturnsAsync((OperatingSystemIdentity?)null);

            UserSignInResult result = await context.UserSignInService.SignInAsync(TestContext.Current.CancellationToken);
            CurrentUser? currentUser = await context.CurrentUserContext.GetCurrentUserAsync(TestContext.Current.CancellationToken);

            Assert.Equal(UserSignInStatus.OperatingSystemIdentityUnavailable, result.Status);
            Assert.False(result.IsSignedIn);
            Assert.Null(result.OperatingSystemIdentity);
            Assert.Null(result.User);
            Assert.Null(currentUser);

            context.UserRepository.Verify(x => x.GetByLoginNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task SignInAsync_WhenPersistentUserDoesNotExist_ReturnsUserUnknown()
        {
            var context = CreateContext(persistentUser: null);

            UserSignInResult result = await context.UserSignInService.SignInAsync(TestContext.Current.CancellationToken);
            CurrentUser? currentUser = await context.CurrentUserContext.GetCurrentUserAsync(TestContext.Current.CancellationToken);

            Assert.Equal(UserSignInStatus.UserUnknown, result.Status);
            Assert.False(result.IsSignedIn);
            Assert.NotNull(result.OperatingSystemIdentity);
            Assert.Equal(LoginName, result.OperatingSystemIdentity.LoginName);
            Assert.Null(result.User);
            Assert.Null(currentUser);
        }

        [Fact]
        public async Task SignInAsync_WhenPersistentUserIsDisabled_ReturnsUserDisabledWithoutPublishingUser()
        {
            var context = CreateContext(CreatePersistentUser(isActive: false));

            UserSignInResult result = await context.UserSignInService.SignInAsync(TestContext.Current.CancellationToken);
            CurrentUser? currentUser = await context.CurrentUserContext.GetCurrentUserAsync(TestContext.Current.CancellationToken);

            Assert.Equal(UserSignInStatus.UserDisabled, result.Status);
            Assert.False(result.IsSignedIn);
            Assert.NotNull(result.User);
            Assert.Equal(UserId, result.User.Id);
            Assert.False(result.User.IsActive);
            Assert.Null(currentUser);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task SignInAsync_WhenPersistentUserIdIsInvalid_ReturnsInvalidPersistentUserId(int userId)
        {
            var persistentUser = CreatePersistentUser(isActive: true);
            persistentUser.Id = userId;
            var context = CreateContext(persistentUser);

            UserSignInResult result = await context.UserSignInService.SignInAsync(TestContext.Current.CancellationToken);
            CurrentUser? currentUser = await context.CurrentUserContext.GetCurrentUserAsync(TestContext.Current.CancellationToken);

            Assert.Equal(UserSignInStatus.InvalidPersistentUserId, result.Status);
            Assert.False(result.IsSignedIn);
            Assert.NotNull(result.OperatingSystemIdentity);
            Assert.Null(result.User);
            Assert.Null(currentUser);
        }

        [Fact]
        public async Task SignInAsync_WhenPersistenceUserRepositoryIsUnavailable_ReturnsPersistenceUnavailable()
        {
            var context = CreateContext(CreatePersistentUser(isActive: true), userRepositoryAvailable: false);

            UserSignInResult result = await context.UserSignInService.SignInAsync(TestContext.Current.CancellationToken);
            CurrentUser? currentUser = await context.CurrentUserContext.GetCurrentUserAsync(TestContext.Current.CancellationToken);

            Assert.Equal(UserSignInStatus.PersistenceUnavailable, result.Status);
            Assert.False(result.IsSignedIn);
            Assert.NotNull(result.OperatingSystemIdentity);
            Assert.Null(result.User);
            Assert.Null(currentUser);
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

            UserSignInResult result = await context.UserSignInService.SignInAsync(TestContext.Current.CancellationToken);
            CurrentUser? currentUser = await context.CurrentUserContext.GetCurrentUserAsync(TestContext.Current.CancellationToken);

            Assert.Equal(UserSignInStatus.Failed, result.Status);
            Assert.False(result.IsSignedIn);
            Assert.Null(result.User);
            Assert.Null(currentUser);
        }

        [Fact]
        public async Task SignInAsync_WhenPersistedNamesContainOuterWhitespace_PublishesNormalizedUser()
        {
            var persistentUser = CreatePersistentUser(isActive: true);
            persistentUser.FirstName = $"  {FirstName}  ";
            persistentUser.LastName = $"  {LastName}  ";
            persistentUser.ShortName = $"  {ShortName}  ";
            var context = CreateContext(persistentUser);

            UserSignInResult result = await context.UserSignInService.SignInAsync(TestContext.Current.CancellationToken);

            Assert.Equal(UserSignInStatus.SignedIn, result.Status);
            Assert.NotNull(result.User);
            Assert.Equal(FirstName, result.User.FirstName);
            Assert.Equal(LastName, result.User.LastName);
            Assert.Equal(ShortName, result.User.ShortName);
        }

        [Fact]
        public async Task SignInAsync_WhenRepositoryLookupFails_ReturnsFailedWithoutPublishingUser()
        {
            var context = CreateContext();
            context.UserRepository.Setup(x => x.GetByLoginNameAsync(LoginName, TestContext.Current.CancellationToken)).ThrowsAsync(new InvalidOperationException("Technical persistence failure."));

            UserSignInResult result = await context.UserSignInService.SignInAsync(TestContext.Current.CancellationToken);
            CurrentUser? currentUser = await context.CurrentUserContext.GetCurrentUserAsync(TestContext.Current.CancellationToken);

            Assert.Equal(UserSignInStatus.Failed, result.Status);
            Assert.False(result.IsSignedIn);
            Assert.Null(result.User);
            Assert.Null(currentUser);
        }

        [Fact]
        public async Task SignInAsync_WhenAlreadySignedInAndNextUserIsUnknown_ClearsPreviousContext()
        {
            var context = CreateContext(CreatePersistentUser(isActive: true));

            UserSignInResult firstResult = await context.UserSignInService.SignInAsync(TestContext.Current.CancellationToken);
            Assert.Equal(UserSignInStatus.SignedIn, firstResult.Status);

            context.UserRepository.Setup(x => x.GetByLoginNameAsync(LoginName, TestContext.Current.CancellationToken)).ReturnsAsync((User?)null);

            UserSignInResult secondResult = await context.UserSignInService.SignInAsync(TestContext.Current.CancellationToken);
            CurrentUser? currentUser = await context.CurrentUserContext.GetCurrentUserAsync(TestContext.Current.CancellationToken);

            Assert.Equal(UserSignInStatus.UserUnknown, secondResult.Status);
            Assert.Null(currentUser);
        }

        [Fact]
        public async Task SignInAsync_WhenCanceledBeforeResolution_DoesNotAccessOperatingSystemIdentity()
        {
            var context = CreateContext();
            using var cancellation = new CancellationTokenSource();
            await cancellation.CancelAsync();

            await Assert.ThrowsAsync<OperationCanceledException>(() => context.UserSignInService.SignInAsync(cancellation.Token));

            context.OperatingSystemIdentityProvider.Verify(x => x.GetCurrentIdentityAsync(It.IsAny<CancellationToken>()), Times.Never);

            context.UserRepository.Verify(x => x.GetByLoginNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task SignInAsync_WhenIdentityResolutionIsCanceled_PropagatesCancellation()
        {
            var context = CreateContext();
            context.OperatingSystemIdentityProvider.Setup(x => x.GetCurrentIdentityAsync(TestContext.Current.CancellationToken)).ThrowsAsync(new OperationCanceledException(TestContext.Current.CancellationToken));

            await Assert.ThrowsAsync<OperationCanceledException>(() => context.UserSignInService.SignInAsync(TestContext.Current.CancellationToken));

            context.UserRepository.Verify(x => x.GetByLoginNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task SignInAsync_WhenRepositoryLookupIsCanceled_PropagatesCancellation()
        {
            var context = CreateContext();
            using var cancellation = new CancellationTokenSource();

            context.OperatingSystemIdentityProvider.Setup(x => x.GetCurrentIdentityAsync(cancellation.Token)).ReturnsAsync(new OperatingSystemIdentity(LoginName));

            context.UserRepository.Setup(x => x.GetByLoginNameAsync(LoginName, cancellation.Token)).Returns((string _, CancellationToken token) =>
            {
                // Cancellation is requested inside the repository operation so the
                // sign-in workflow has already reached the Persistence boundary.
                cancellation.Cancel();
                return Task.FromCanceled<User?>(token);
            });

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => context.UserSignInService.SignInAsync(cancellation.Token));
            context.OperatingSystemIdentityProvider.Verify(x => x.GetCurrentIdentityAsync(cancellation.Token), Times.Once);

            context.UserRepository.Verify(x => x.GetByLoginNameAsync(LoginName, cancellation.Token), Times.Once);

            CurrentUser? currentUser = await context.CurrentUserContext.GetCurrentUserAsync(TestContext.Current.CancellationToken);

            Assert.Null(currentUser);
        }

        [Fact]
        public async Task SeparateApplicationFactories_DoNotShareCurrentUserContext()
        {
            var firstContext = CreateContext(CreatePersistentUser(isActive: true));
            var secondContext = CreateContext(persistentUser: null);

            UserSignInResult firstResult = await firstContext.UserSignInService.SignInAsync(TestContext.Current.CancellationToken);
            CurrentUser? firstUser = await firstContext.CurrentUserContext.GetCurrentUserAsync(TestContext.Current.CancellationToken);
            CurrentUser? secondUser = await secondContext.CurrentUserContext.GetCurrentUserAsync(TestContext.Current.CancellationToken);

            Assert.Equal(UserSignInStatus.SignedIn, firstResult.Status);
            Assert.NotNull(firstUser);
            Assert.Null(secondUser);
            Assert.NotSame(firstContext.Application, secondContext.Application);
            Assert.NotSame(firstContext.CurrentUserContext, secondContext.CurrentUserContext);
        }

        private static UserSignInTestContext CreateContext(User? persistentUser = null, bool userRepositoryAvailable = true) => new(persistentUser, userRepositoryAvailable);

        private static User CreatePersistentUser(bool isActive) => new()
        {
            Id = UserId,
            FirstName = FirstName,
            IsActive = isActive,
            LastName = LastName,
            LoginName = LoginName,
            ShortName = ShortName
        };

        private sealed class UserSignInTestContext
        {
            public UserSignInTestContext(User? persistentUser, bool userRepositoryAvailable)
            {
                OperatingSystemIdentityProvider = new Mock<IOperatingSystemIdentityProvider>(MockBehavior.Strict);
                OperatingSystemIdentityProvider.Setup(x => x.GetCurrentIdentityAsync(TestContext.Current.CancellationToken)).ReturnsAsync(new OperatingSystemIdentity(LoginName));

                UserRepository = new Mock<IUserRepository>(MockBehavior.Strict);

                if (userRepositoryAvailable)
                {
                    UserRepository.Setup(x => x.GetByLoginNameAsync(LoginName, TestContext.Current.CancellationToken)).ReturnsAsync(persistentUser);
                }

                Persistence = new Mock<IPersistence>(MockBehavior.Strict);
                Persistence.SetupGet(x => x.User).Returns(userRepositoryAvailable ? UserRepository.Object : null);

                Factory = new ApplicationFactory(Mock.Of<IAuditIdentityProvider>(), Persistence.Object, repository: null, OperatingSystemIdentityProvider.Object);

                Application = Factory.Create();
                UserSignInService = Application.IdentityService?.UserSignInService ?? throw new InvalidOperationException("The user sign-in service is not available.");
                CurrentUserContext = Application.IdentityService.CurrentUserContext ?? throw new InvalidOperationException("The current-user context is not available.");
            }

            public IApplication Application { get; }

            public ICurrentUserContext CurrentUserContext { get; }

            public ApplicationFactory Factory { get; }

            public Mock<IOperatingSystemIdentityProvider> OperatingSystemIdentityProvider { get; }

            public Mock<IPersistence> Persistence { get; }

            public Mock<IUserRepository> UserRepository { get; }

            public IUserSignInService UserSignInService { get; }
        }
    }
}