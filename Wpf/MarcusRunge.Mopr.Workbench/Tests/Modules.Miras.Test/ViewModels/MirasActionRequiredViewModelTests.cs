using MarcusRunge.Mopr.Workbench.Services.Miras.Enums;
using MarcusRunge.Mopr.Workbench.Services.Miras.Models;
using Moq;
using Xunit;

namespace MarcusRunge.Mopr.Workbench.Modules.Miras.Test.ViewModels
{
    public sealed class MirasActionRequiredViewModelTests
    {
        [Fact]
        public void Commands_WhenAdministrativeAuthorizationIsMissing_AreDisabled()
        {
            using var context = new MirasActionRequiredViewModelTestContext(isElevatedAdministrator: false);

            context.ViewModel.RepositoryPath = MirasActionRequiredViewModelTestContext.SelectedRepositoryPath;

            Assert.False(context.ViewModel.HasAdministrativeAuthorization);
            Assert.False(context.ViewModel.SelectRepositoryCommand.CanExecute());
            Assert.False(context.ViewModel.RecoverCommand.CanExecute());
        }

        [Fact]
        public void SelectRepositoryCommand_WhenFileDialogIsUnavailable_IsDisabled()
        {
            using var context = new MirasActionRequiredViewModelTestContext(fileDialogAvailable: false);

            Assert.False(context.ViewModel.SelectRepositoryCommand.CanExecute());
        }

        [Fact]
        public void SelectRepositoryCommand_WhenFolderIsSelected_UpdatesRepositoryPathAndEnablesRecovery()
        {
            using var context = new MirasActionRequiredViewModelTestContext();
            context.ConfigureFolderSelection();

            Assert.True(context.ViewModel.SelectRepositoryCommand.CanExecute());
            Assert.False(context.ViewModel.RecoverCommand.CanExecute());

            context.ExecuteSelectRepository();

            Assert.Equal(MirasActionRequiredViewModelTestContext.SelectedRepositoryPath, context.ViewModel.RepositoryPath);
            Assert.True(context.ViewModel.RecoverCommand.CanExecute());
            context.VerifyFolderSelectionCalledOnce(initialDirectory: null);
        }

        [Fact]
        public void SelectRepositoryCommand_WhenExistingPathIsAvailable_UsesItAsInitialDirectory()
        {
            using var context = new MirasActionRequiredViewModelTestContext();
            context.ViewModel.RepositoryPath = MirasActionRequiredViewModelTestContext.ExistingRepositoryPath;
            context.ConfigureFolderSelection();

            context.ExecuteSelectRepository();

            Assert.Equal(MirasActionRequiredViewModelTestContext.SelectedRepositoryPath, context.ViewModel.RepositoryPath);
            context.VerifyFolderSelectionCalledOnce(MirasActionRequiredViewModelTestContext.ExistingRepositoryPath);
        }

        [Fact]
        public void SelectRepositoryCommand_WhenSelectionIsCanceled_PreservesExistingRepositoryPath()
        {
            using var context = new MirasActionRequiredViewModelTestContext();
            context.ViewModel.RepositoryPath = MirasActionRequiredViewModelTestContext.ExistingRepositoryPath;
            context.ConfigureCanceledFolderSelection();

            context.ExecuteSelectRepository();

            Assert.Equal(MirasActionRequiredViewModelTestContext.ExistingRepositoryPath, context.ViewModel.RepositoryPath);
            context.VerifyFolderSelectionCalledOnce(MirasActionRequiredViewModelTestContext.ExistingRepositoryPath);
            context.VerifyRecoveryNotCalled();
        }

        [Fact]
        public void RecoverCommand_WhenRepositoryPathIsMissing_IsDisabled()
        {
            using var context = new MirasActionRequiredViewModelTestContext();

            context.ViewModel.RepositoryPath = "   ";

            Assert.False(context.ViewModel.RecoverCommand.CanExecute());
        }

        [Theory]
        [InlineData(RepositoryInfrastructureRecoveryStatus.AdministrativeAuthorizationRequired)]
        [InlineData(RepositoryInfrastructureRecoveryStatus.RepositoryValidationFailed)]
        [InlineData(RepositoryInfrastructureRecoveryStatus.RepositoryAlreadyConfigured)]
        [InlineData(RepositoryInfrastructureRecoveryStatus.PersistenceUnavailable)]
        [InlineData(RepositoryInfrastructureRecoveryStatus.Failed)]
        public async Task RecoverCommand_WhenRecoveryFails_DoesNotStartMirasVerification(RepositoryInfrastructureRecoveryStatus status)
        {
            using var context = new MirasActionRequiredViewModelTestContext();
            context.ViewModel.RepositoryPath = MirasActionRequiredViewModelTestContext.SelectedRepositoryPath;
            context.ConfigureRecoveryResult(CreateFailedResult(status));

            context.ExecuteRecover();

            await MirasActionRequiredViewModelTestContext.WaitUntilAsync(() => !context.ViewModel.IsBusy && !string.IsNullOrWhiteSpace(context.ViewModel.StatusMessage), TestContext.Current.CancellationToken);

            Assert.NotEmpty(context.ViewModel.StatusMessage);
            context.VerifyRecoveryCalledOnce(MirasActionRequiredViewModelTestContext.SelectedRepositoryPath);
            context.VerifyVerificationNotStarted();
        }

