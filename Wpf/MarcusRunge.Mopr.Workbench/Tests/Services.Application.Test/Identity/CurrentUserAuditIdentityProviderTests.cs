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
        private const string LoginName = @"DOMAIN\User";

        [Fact]
        public async Task GetCurrentUserIdAsync_WhenNoUserIsSignedIn_ReturnsNull()
        {
            var context = CreateContext();

            int? result = await context.AuditIdentityProvider.GetCurrentUserIdAsync(TestContext.Current.CancellationToken);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetCurrentUserIdAsync_WhenActiveUserIsSignedIn_ReturnsPersistentUserId()
        {
            var context = CreateContext(new User { Id = 73, FirstName = "Marcus", IsActive = true, LastName = "Runge", LoginName = LoginName, ShortName = "MR" });

            UserSignInResult signInResult = await context.UserSignInService.SignInAsync(TestContext.Current.CancellationToken);
            int? result = await context.AuditIdentityProvider.GetCurrentUserIdAsync(TestContext.Current.CancellationToken);

            Assert.Equal(UserSignInStatus.SignedIn, signInResult.Status);
            Assert.Equal(73, result);
        }

        [Fact]
        public async Task GetCurrentUserIdAsync_WhenUserIsDisabled_ReturnsNull()
        {
            var context = CreateContext(new User { Id = 73, FirstName = "Marcus", IsActive = false, LastName = "Runge", LoginName = LoginName, ShortName = "MR" });

            UserSignInResult signInResult = await context.UserSignInService.SignInAsync(TestContext.Current.CancellationToken);
            int? result = await context.AuditIdentityProvider.GetCurrentUserIdAsync(TestContext.Current.CancellationToken);

            Assert.Equal(UserSignInStatus.UserDisabled, signInResult.Status);
            Assert.Null(result);
        }

        [Fact]
        public async Task GetCurrentUserIdAsync_WhenCanceled_PropagatesCancellation()
        {
            var context = CreateContext();
            using var cancellation = new CancellationTokenSource();
            await cancellation.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => context.AuditIdentityProvider.GetCurrentUserIdAsync(cancellation.Token));
        }

        private static TestContextData CreateContext(User? persistentUser = null)
        {
            var operatingSystemIdentityProvider = new Mock<IOperatingSystemIdentityProvider>(MockBehavior.Strict);
            operatingSystemIdentityProvider.Setup(x => x.GetCurrentIdentityAsync(TestContext.Current.CancellationToken)).ReturnsAsync(new OperatingSystemIdentity(LoginName));

            var userRepository = new Mock<IUserRepository>(MockBehavior.Strict);
            userRepository.Setup(x => x.GetByLoginNameAsync(LoginName, TestContext.Current.CancellationToken)).ReturnsAsync(persistentUser);

            var persistence = new Mock<IPersistence>(MockBehavior.Strict);
            persistence.SetupGet(x => x.User).Returns(userRepository.Object);

            var factory = new ApplicationFactory(persistence: persistence.Object, repository: null, operatingSystemIdentityProvider: operatingSystemIdentityProvider.Object);
            IApplication application = factory.Create();
            var identityService = application.IdentityService ?? throw new InvalidOperationException("The identity service is not available.");
            var identityServiceBase = identityService as IIdentityServiceBase ?? throw new InvalidOperationException("The internal identity-service contract is not available.");
            var auditIdentityProvider = identityServiceBase.AuditIdentityProvider ?? throw new InvalidOperationException("The current-user audit identity provider is not available.");
            var userSignInService = identityService.UserSignInService ?? throw new InvalidOperationException("The user sign-in service is not available.");

            return new TestContextData(auditIdentityProvider, userSignInService);
        }

        private sealed record TestContextData(IAuditIdentityProvider AuditIdentityProvider, IUserSignInService UserSignInService);
    }
}