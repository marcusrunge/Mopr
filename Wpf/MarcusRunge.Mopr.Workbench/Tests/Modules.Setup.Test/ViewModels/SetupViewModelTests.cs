using MarcusRunge.Mopr.Workbench.Application.Configuration;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Configuration;
using MarcusRunge.Mopr.Workbench.Contracts.Models.Configuration;
using MarcusRunge.Mopr.Workbench.Modules.Setup.Properties;
using MarcusRunge.Mopr.Workbench.Modules.Setup.ViewModels;
using MarcusRunge.Mopr.Workbench.Services.Wpf.Contracts;
using Moq;
using Prism.Navigation.Regions;

namespace MarcusRunge.Mopr.Workbench.Modules.Setup.Test.ViewModels
{
    public sealed class SetupViewModelTests
    {
        private const string DefaultLocalDbConnectionString = @"Server=(localdb)\MSSQLLocalDB;Database=Mopr;Integrated Security=True;TrustServerCertificate=True;MultipleActiveResultSets=True;";
        private const string OriginalConnectionString = @"Server=original;Database=Mopr;";
        private const string RepositoryPath = @"C:\MOPR\Repository";
        private const string UpdatedConnectionString = @"Server=updated;Database=Mopr;Integrated Security=True;";

        [Fact]
        public void UseLocalDatabaseCommand_WhenExecuted_InsertsRecommendedConnectionString()
        {
            var context = new SetupViewModelTestContext();

            context.ViewModel.UseLocalDatabaseCommand.Execute();

            Assert.Equal(DefaultLocalDbConnectionString, context.ViewModel.ConnectionString);
            Assert.Null(context.ViewModel.IsDatabaseConnectionSuccessful);
            Assert.Empty(context.ViewModel.DatabaseStatusText);
            Assert.False(context.ViewModel.CanContinueFromDatabase);
        }

        [Fact]
        public void UseLocalDatabaseCommand_WhenModificationIsAllowed_CanExecuteOnDatabaseStep()
        {
            var context = new SetupViewModelTestContext();

            Assert.True(context.ViewModel.IsDatabaseStep);
            Assert.True(context.ViewModel.UseLocalDatabaseCommand.CanExecute());
        }

        [Fact]
        public void UseLocalDatabaseCommand_WhenModificationIsNotAllowed_CannotExecute()
        {
            var context = new SetupViewModelTestContext(canModify: false);

            Assert.False(context.ViewModel.UseLocalDatabaseCommand.CanExecute());
        }

        [Fact]
        public void ConnectionString_WhenChanged_ClearsPreviousCompletionStatus()
        {
            var context = new SetupViewModelTestContext();

            context.ViewModel.ConnectionString = OriginalConnectionString;
            context.ViewModel.ConnectionString = UpdatedConnectionString;

            Assert.Null(context.ViewModel.IsDatabaseConnectionSuccessful);
            Assert.Empty(context.ViewModel.DatabaseStatusText);
            Assert.Empty(context.ViewModel.CompletionStatusText);
        }

        [Fact]
        public async Task ConnectionString_WhenChangedAfterSuccessfulValidation_InvalidatesValidationResult()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            var context = new SetupViewModelTestContext();

            await context.LoadAsync(cancellationToken);

            context.ViewModel.TestDatabaseConnectionCommand.Execute();

            await WaitUntilAsync(() => !context.ViewModel.IsTestingDatabase && context.ViewModel.IsDatabaseConnectionSuccessful == true, cancellationToken);

            Assert.True(context.ViewModel.CanContinueFromDatabase);

            context.ViewModel.ConnectionString = UpdatedConnectionString;

            Assert.Null(context.ViewModel.IsDatabaseConnectionSuccessful);
            Assert.False(context.ViewModel.CanContinueFromDatabase);
            Assert.Empty(context.ViewModel.DatabaseStatusText);
        }

        [Fact]
        public async Task CompleteSetupCommand_UsesCurrentConnectionStringInsteadOfLoadedConfiguration()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            var context = new SetupViewModelTestContext();
            SetupCompletionRequest? capturedRequest = null;

