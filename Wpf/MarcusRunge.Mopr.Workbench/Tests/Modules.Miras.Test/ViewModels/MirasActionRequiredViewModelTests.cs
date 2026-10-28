using MarcusRunge.Mopr.Workbench.Services.Miras.Enums;
using MarcusRunge.Mopr.Workbench.Services.Miras.Models;
using Moq;
using Xunit;

namespace MarcusRunge.Mopr.Workbench.Modules.Miras.Test.ViewModels
{
    public sealed class MirasActionRequiredViewModelTests
    {
        [Fact]
        public async Task DeferForSessionCommand_WithRepositoryUnavailable_DefersIssueAndRemovesOpenItem()
        {
            using var context = new MirasActionRequiredViewModelTestContext();
            var issue = context.ConfigureLastResult();
            context.NavigateTo(isAdministrativeContext: true);
            await MirasActionRequiredViewModelTestContext.WaitUntilAsync(() => context.ViewModel.Issues.Count == 1, TestContext.Current.CancellationToken);

            Assert.True(context.ViewModel.DeferForSessionCommand.CanExecute());

            context.ExecuteDeferForSession();

            Assert.Equal(MirasIssueState.Deferred, issue.IssueState);
            Assert.Empty(context.ViewModel.Issues);
            Assert.False(context.ViewModel.IsInfrastructureRecoveryVisible);
            Assert.Null(context.ViewModel.SelectedIssue);
            Assert.False(context.ViewModel.DeferForSessionCommand.CanExecute());
            Assert.False(context.WasSessionReleaseRequested());
        }

        [Theory]
        [InlineData(MirasIssueType.IdentityMismatch)]
        [InlineData(MirasIssueType.PersistenceRelationshipConflict)]
        public async Task DeferForSessionCommand_WithIneligibleIssue_IsDisabled(MirasIssueType issueType)
        {
            using var context = new MirasActionRequiredViewModelTestContext();
            var issue = context.ConfigureLastResult(issueType);
            context.NavigateTo(isAdministrativeContext: true);
            await MirasActionRequiredViewModelTestContext.WaitUntilAsync(() => context.ViewModel.Issues.Count == 1, TestContext.Current.CancellationToken);

            Assert.False(issue.CanDeferForSession);
            Assert.False(context.ViewModel.DeferForSessionCommand.CanExecute());
        }

        [Fact]
        public async Task DeferForSessionCommand_InStartupContext_WhenReleaseIsPermitted_RequestsSessionRelease()
        {
            using var context = new MirasActionRequiredViewModelTestContext();
            var issue = context.ConfigureLastResult();
            context.NavigateTo();
            await MirasActionRequiredViewModelTestContext.WaitUntilAsync(() => context.ViewModel.Issues.Count == 1, TestContext.Current.CancellationToken);

            context.ExecuteDeferForSession();

            Assert.Equal(MirasIssueState.Deferred, issue.IssueState);
            Assert.Empty(context.ViewModel.Issues);
            Assert.False(context.ViewModel.IsInfrastructureRecoveryVisible);
            Assert.True(context.WasSessionReleaseRequested());
        }

        [Fact]
        public async Task DeferForSessionCommand_InStartupContext_WithTechnicalError_DoesNotNavigateToImaging()
        {
            using var context = new MirasActionRequiredViewModelTestContext();
            context.ConfigureLastResult(includeTechnicalError: true);
            context.NavigateTo();
            await MirasActionRequiredViewModelTestContext.WaitUntilAsync(() => context.ViewModel.Issues.Count == 1, TestContext.Current.CancellationToken);

            context.ExecuteDeferForSession();

            Assert.False(context.WasSessionReleaseRequested());
        }

        [Fact]
        public async Task DeferForSessionCommand_InStartupContext_WithRemainingActionRequiredIssue_DoesNotNavigateToImaging()
        {
            using var context = new MirasActionRequiredViewModelTestContext();
            var deferredIssue = context.ConfigureLastResult();
            var result = context.Flow.Object.LastResult!;
            result.Issues.Add(new MirasIssue { Id = Guid.NewGuid(), IssueState = MirasIssueState.ActionRequired, IssueType = MirasIssueType.MissingFile });
            context.NavigateTo();
            await MirasActionRequiredViewModelTestContext.WaitUntilAsync(() => context.ViewModel.Issues.Count == 2, TestContext.Current.CancellationToken);
            context.ViewModel.SelectedIssue = context.ViewModel.Issues.Single(item => item.Id == deferredIssue.Id);

            context.ExecuteDeferForSession();

            Assert.Single(context.ViewModel.Issues);
            Assert.Equal(MirasIssueType.MissingFile, context.ViewModel.SelectedIssue?.IssueType);
            Assert.False(context.WasSessionReleaseRequested());
        }

        [Fact]
        public void OnNavigatedTo_WithoutAdministrativeContext_PreservesStartupBehavior()
        {
            using var context = new MirasActionRequiredViewModelTestContext();

            context.NavigateTo();

            Assert.False(context.ViewModel.IsAdministrativeContext);
            Assert.True(context.ViewModel.IsInfrastructureRecoveryVisible);
            Assert.False(context.ViewModel.ReturnToAdministrationCommand.CanExecute());
        }

        [Fact]
        public void OnNavigatedTo_WithAdministrativeContext_DisablesStartupInfrastructureRecovery()
        {
            using var context = new MirasActionRequiredViewModelTestContext();

            context.NavigateTo(isAdministrativeContext: true);

            Assert.True(context.ViewModel.IsAdministrativeContext);
            Assert.False(context.ViewModel.IsInfrastructureRecoveryVisible);
            Assert.False(context.ViewModel.SelectRepositoryCommand.CanExecute());
            Assert.False(context.ViewModel.RecoverCommand.CanExecute());
            Assert.True(context.ViewModel.ReturnToAdministrationCommand.CanExecute());
        }

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
