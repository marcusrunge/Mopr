using MarcusRunge.Mopr.Workbench.Contracts.Application.Administration.Services;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Identity;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Identity.Models;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Identity.Services;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Lifetime.Services;
using MarcusRunge.Mopr.Workbench.Contracts.Enums;
using MarcusRunge.Mopr.Workbench.Core.Events;
using MarcusRunge.Mopr.Workbench.Modules.Identity.Properties;
using MarcusRunge.Mopr.Workbench.Modules.Identity.ViewModels;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Identity;
using Moq;

namespace MarcusRunge.Mopr.Workbench.Modules.Identity.Test.ViewModels
{
    public sealed class UserProvisioningViewModelTests
    {
        private const int UserId = 73;
        private const string FirstName = "Marcus";
        private const string LastName = "Runge";
        private const string LoginName = @"DOMAIN\User";
        private const string ShortName = "MR";

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

            context.OperatingSystemIdentityProvider.Verify(provider => provider.GetCurrentIdentityAsync(It.IsAny<CancellationToken>()), Times.Once);
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

            context.AdministrativeAuthorizationService.VerifyGet(service => service.IsElevatedAdministrator, Times.AtLeastOnce);

            context.OperatingSystemIdentityProvider.Verify(provider => provider.GetCurrentIdentityAsync(It.IsAny<CancellationToken>()), Times.Once);