            context.SetupCompletionService.Setup(service => service.CompleteAsync(It.IsAny<SetupCompletionRequest>(), It.IsAny<CancellationToken>()))
                .Callback<SetupCompletionRequest, CancellationToken>((request, _) => capturedRequest = request)
                .ReturnsAsync(SetupCompletionResult.Completed());

            await context.PrepareCompletionStepAsync(UpdatedConnectionString, RepositoryPath, cancellationToken);

            context.ViewModel.CompleteSetupCommand.Execute();

            await WaitUntilAsync(() => !context.ViewModel.IsCompletingSetup && context.ViewModel.IsSetupComplete, cancellationToken);

            Assert.NotNull(capturedRequest);
            Assert.Equal(UpdatedConnectionString, capturedRequest.Configuration.Database.ConnectionString);
            Assert.Equal(RepositoryPath, capturedRequest.RepositoryPath);
            Assert.False(capturedRequest.Configuration.IsSetupComplete);
            Assert.True(capturedRequest.Configuration.Repository.AutomaticallyRepairPaths);
            Assert.True(capturedRequest.Configuration.Security.AllowSelfModification);
            Assert.True(capturedRequest.Configuration.Security.HideOtherUsersFromRegularUsers);
            Assert.Equal(ApplicationConfiguration.CurrentSetupVersion, capturedRequest.Configuration.SetupVersion);
            Assert.True(context.ViewModel.IsSetupComplete);

            context.SetupCompletionService.Verify(service => service.CompleteAsync(
                It.Is<SetupCompletionRequest>(request => request.Configuration.Database.ConnectionString == UpdatedConnectionString && request.RepositoryPath == RepositoryPath),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CompleteSetupCommand_WhileRunning_DisablesRegularCommandsAndAllowsCancellation()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            var context = new SetupViewModelTestContext();
            var completionSource = new TaskCompletionSource<SetupCompletionResult>(TaskCreationOptions.RunContinuationsAsynchronously);

            context.SetupCompletionService.Setup(service => service.CompleteAsync(It.IsAny<SetupCompletionRequest>(), It.IsAny<CancellationToken>())).Returns(completionSource.Task);

            await context.PrepareCompletionStepAsync(UpdatedConnectionString, RepositoryPath, cancellationToken);

            context.ViewModel.CompleteSetupCommand.Execute();

            await WaitUntilAsync(() => context.ViewModel.IsCompletingSetup, cancellationToken);

            Assert.False(context.ViewModel.BackCommand.CanExecute());
            Assert.False(context.ViewModel.ContinueCommand.CanExecute());
            Assert.False(context.ViewModel.CompleteSetupCommand.CanExecute());
            Assert.False(context.ViewModel.TestDatabaseConnectionCommand.CanExecute());
            Assert.False(context.ViewModel.ValidateRepositoryLocationCommand.CanExecute());
            Assert.False(context.ViewModel.SelectRepositoryLocationCommand.CanExecute());
            Assert.False(context.ViewModel.UseLocalDatabaseCommand.CanExecute());
            Assert.True(context.ViewModel.CancelSetupCompletionCommand.CanExecute());
            Assert.False(context.ViewModel.IsSetupInteractionEnabled);
            Assert.Equal(Resources.Setup_CompletionRunning, context.ViewModel.CompletionStatusText);

            completionSource.SetResult(SetupCompletionResult.Failed(new InvalidOperationException("Controlled test completion.")));

            await WaitUntilAsync(() => !context.ViewModel.IsCompletingSetup, cancellationToken);

            Assert.True(context.ViewModel.BackCommand.CanExecute());
            Assert.True(context.ViewModel.CompleteSetupCommand.CanExecute());
            Assert.False(context.ViewModel.CancelSetupCompletionCommand.CanExecute());
            Assert.True(context.ViewModel.IsSetupInteractionEnabled);
        }

        [Fact]
        public async Task CancelSetupCompletionCommand_WhenCompletionIsRunning_CancelsProductiveToken()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            var context = new SetupViewModelTestContext();
            var completionSource = new TaskCompletionSource<SetupCompletionResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            CancellationToken productiveToken = default;

            context.SetupCompletionService.Setup(service => service.CompleteAsync(It.IsAny<SetupCompletionRequest>(), It.IsAny<CancellationToken>()))
                .Callback<SetupCompletionRequest, CancellationToken>((_, token) => productiveToken = token)
                .Returns(completionSource.Task);

            await context.PrepareCompletionStepAsync(UpdatedConnectionString, RepositoryPath, cancellationToken);

            context.ViewModel.CompleteSetupCommand.Execute();

            await WaitUntilAsync(() => context.ViewModel.IsCompletingSetup && productiveToken.CanBeCanceled, cancellationToken);

            context.ViewModel.CancelSetupCompletionCommand.Execute();

            Assert.True(productiveToken.IsCancellationRequested);
            Assert.False(context.ViewModel.CancelSetupCompletionCommand.CanExecute());
            Assert.Equal(Resources.Setup_CompletionCancellationRequested, context.ViewModel.CompletionStatusText);

            completionSource.SetResult(SetupCompletionResult.Canceled());

            await WaitUntilAsync(() => !context.ViewModel.IsCompletingSetup, cancellationToken);

            Assert.False(context.ViewModel.IsSetupComplete);
            Assert.Equal(Resources.Setup_CompletionCanceled, context.ViewModel.CompletionStatusText);
        }

