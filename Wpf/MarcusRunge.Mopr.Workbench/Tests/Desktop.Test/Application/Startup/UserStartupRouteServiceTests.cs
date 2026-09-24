using MarcusRunge.Mopr.Workbench.Application.Startup;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Identity;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Identity.Models;
using MarcusRunge.Mopr.Workbench.Contracts.Enums;
using MarcusRunge.Mopr.Workbench.Core;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Identity;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace MarcusRunge.Mopr.Workbench.Test.Application.Startup
{
    public sealed class UserStartupRouteServiceTests
    {
        [Fact]
        public async Task GetNavigationTargetAsync_WhenUserIsSignedIn_ReturnsImaging()
        {
            var context = CreateContext(UserSignInResult.SignedIn(CreateOperatingSystemIdentity(), CreateCurrentUser()));

            string result = await context.Service.GetNavigationTargetAsync(TestContext.Current.CancellationToken);

            Assert.Equal(NavigationNames.Imaging, result);
        }

        [Fact]
        public async Task GetNavigationTargetAsync_WhenUserIsUnknown_ReturnsIdentityProvisioning()
        {
            var context = CreateContext(UserSignInResult.UserUnknown(CreateOperatingSystemIdentity()));

            string result = await context.Service.GetNavigationTargetAsync(TestContext.Current.CancellationToken);

            Assert.Equal(NavigationNames.IdentityProvisioning, result);
        }

        [Fact]
        public async Task GetNavigationTargetAsync_WhenUserIsDisabled_ReturnsIdentityBlocked()
        {
            var user = new CurrentUser(73, @"DOMAIN\User", "Marcus", "Runge", "MR", isActive: false);
            var context = CreateContext(UserSignInResult.UserDisabled(CreateOperatingSystemIdentity(), user));

            string result = await context.Service.GetNavigationTargetAsync(TestContext.Current.CancellationToken);

            Assert.Equal(NavigationNames.IdentityBlocked, result);
        }

        [Theory]
        [InlineData(UserSignInStatus.OperatingSystemIdentityUnavailable)]
        [InlineData(UserSignInStatus.InvalidPersistentUserId)]
        [InlineData(UserSignInStatus.PersistenceUnavailable)]
        [InlineData(UserSignInStatus.Failed)]
        public async Task GetNavigationTargetAsync_WhenIdentityIsUnavailable_ReturnsIdentityUnavailable(UserSignInStatus status)
        {
            UserSignInResult signInResult = status switch
            {
                UserSignInStatus.OperatingSystemIdentityUnavailable => UserSignInResult.OperatingSystemIdentityUnavailable(),
                UserSignInStatus.InvalidPersistentUserId => UserSignInResult.InvalidPersistentUserId(CreateOperatingSystemIdentity()),
                UserSignInStatus.PersistenceUnavailable => UserSignInResult.PersistenceUnavailable(CreateOperatingSystemIdentity()),
                UserSignInStatus.Failed => UserSignInResult.Failed(CreateOperatingSystemIdentity()),
                _ => throw new InvalidOperationException($"Unsupported test status '{status}'.")
            };

            var context = CreateContext(signInResult);

            string navigationTarget = await context.Service.GetNavigationTargetAsync(TestContext.Current.CancellationToken);

            Assert.Equal(NavigationNames.IdentityUnavailable, navigationTarget);
        }

        [Fact]
        public async Task GetNavigationTargetAsync_WhenSignInServiceIsUnavailable_ReturnsIdentityUnavailable()
        {
            var application = new Mock<IApplication>(MockBehavior.Strict);
            var identityService = new Mock<IIdentityService>(MockBehavior.Strict);
            identityService.SetupGet(x => x.UserSignInService).Returns((IUserSignInService?)null);
            application.SetupGet(x => x.IdentityService).Returns(identityService.Object);
            var service = new UserStartupRouteService(application.Object);

            string result = await service.GetNavigationTargetAsync(TestContext.Current.CancellationToken);

            Assert.Equal(NavigationNames.IdentityUnavailable, result);
        }

        [Fact]
        public async Task GetNavigationTargetAsync_WhenSignInIsCanceled_PropagatesCancellation()
        {
            var application = new Mock<IApplication>(MockBehavior.Strict);
            var identityService = new Mock<IIdentityService>(MockBehavior.Strict);
            var signInService = new Mock<IUserSignInService>(MockBehavior.Strict);
            using var cancellation = new CancellationTokenSource();
            await cancellation.CancelAsync();

            signInService.Setup(x => x.SignInAsync(cancellation.Token)).ThrowsAsync(new OperationCanceledException(cancellation.Token));
            identityService.SetupGet(x => x.UserSignInService).Returns(signInService.Object);
            application.SetupGet(x => x.IdentityService).Returns(identityService.Object);
            var service = new UserStartupRouteService(application.Object);

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.GetNavigationTargetAsync(cancellation.Token));
        }

        private static TestContextData CreateContext(UserSignInResult result)
        {
            var application = new Mock<IApplication>(MockBehavior.Strict);
            var identityService = new Mock<IIdentityService>(MockBehavior.Strict);
            var signInService = new Mock<IUserSignInService>(MockBehavior.Strict);

            signInService.Setup(x => x.SignInAsync(TestContext.Current.CancellationToken)).ReturnsAsync(result);
            identityService.SetupGet(x => x.UserSignInService).Returns(signInService.Object);
            application.SetupGet(x => x.IdentityService).Returns(identityService.Object);

            return new TestContextData(new UserStartupRouteService(application.Object));
        }

        private static CurrentUser CreateCurrentUser() => new(73, @"DOMAIN\User", "Marcus", "Runge", "MR", isActive: true);

        private static OperatingSystemIdentity CreateOperatingSystemIdentity() => new(@"DOMAIN\User");

        private sealed record TestContextData(IUserStartupRouteService Service);
    }
}