            context.UserProvisioningService.Verify(service => service.ProvisionAsync(It.IsAny<UserProvisioningRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ProvisionCommand_WhenAdministratorProvidesAllRequiredValues_CanExecute()
        {
            var context = CreateContext(isElevatedAdministrator: true);

            await context.PrepareProvisioningAsync(TestContext.Current.CancellationToken);

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

            context.ViewModel.FirstName = FirstName;
            context.ViewModel.LastName = LastName;
            context.ViewModel.ShortName = ShortName;

            Assert.False(context.ViewModel.ProvisionCommand.CanExecute());

            context.UserProvisioningService.Verify(service => service.ProvisionAsync(It.IsAny<UserProvisioningRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ProvisionCommand_WhenProvisioningCompletes_PublishesInitialUserProvisioningCompletedEventOnce()
        {
            var context = CreateContext(isElevatedAdministrator: true);
            var publishedCount = 0;

            context.EventAggregator.GetEvent<InitialUserProvisioningCompletedEvent>().Subscribe(() => publishedCount++);

            context.UserProvisioningService.Setup(service => service.ProvisionAsync(It.Is<UserProvisioningRequest>(request => request.FirstName == FirstName && request.LastName == LastName && request.ShortName == ShortName), It.IsAny<CancellationToken>())).ReturnsAsync(CreateCompletedResult());

            await context.PrepareProvisioningAsync(TestContext.Current.CancellationToken);

            context.ViewModel.ProvisionCommand.Execute();

            await WaitForAsync(() => publishedCount == 1 && !context.ViewModel.IsBusy, TestContext.Current.CancellationToken);

            Assert.Equal(1, publishedCount);

            context.UserProvisioningService.Verify(service => service.ProvisionAsync(It.Is<UserProvisioningRequest>(request => request.FirstName == FirstName && request.LastName == LastName && request.ShortName == ShortName), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ProvisionCommand_WhenMatchingActiveUserWasCreatedConcurrently_PublishesInitialUserProvisioningCompletedEventOnce()
        {
            var context = CreateContext(isElevatedAdministrator: true);
            var publishedCount = 0;

            context.EventAggregator.GetEvent<InitialUserProvisioningCompletedEvent>().Subscribe(() => publishedCount++);

            context.UserProvisioningService.Setup(service => service.ProvisionAsync(It.IsAny<UserProvisioningRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(CreateExistingActiveUserResult());

            await context.PrepareProvisioningAsync(TestContext.Current.CancellationToken);

            context.ViewModel.ProvisionCommand.Execute();

            await WaitForAsync(() => publishedCount == 1 && !context.ViewModel.IsBusy, TestContext.Current.CancellationToken);

            Assert.Equal(1, publishedCount);
        }

        [Fact]
        public async Task ProvisionCommand_WhenValidationFails_RemainsOnFormAndShowsValidationMessage()
        {
            var context = CreateContext(isElevatedAdministrator: true);
            var publishedCount = 0;

            context.EventAggregator.GetEvent<InitialUserProvisioningCompletedEvent>().Subscribe(() => publishedCount++);

            context.UserProvisioningService.Setup(service => service.ProvisionAsync(It.IsAny<UserProvisioningRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(UserProvisioningResult.ValidationFailed([UserProvisioningValidationIssue.FirstNameRequired, UserProvisioningValidationIssue.ShortNameInvalid]));

            await context.PrepareProvisioningAsync(TestContext.Current.CancellationToken);

            context.ViewModel.ProvisionCommand.Execute();

            await WaitForAsync(() => context.ViewModel.HasValidationMessage && !context.ViewModel.IsBusy, TestContext.Current.CancellationToken);

            Assert.True(context.ViewModel.HasValidationMessage);
            Assert.Contains(Resources.IdentityProvisioningValidationSummary, context.ViewModel.ValidationMessage);
            Assert.Contains(Resources.IdentityProvisioningFirstNameRequired, context.ViewModel.ValidationMessage);
            Assert.Contains(Resources.IdentityProvisioningShortNameInvalid, context.ViewModel.ValidationMessage);
            Assert.Equal(0, publishedCount);
        }

        [Fact]
        public async Task ProvisionCommand_WhenAdministrativeAuthorizationIsRejectedByService_RemainsOnFormWithoutPublishingCompletionEvent()
        {
            var context = CreateContext(isElevatedAdministrator: true);
            var publishedCount = 0;

            context.EventAggregator.GetEvent<InitialUserProvisioningCompletedEvent>().Subscribe(() => publishedCount++);

            context.UserProvisioningService.Setup(service => service.ProvisionAsync(It.IsAny<UserProvisioningRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(UserProvisioningResult.AdministrativeAuthorizationRequired(new OperatingSystemIdentity(LoginName)));

            await context.PrepareProvisioningAsync(TestContext.Current.CancellationToken);

            context.ViewModel.ProvisionCommand.Execute();

            await WaitForAsync(() => !context.ViewModel.IsBusy, TestContext.Current.CancellationToken);

            Assert.Equal(string.Empty, context.ViewModel.StatusMessage);
            Assert.Equal(0, publishedCount);
        }

        [Fact]
        public async Task OnNavigatedFrom_WhenIdentityResolutionIsRunning_CancelsIdentityResolution()
        {
            var identityResolutionStarted = new TaskCompletionSource<CancellationToken>(TaskCreationOptions.RunContinuationsAsynchronously);
            var identityResolutionCanceled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var context = CreateContext(isElevatedAdministrator: true, configureIdentityProvider: false);

            context.OperatingSystemIdentityProvider.Setup(provider => provider.GetCurrentIdentityAsync(It.IsAny<CancellationToken>())).Returns(async (CancellationToken cancellationToken) =>
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

        [Fact]
        public async Task OnNavigatedFrom_WhenProvisioningIsRunning_CancelsProvisioningWithoutPublishingCompletionEvent()
        {
            var context = CreateContext(isElevatedAdministrator: true);
            var provisioningStarted = new TaskCompletionSource<CancellationToken>(TaskCreationOptions.RunContinuationsAsynchronously);
            var provisioningCanceled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var publishedCount = 0;

            context.EventAggregator.GetEvent<InitialUserProvisioningCompletedEvent>().Subscribe(() => publishedCount++);

            context.UserProvisioningService.Setup(service => service.ProvisionAsync(It.IsAny<UserProvisioningRequest>(), It.IsAny<CancellationToken>())).Returns(async (UserProvisioningRequest _, CancellationToken cancellationToken) =>
            {
                provisioningStarted.TrySetResult(cancellationToken);

                try
                {
                    await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                    return CreateCompletedResult();
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    provisioningCanceled.TrySetResult();
                    throw;
                }
            });

            await context.PrepareProvisioningAsync(TestContext.Current.CancellationToken);

            context.ViewModel.ProvisionCommand.Execute();

            await provisioningStarted.Task.WaitAsync(TestContext.Current.CancellationToken);

            context.ViewModel.OnNavigatedFrom(navigationContext: null!);

            await provisioningCanceled.Task.WaitAsync(TestContext.Current.CancellationToken);

            Assert.Equal(0, publishedCount);
        }

        private static UserProvisioningResult CreateCompletedResult()
        {
            var operatingSystemIdentity = new OperatingSystemIdentity(LoginName);
            var currentUser = CreateCurrentUser(isActive: true);
            return UserProvisioningResult.Completed(operatingSystemIdentity, currentUser);
        }

        private static CurrentUser CreateCurrentUser(bool isActive) => new(UserId, LoginName, FirstName, LastName, ShortName, isActive);

        private static UserProvisioningResult CreateExistingActiveUserResult()
        {
            var operatingSystemIdentity = new OperatingSystemIdentity(LoginName);
            var currentUser = CreateCurrentUser(isActive: true); return UserProvisioningResult.UserAlreadyExists(operatingSystemIdentity, currentUser);
        }

        private static UserProvisioningViewModelTestContext CreateContext(bool isElevatedAdministrator, bool configureIdentityProvider = true) => new(isElevatedAdministrator, configureIdentityProvider);

        private static async Task WaitForAsync(Func<bool> condition, CancellationToken cancellationToken)
        {
            var timeoutAt = DateTime.UtcNow.AddSeconds(5);

            while (!condition())
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (DateTime.UtcNow >= timeoutAt)
                {
                    throw new TimeoutException("The expected UserProvisioningViewModel state was not reached.");
                }

                await Task.Delay(10, cancellationToken);
            }
        }

        private sealed class UserProvisioningViewModelTestContext
        {
            public UserProvisioningViewModelTestContext(bool isElevatedAdministrator, bool configureIdentityProvider)
            {
                AdministrativeAuthorizationService = new Mock<IAdministrativeAuthorizationService>(MockBehavior.Strict);

                AdministrativeAuthorizationService.SetupGet(service => service.IsElevatedAdministrator).Returns(isElevatedAdministrator);

                OperatingSystemIdentityProvider = new Mock<IOperatingSystemIdentityProvider>(MockBehavior.Strict);

                if (configureIdentityProvider)
                {
                    OperatingSystemIdentityProvider.Setup(provider => provider.GetCurrentIdentityAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new OperatingSystemIdentity(LoginName));
                }

                UserProvisioningService = new Mock<IUserProvisioningService>(MockBehavior.Strict);

                IdentityService = new Mock<IIdentityService>(MockBehavior.Strict);

                IdentityService.SetupGet(service => service.UserProvisioningService).Returns(UserProvisioningService.Object);

                Application = new Mock<IApplication>(MockBehavior.Strict);

                Application.SetupGet(application => application.IdentityService).Returns(IdentityService.Object);

                LifetimeService = new Mock<ILifetimeService>(MockBehavior.Strict);

                LifetimeService.SetupGet(service => service.ApplicationStopping).Returns(TestContext.Current.CancellationToken);

                // Prism navigation remains part of the productive view model.
                // These unit tests exercise only branches that remain on the
                // provisioning view or publish the protected completion event.
                RegionManager = new Mock<IRegionManager>(MockBehavior.Loose);
                EventAggregator = new EventAggregator();

                ViewModel = new UserProvisioningViewModel(Application.Object, AdministrativeAuthorizationService.Object, OperatingSystemIdentityProvider.Object, LifetimeService.Object, RegionManager.Object, EventAggregator);
            }

            public Mock<IAdministrativeAuthorizationService> AdministrativeAuthorizationService { get; }

            public Mock<IApplication> Application { get; }

            public IEventAggregator EventAggregator { get; }

            public Mock<IIdentityService> IdentityService { get; }

            public Mock<ILifetimeService> LifetimeService { get; }
            public Mock<IOperatingSystemIdentityProvider> OperatingSystemIdentityProvider { get; }

            public Mock<IRegionManager> RegionManager { get; }

            public Mock<IUserProvisioningService> UserProvisioningService { get; }

            public UserProvisioningViewModel ViewModel { get; }

            public async Task PrepareProvisioningAsync(CancellationToken cancellationToken)
            {
                ViewModel.OnNavigatedTo(navigationContext: null!);

                await WaitForAsync(() => ViewModel.LoginName == LoginName && !ViewModel.IsBusy, cancellationToken);

                ViewModel.FirstName = FirstName;
                ViewModel.LastName = LastName;
                ViewModel.ShortName = ShortName;

                Assert.True(ViewModel.ProvisionCommand.CanExecute());
            }
        }
    }
}