        [Fact]
        public async Task ConfirmNavigationRequest_WhileCompletionIsRunning_RejectsNavigation()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            var context = new SetupViewModelTestContext();
            var completionSource = new TaskCompletionSource<SetupCompletionResult>(TaskCreationOptions.RunContinuationsAsynchronously);

            context.SetupCompletionService.Setup(service => service.CompleteAsync(It.IsAny<SetupCompletionRequest>(), It.IsAny<CancellationToken>())).Returns(completionSource.Task);

            await context.PrepareCompletionStepAsync(UpdatedConnectionString, RepositoryPath, cancellationToken);

            context.ViewModel.CompleteSetupCommand.Execute();

            await WaitUntilAsync(() => context.ViewModel.IsCompletingSetup, cancellationToken);

            bool? navigationAllowed = null;
            context.ViewModel.ConfirmNavigationRequest(null!, allowed => navigationAllowed = allowed);

            Assert.False(navigationAllowed);

            completionSource.SetResult(SetupCompletionResult.Failed(new InvalidOperationException("Controlled test completion.")));

            await WaitUntilAsync(() => !context.ViewModel.IsCompletingSetup, cancellationToken);

            navigationAllowed = null;
            context.ViewModel.ConfirmNavigationRequest(null!, allowed => navigationAllowed = allowed);

