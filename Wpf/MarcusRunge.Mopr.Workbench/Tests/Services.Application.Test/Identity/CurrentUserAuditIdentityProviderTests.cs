using MarcusRunge.Mopr.Workbench.Contracts.Application.Identity;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Identity.Models;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Identity.Services;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Security.Services;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Identity;
using MarcusRunge.Mopr.Workbench.Services.Application.Implementations.Identity;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Entities;
using Moq;

namespace MarcusRunge.Mopr.Workbench.Services.Application.Test.Identity
{
    public sealed class CurrentUserAuditIdentityProviderTests
    {
        private const int UserId = 73;
        private const string LoginName = @"DOMAIN\User";
        private const string SecurityIdentifier = "S-1-5-21-1000000000-2000000000-3000000000-1001";

        [Fact]
        public async Task GetCurrentUserIdAsync_WhenNoUserIsSignedIn_ReturnsNull()
        {
            var context = CreateContext();

            var result = await context.AuditIdentityProvider.GetCurrentUserIdAsync(TestContext.Current.CancellationToken);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetCurrentUserIdAsync_WhenActiveUserIsSignedIn_ReturnsPersistentUserId()
        {
            var context = CreateContext(CreatePersistentUser(isActive: true));

            var signInResult = await context.UserSignInService.SignInAsync(TestContext.Current.CancellationToken);
            var result = await context.AuditIdentityProvider.GetCurrentUserIdAsync(TestContext.Current.CancellationToken);

            Assert.Equal(UserSignInStatus.SignedIn, signInResult.Status);
            Assert.Equal(UserId, result);
            context.UserRepository.Verify(repository => repository.GetBySecurityIdentifierAsync(SecurityIdentifier, TestContext.Current.CancellationToken), Times.Once);
            context.UserRepository.Verify(repository => repository.GetByLoginNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task GetCurrentUserIdAsync_WhenUserIsDisabled_ReturnsNull()
        {
            var context = CreateContext(CreatePersistentUser(isActive: false));

            var signInResult = await context.UserSignInService.SignInAsync(TestContext.Current.CancellationToken);
            var result = await context.AuditIdentityProvider.GetCurrentUserIdAsync(TestContext.Current.CancellationToken);

            Assert.Equal(UserSignInStatus.UserDisabled, signInResult.Status);
            Assert.Null(result);
            context.UserRepository.Verify(repository => repository.GetBySecurityIdentifierAsync(SecurityIdentifier, TestContext.Current.CancellationToken), Times.Once);
        }

        [Fact]
        public async Task GetCurrentUserIdAsync_WhenCanceled_PropagatesCancellation()
        {
            var context = CreateContext();
            using var cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);

            await cancellationSource.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => context.AuditIdentityProvider.GetCurrentUserIdAsync(cancellationSource.Token));
        }

        private static TestContextData CreateContext(User? persistentUser = null)
        {
            var operatingSystemIdentityProvider = new Mock<IOperatingSystemIdentityProvider>(MockBehavior.Strict);

            operatingSystemIdentityProvider
                .Setup(provider => provider.GetCurrentIdentityAsync(TestContext.Current.CancellationToken))
                .ReturnsAsync(new OperatingSystemIdentity(LoginName, SecurityIdentifier));

            var userRepository = new Mock<IUserRepository>(MockBehavior.Strict);

            userRepository
                .Setup(repository => repository.GetBySecurityIdentifierAsync(SecurityIdentifier, TestContext.Current.CancellationToken))
                .ReturnsAsync(persistentUser);

            var persistence = new Mock<IPersistence>(MockBehavior.Strict);
            persistence.SetupGet(instance => instance.User).Returns(userRepository.Object);

            var factory = new ApplicationFactory(persistence: persistence.Object, repository: null, operatingSystemIdentityProvider: operatingSystemIdentityProvider.Object);
            var application = factory.Create();
            var identityService = application.IdentityService ?? throw new InvalidOperationException("The identity service is not available.");
            var identityServiceBase = identityService as IIdentityServiceBase ?? throw new InvalidOperationException("The internal identity-service contract is not available.");
            var auditIdentityProvider = identityServiceBase.AuditIdentityProvider ?? throw new InvalidOperationException("The current-user audit identity provider is not available.");
            var userSignInService = identityService.UserSignInService ?? throw new InvalidOperationException("The user sign-in service is not available.");

            return new TestContextData(auditIdentityProvider, userSignInService, userRepository);
        }

        private static User CreatePersistentUser(bool isActive) => new()
        {
            Id = UserId,
            FirstName = "Marcus",
            IsActive = isActive,
            LastName = "Runge",
            LoginName = LoginName,
            SecurityIdentifier = SecurityIdentifier,
            ShortName = "MR"
        };

        private sealed record TestContextData(IAuditIdentityProvider AuditIdentityProvider, IUserSignInService UserSignInService, Mock<IUserRepository> UserRepository);
    }
}