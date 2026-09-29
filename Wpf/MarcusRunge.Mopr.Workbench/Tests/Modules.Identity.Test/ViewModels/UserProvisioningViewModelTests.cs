using MarcusRunge.Mopr.Workbench.Contracts.Application.Administration.Services;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Identity.Models;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Identity.Services;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Lifetime.Services;
using MarcusRunge.Mopr.Workbench.Modules.Identity.Properties;
using MarcusRunge.Mopr.Workbench.Modules.Identity.ViewModels;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Identity;
using Moq;
using Prism.Navigation.Regions;

namespace MarcusRunge.Mopr.Workbench.Modules.Identity.Test.ViewModels
{
    public sealed class UserProvisioningViewModelTests
    {
        private const string LoginName = @"DOMAIN\User";

        [Fact]
        public void Constructor_WhenAdministrativeAuthorizationIsMissing_ExposesRequiredAuthorizationState()
        {
            var context = CreateContext(isElevatedAdministrator: false);

            Assert.False(context.ViewModel.HasAdministrativeAuthorization);
            Assert.True(context.ViewModel.IsAdministrativeAuthorizationRequired);
            Assert.False(context.ViewModel.ProvisionCommand.CanExecute());
        }

        [Fact]
        public void Constructor_WhenAdministrativeAuthorizationIsAvailable_ExposesAuthorizedState()
        {
            var context = CreateContext(isElevatedAdministrator: true);

            Assert.True(context.ViewModel.HasAdministrativeAuthorization);
            Assert.False(context.ViewModel.IsAdministrativeAuthorizationRequired);
            Assert.False(context.ViewModel.ProvisionCommand.CanExecute());
        }