            Assert.True(navigationAllowed);
        }

        [Theory]
        [InlineData(SetupCompletionStatus.DatabaseValidationFailed)]
        [InlineData(SetupCompletionStatus.RepositoryValidationFailed)]
        [InlineData(SetupCompletionStatus.Canceled)]
        [InlineData(SetupCompletionStatus.Failed)]
        [InlineData(SetupCompletionStatus.FailedAndRollbackFailed)]
        public async Task CompleteSetupCommand_WhenCompletionDoesNotSucceed_DoesNotMarkSetupComplete(SetupCompletionStatus status)
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            var context = new SetupViewModelTestContext();

            context.SetupCompletionService.Setup(service => service.CompleteAsync(It.IsAny<SetupCompletionRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(CreateCompletionResult(status));

            await context.PrepareCompletionStepAsync(UpdatedConnectionString, RepositoryPath, cancellationToken);

            context.ViewModel.CompleteSetupCommand.Execute();

            await WaitUntilAsync(() => !context.ViewModel.IsCompletingSetup && !string.IsNullOrWhiteSpace(context.ViewModel.CompletionStatusText), cancellationToken);

            Assert.False(context.ViewModel.IsSetupComplete);
            Assert.Equal(GetExpectedCompletionStatusText(status), context.ViewModel.CompletionStatusText);
            Assert.True(context.ViewModel.CompleteSetupCommand.CanExecute());
            Assert.False(context.ViewModel.CancelSetupCompletionCommand.CanExecute());
        }

        [Fact]
        public async Task CompleteSetupCommand_WhenCompletionSucceeds_MarksSetupComplete()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            var context = new SetupViewModelTestContext();

            context.SetupCompletionService.Setup(service => service.CompleteAsync(It.IsAny<SetupCompletionRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(SetupCompletionResult.Completed());

            await context.PrepareCompletionStepAsync(UpdatedConnectionString, RepositoryPath, cancellationToken);

            context.ViewModel.CompleteSetupCommand.Execute();

            await WaitUntilAsync(() => !context.ViewModel.IsCompletingSetup && context.ViewModel.IsSetupComplete, cancellationToken);

            Assert.True(context.ViewModel.IsSetupComplete);
            Assert.Equal(Resources.Setup_CompletionSuccessful, context.ViewModel.CompletionStatusText);
            Assert.False(context.ViewModel.CancelSetupCompletionCommand.CanExecute());
        }

        [Fact]
        public async Task OnNavigatedFrom_WhenCompletionIsRunning_CancelsProductiveToken()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            var context = new SetupViewModelTestContext();
            var completionSource = new TaskCompletionSource<SetupCompletionResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            CancellationToken productiveToken = default;

            context.SetupCompletionService.Setup(service => service.CompleteAsync(It.IsAny<SetupCompletionRequest>(), It.IsAny<CancellationToken>()))
                .Callback<SetupCompletionRequest, CancellationToken>((_, token) => productiveToken = token)
                .Returns(completionSource.Task);

            await context.PrepareCompletionStepAsync(UpdatedConnectionString, RepositoryPath, cancellationToken);

            context.ViewModel.CompleteSetupCommand.Execute();

            await WaitUntilAsync(() => context.ViewModel.IsCompletingSetup && productiveToken.CanBeCanceled, cancellationToken);

            context.ViewModel.OnNavigatedFrom(null!);

            Assert.True(productiveToken.IsCancellationRequested);

            completionSource.SetResult(SetupCompletionResult.Canceled());

            await WaitUntilAsync(() => !context.ViewModel.IsCompletingSetup, cancellationToken);
        }

        [Fact]
        public async Task RepositoryLocationPath_WhenChangedAfterSuccessfulValidation_InvalidatesValidationResult()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            var context = new SetupViewModelTestContext();

            await context.PrepareRepositoryStepAsync(UpdatedConnectionString, cancellationToken);

            context.ViewModel.RepositoryLocationPath = RepositoryPath;
            context.ViewModel.ValidateRepositoryLocationCommand.Execute();

            await WaitUntilAsync(() => !context.ViewModel.IsValidatingRepositoryLocation && context.ViewModel.IsRepositoryLocationValid == true, cancellationToken);

            Assert.True(context.ViewModel.CanContinueFromRepository);

            context.ViewModel.RepositoryLocationPath = @"D:\MOPR\Repository";

            Assert.Null(context.ViewModel.IsRepositoryLocationValid);
            Assert.False(context.ViewModel.CanContinueFromRepository);
            Assert.Empty(context.ViewModel.RepositoryStatusText);
        }

        private static SetupCompletionResult CreateCompletionResult(SetupCompletionStatus status) => status switch
        {
            SetupCompletionStatus.DatabaseValidationFailed => SetupCompletionResult.DatabaseValidationFailed(),
            SetupCompletionStatus.RepositoryValidationFailed => SetupCompletionResult.RepositoryValidationFailed(),
            SetupCompletionStatus.Canceled => SetupCompletionResult.Canceled(),
            SetupCompletionStatus.FailedAndRollbackFailed => SetupCompletionResult.Failed(new InvalidOperationException("Rollback failed."), rollbackAttempted: true, rollbackSuccessful: false),
            _ => SetupCompletionResult.Failed(new InvalidOperationException("Setup completion failed."))
        };

        private static string GetExpectedCompletionStatusText(SetupCompletionStatus status) => status switch
        {
            SetupCompletionStatus.DatabaseValidationFailed => Resources.Setup_CompletionDatabaseValidationFailed,
            SetupCompletionStatus.RepositoryValidationFailed => Resources.Setup_CompletionRepositoryValidationFailed,
            SetupCompletionStatus.Canceled => Resources.Setup_CompletionCanceled,
            SetupCompletionStatus.FailedAndRollbackFailed => Resources.Setup_CompletionRollbackFailed,
            _ => Resources.Setup_CompletionFailed
        };

        private static async Task WaitUntilAsync(Func<bool> condition, CancellationToken cancellationToken)
        {
            var timeoutAt = DateTime.UtcNow.AddSeconds(5);

            while (!condition())
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (DateTime.UtcNow >= timeoutAt) throw new TimeoutException("The expected SetupViewModel state was not reached.");
                await Task.Delay(10, cancellationToken);
            }
        }

        private sealed class SetupViewModelTestContext
        {
            public SetupViewModelTestContext(bool canModify = true)
            {
                ConfigurationService.SetupGet(service => service.CanModify).Returns(canModify);
                ConfigurationService.Setup(service => service.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(CreateApplicationConfiguration());
                ConfigurationService.Setup(service => service.TestDatabaseConnectionAsync(It.IsAny<IDatabaseConfiguration>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

                RepositoryLocationValidationService.Setup(service => service.ValidateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((string path, CancellationToken _) => new RepositoryLocationValidationResult
                    {
                        Exists = true,
                        IsReadable = true,
                        IsWritable = true,
                        NormalizedPath = path
                    });

                SetupCompletionService.Setup(service => service.CompleteAsync(It.IsAny<SetupCompletionRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(SetupCompletionResult.Completed());

                ViewModel = new SetupViewModel(ConfigurationService.Object, RepositoryLocationValidationService.Object, SetupCompletionService.Object, Wpf.Object, RegionManager.Object);
            }

            public Mock<IMachineConfigurationService> ConfigurationService { get; } = new(MockBehavior.Strict);

            public Mock<IRegionManager> RegionManager { get; } = new(MockBehavior.Loose);

            public Mock<IRepositoryLocationValidationService> RepositoryLocationValidationService { get; } = new(MockBehavior.Strict);

            public Mock<ISetupCompletionService> SetupCompletionService { get; } = new(MockBehavior.Strict);

            public SetupViewModel ViewModel { get; }

            public Mock<IWpf> Wpf { get; } = new(MockBehavior.Strict);

            public async Task LoadAsync(CancellationToken cancellationToken)
            {
                ViewModel.OnNavigatedTo(null!);
                await WaitUntilAsync(() => !ViewModel.IsLoading, cancellationToken);

                Assert.Equal(OriginalConnectionString, ViewModel.ConnectionString);
                Assert.False(ViewModel.IsSetupComplete);
            }

            public async Task PrepareCompletionStepAsync(string connectionString, string repositoryPath, CancellationToken cancellationToken)
            {
                await PrepareRepositoryStepAsync(connectionString, cancellationToken);

                ViewModel.RepositoryLocationPath = repositoryPath;
                ViewModel.ValidateRepositoryLocationCommand.Execute();

                await WaitUntilAsync(() => !ViewModel.IsValidatingRepositoryLocation && ViewModel.IsRepositoryLocationValid == true, cancellationToken);

                ViewModel.ContinueCommand.Execute();

                Assert.True(ViewModel.IsVerificationStep);
                Assert.True(ViewModel.ContinueCommand.CanExecute());

                ViewModel.ContinueCommand.Execute();

                Assert.True(ViewModel.IsCompletionStep);
                Assert.True(ViewModel.CompleteSetupCommand.CanExecute());
            }

            public async Task PrepareRepositoryStepAsync(string connectionString, CancellationToken cancellationToken)
            {
                await LoadAsync(cancellationToken);

                ViewModel.ConnectionString = connectionString;
                ViewModel.TestDatabaseConnectionCommand.Execute();

                await WaitUntilAsync(() => !ViewModel.IsTestingDatabase && ViewModel.IsDatabaseConnectionSuccessful == true, cancellationToken);

                Assert.True(ViewModel.CanContinueFromDatabase);

                ViewModel.ContinueCommand.Execute();

                Assert.True(ViewModel.IsRepositoryStep);
            }

            private static ApplicationConfiguration CreateApplicationConfiguration() => new()
            {
                DatabaseConfiguration = new DatabaseConfiguration { ConnectionString = OriginalConnectionString },
                IsSetupComplete = false,
                RepositoryConfiguration = new RepositoryConfiguration { AutomaticallyRepairPaths = true },
                SecurityConfiguration = new SecurityConfiguration { AllowSelfDeletion = false, AllowSelfModification = true, HideOtherUsersFromRegularUsers = true },
                SetupVersion = ApplicationConfiguration.CurrentSetupVersion
            };
        }
    }
}