        [Fact]
        public async Task RecoverCommand_WhileRecoveryIsRunning_DisablesBothCommands()
        {
            using var context = new MirasActionRequiredViewModelTestContext();
            var recoveryCompletion = new TaskCompletionSource<RepositoryInfrastructureRecoveryResult>(TaskCreationOptions.RunContinuationsAsynchronously);

            context.ViewModel.RepositoryPath = MirasActionRequiredViewModelTestContext.SelectedRepositoryPath;
            context.RecoveryService.Setup(service => service.RecoverAsync(It.IsAny<RepositoryInfrastructureRecoveryRequest>(), It.IsAny<CancellationToken>())).Returns(recoveryCompletion.Task);

            context.ExecuteRecover();

            await MirasActionRequiredViewModelTestContext.WaitUntilAsync(() => context.ViewModel.IsBusy, TestContext.Current.CancellationToken);

            Assert.False(context.ViewModel.SelectRepositoryCommand.CanExecute());
            Assert.False(context.ViewModel.RecoverCommand.CanExecute());

            recoveryCompletion.SetResult(RepositoryInfrastructureRecoveryResult.RepositoryValidationFailed());

            await MirasActionRequiredViewModelTestContext.WaitUntilAsync(() => !context.ViewModel.IsBusy, TestContext.Current.CancellationToken);

            context.VerifyRecoveryCalledOnce(MirasActionRequiredViewModelTestContext.SelectedRepositoryPath);
            context.VerifyVerificationNotStarted();
        }

        [Fact]
        public async Task RecoverCommand_WhenApplicationStops_CancelsRecovery()
        {
            using var context = new MirasActionRequiredViewModelTestContext();
            var receivedToken = CancellationToken.None;
            var recoveryStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            context.ViewModel.RepositoryPath = MirasActionRequiredViewModelTestContext.SelectedRepositoryPath;

            context.RecoveryService
                .Setup(service => service.RecoverAsync(It.IsAny<RepositoryInfrastructureRecoveryRequest>(), It.IsAny<CancellationToken>()))
                .Callback<RepositoryInfrastructureRecoveryRequest, CancellationToken>((_, cancellationToken) =>
                {
                    receivedToken = cancellationToken;
                    recoveryStarted.TrySetResult();
                })
                .Returns<RepositoryInfrastructureRecoveryRequest, CancellationToken>(async (_, cancellationToken) =>
                {
                    await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                    return RepositoryInfrastructureRecoveryResult.RepositoryValidationFailed();
                });

            context.ExecuteRecover();
            await recoveryStarted.Task.WaitAsync(TestContext.Current.CancellationToken);

            context.LifetimeService.Cancel();

            await MirasActionRequiredViewModelTestContext.WaitUntilAsync(() => !context.ViewModel.IsBusy, TestContext.Current.CancellationToken);

            Assert.True(receivedToken.CanBeCanceled);
            Assert.True(receivedToken.IsCancellationRequested);
            context.VerifyVerificationNotStarted();
        }

        private static RepositoryInfrastructureRecoveryResult CreateFailedResult(RepositoryInfrastructureRecoveryStatus status) => status switch
        {
            RepositoryInfrastructureRecoveryStatus.AdministrativeAuthorizationRequired => RepositoryInfrastructureRecoveryResult.AdministrativeAuthorizationRequired(),
            RepositoryInfrastructureRecoveryStatus.RepositoryValidationFailed => RepositoryInfrastructureRecoveryResult.RepositoryValidationFailed(),
            RepositoryInfrastructureRecoveryStatus.RepositoryAlreadyConfigured => RepositoryInfrastructureRecoveryResult.RepositoryAlreadyConfigured(),
            RepositoryInfrastructureRecoveryStatus.PersistenceUnavailable => RepositoryInfrastructureRecoveryResult.PersistenceUnavailable(),
            RepositoryInfrastructureRecoveryStatus.Failed => RepositoryInfrastructureRecoveryResult.Failed(new InvalidOperationException("Recovery failed for the test.")),
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, "The test requires a failed recovery status.")
        };
    }
}