        [Fact]
        public async Task OnNavigatedTo_WhenIdentityIsAvailable_LoadsReadOnlyLoginName()
        {
            var context = CreateContext(isElevatedAdministrator: true);

            context.ViewModel.OnNavigatedTo(navigationContext: null!);

            await WaitForAsync(() => context.ViewModel.LoginName == LoginName && !context.ViewModel.IsBusy, TestContext.Current.CancellationToken);

            Assert.Equal(LoginName, context.ViewModel.LoginName);
            Assert.False(context.ViewModel.IsBusy);
            context.OperatingSystemIdentityProvider.Verify(x => x.GetCurrentIdentityAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task OnNavigatedTo_WhenAdministrativeAuthorizationIsMissing_ExposesAuthorizationRequiredStateWithoutDuplicateStatusMessage()
        {
            var context = CreateContext(isElevatedAdministrator: false);

            context.ViewModel.OnNavigatedTo(navigationContext: null!);

            await WaitForAsync(() => context.ViewModel.LoginName == LoginName && !context.ViewModel.IsBusy, TestContext.Current.CancellationToken);

            Assert.Equal(LoginName, context.ViewModel.LoginName);
            Assert.False(context.ViewModel.HasAdministrativeAuthorization);
            Assert.True(context.ViewModel.IsAdministrativeAuthorizationRequired);
            Assert.False(context.ViewModel.HasStatusMessage);
            Assert.Equal(string.Empty, context.ViewModel.StatusMessage);
            Assert.False(context.ViewModel.ProvisionCommand.CanExecute());

            context.AdministrativeAuthorizationService.VerifyGet(x => x.IsElevatedAdministrator, Times.AtLeastOnce);
            context.OperatingSystemIdentityProvider.Verify(x => x.GetCurrentIdentityAsync(It.IsAny<CancellationToken>()), Times.Once);
            context.UserProvisioningService.Verify(x => x.ProvisionAsync(It.IsAny<UserProvisioningRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ProvisionCommand_WhenAdministratorProvidesAllRequiredValues_CanExecute()
        {
            var context = CreateContext(isElevatedAdministrator: true);

            context.ViewModel.OnNavigatedTo(navigationContext: null!);

            await WaitForAsync(() => context.ViewModel.LoginName == LoginName && !context.ViewModel.IsBusy, TestContext.Current.CancellationToken);

            context.ViewModel.FirstName = "Marcus";
            context.ViewModel.LastName = "Runge";
            context.ViewModel.ShortName = "MR";

            Assert.True(context.ViewModel.ProvisionCommand.CanExecute());
        }

        [Theory]
        [InlineData("", "Runge", "MR")]
        [InlineData("Marcus", "", "MR")]
        [InlineData("Marcus", "Runge", "")]
        [InlineData(" ", "Runge", "MR")]
        [InlineData("Marcus", " ", "MR")]
        [InlineData("Marcus", "Runge", " ")]
        public async Task ProvisionCommand_WhenRequiredValueIsMissing_CannotExecute(string firstName, string lastName, string shortName)
        {
            var context = CreateContext(isElevatedAdministrator: true);

            context.ViewModel.OnNavigatedTo(navigationContext: null!);

            await WaitForAsync(() => context.ViewModel.LoginName == LoginName && !context.ViewModel.IsBusy, TestContext.Current.CancellationToken);

            context.ViewModel.FirstName = firstName;
            context.ViewModel.LastName = lastName;
            context.ViewModel.ShortName = shortName;

            Assert.False(context.ViewModel.ProvisionCommand.CanExecute());
        }

        [Fact]
        public async Task ProvisionCommand_WhenAdministrativeAuthorizationIsMissing_RemainsDisabledWithCompleteInput()
        {
            var context = CreateContext(isElevatedAdministrator: false);

            context.ViewModel.OnNavigatedTo(navigationContext: null!);

            await WaitForAsync(() => context.ViewModel.LoginName == LoginName && !context.ViewModel.IsBusy, TestContext.Current.CancellationToken);

            context.ViewModel.FirstName = "Marcus";
            context.ViewModel.LastName = "Runge";
            context.ViewModel.ShortName = "MR";

            Assert.False(context.ViewModel.ProvisionCommand.CanExecute());
            context.UserProvisioningService.Verify(x => x.ProvisionAsync(It.IsAny<UserProvisioningRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task OnNavigatedFrom_WhenIdentityResolutionIsRunning_CancelsIdentityResolution()
        {
            var identityResolutionStarted = new TaskCompletionSource<CancellationToken>(TaskCreationOptions.RunContinuationsAsynchronously);
            var identityResolutionCanceled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var context = CreateContext(isElevatedAdministrator: true, configureIdentityProvider: false);

            context.OperatingSystemIdentityProvider
                .Setup(x => x.GetCurrentIdentityAsync(It.IsAny<CancellationToken>()))
                .Returns(async (CancellationToken cancellationToken) =>
                {
                    identityResolutionStarted.TrySetResult(cancellationToken);

                    try
                    {
                        await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                        return new OperatingSystemIdentity(LoginName);
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        identityResolutionCanceled.TrySetResult();
                        throw;
                    }
                });

            context.ViewModel.OnNavigatedTo(navigationContext: null!);

            await identityResolutionStarted.Task.WaitAsync(TestContext.Current.CancellationToken);

            context.ViewModel.OnNavigatedFrom(navigationContext: null!);

            await identityResolutionCanceled.Task.WaitAsync(TestContext.Current.CancellationToken);

            Assert.False(context.ViewModel.ProvisionCommand.CanExecute());
        }

        private static UserProvisioningViewModelTestContext CreateContext(bool isElevatedAdministrator, bool configureIdentityProvider = true) => new(isElevatedAdministrator, configureIdentityProvider);

        private static async Task WaitForAsync(Func<bool> condition, CancellationToken cancellationToken)
        {
            while (!condition())
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Yield();
            }
        }

        private sealed class UserProvisioningViewModelTestContext
        {
            public UserProvisioningViewModelTestContext(bool isElevatedAdministrator, bool configureIdentityProvider)
            {
                AdministrativeAuthorizationService = new Mock<IAdministrativeAuthorizationService>(MockBehavior.Strict);
                AdministrativeAuthorizationService.SetupGet(x => x.IsElevatedAdministrator).Returns(isElevatedAdministrator);

                OperatingSystemIdentityProvider = new Mock<IOperatingSystemIdentityProvider>(MockBehavior.Strict);

                if (configureIdentityProvider)
                {
                    OperatingSystemIdentityProvider.Setup(x => x.GetCurrentIdentityAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new OperatingSystemIdentity(LoginName));
                }

                UserProvisioningService = new Mock<IUserProvisioningService>(MockBehavior.Strict);

                IdentityService = new Mock<IIdentityService>(MockBehavior.Strict);
                IdentityService.SetupGet(x => x.UserProvisioningService).Returns(UserProvisioningService.Object);

                Application = new Mock<IApplication>(MockBehavior.Strict);
                Application.SetupGet(x => x.IdentityService).Returns(IdentityService.Object);

                LifetimeService = new Mock<ILifetimeService>(MockBehavior.Strict);
                LifetimeService.SetupGet(x => x.ApplicationStopping).Returns(TestContext.Current.CancellationToken);

                RegionManager = new Mock<IRegionManager>(MockBehavior.Loose);

                ViewModel = new UserProvisioningViewModel(Application.Object, AdministrativeAuthorizationService.Object, OperatingSystemIdentityProvider.Object, LifetimeService.Object, RegionManager.Object);
            }

            public Mock<IAdministrativeAuthorizationService> AdministrativeAuthorizationService { get; }

            public Mock<IApplication> Application { get; }

            public Mock<IIdentityService> IdentityService { get; }

            public Mock<ILifetimeService> LifetimeService { get; }

            public Mock<IOperatingSystemIdentityProvider> OperatingSystemIdentityProvider { get; }

            public Mock<IRegionManager> RegionManager { get; }

            public Mock<IUserProvisioningService> UserProvisioningService { get; }

            public UserProvisioningViewModel ViewModel { get; }
        }
    }
}