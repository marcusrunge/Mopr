using MarcusRunge.Mopr.Workbench.Contracts.Application.Administration.Services;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Lifetime.Services;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.RepositoryRecovery;
using MarcusRunge.Mopr.Workbench.Services.Application.Models.RepositoryRecovery;
using MarcusRunge.Mopr.Workbench.Core;
using MarcusRunge.Mopr.Workbench.Modules.Miras.Properties;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.RepositoryAdministration;
using MarcusRunge.Mopr.Workbench.Services.Miras;
using MarcusRunge.Mopr.Workbench.Services.Miras.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Miras.Enums;
using MarcusRunge.Mopr.Workbench.Services.Miras.Models;
using System.Collections.ObjectModel;
using WpfApplication = System.Windows.Application;

namespace MarcusRunge.Mopr.Workbench.Modules.Miras.ViewModels
{
    /// <summary>
    /// Provides the protected MIRAS startup state, exposes structured integrity findings,
    /// and controls explicit repository recovery actions.
    /// </summary>
    public sealed class MirasActionRequiredViewModel : BindableBase, INavigationAware
    {
        private readonly IAdministrativeAuthorizationService _administrativeAuthorizationService;
        private readonly IApplication _application;
        private readonly ILifetimeService _lifetimeService;
        private readonly IMiras _miras;
        private readonly IRegionManager _regionManager;
        private readonly IRepositoryLocationAdministrationService _repositoryLocationAdministrationService;
        private readonly IRepositoryRecoveryActionService _repositoryRecoveryActionService;
        private readonly Dictionary<int, byte[]> _repositoryRowVersions = [];

        private bool _isAdministrativeContext;
        private bool _isBusy;
        private CancellationTokenSource? _navigationCancellation;
        private DelegateCommand? _recreateDirectoryCommand, _relocateCommand, _removeRepositoryCommand, _retryCommand;
        private string _repositoryPath = string.Empty;
        private MirasIssueItemViewModel? _selectedIssue;
        private string _statusMessage = string.Empty;

        /// <summary>
        /// Initializes a new instance of the <see cref="MirasActionRequiredViewModel"/> class.
        /// </summary>
        public MirasActionRequiredViewModel(IApplication application, IMiras miras, IAdministrativeAuthorizationService administrativeAuthorizationService, ILifetimeService lifetimeService, IRegionManager regionManager)
        {
            _application = application ?? throw new ArgumentNullException(nameof(application));
            _miras = miras ?? throw new ArgumentNullException(nameof(miras));
            _administrativeAuthorizationService = administrativeAuthorizationService ?? throw new ArgumentNullException(nameof(administrativeAuthorizationService));
            _lifetimeService = lifetimeService ?? throw new ArgumentNullException(nameof(lifetimeService));
            _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));
            _repositoryLocationAdministrationService = _application.RepositoryAdministration ?? throw new InvalidOperationException("The repository-location administration service has not been initialized.");
            _repositoryRecoveryActionService = _application.RepositoryRecovery ?? throw new InvalidOperationException("The repository-recovery action service has not been initialized.");

            SelectRepositoryCommand = new DelegateCommand(SelectRepository, CanSelectRepository);
            RecoverCommand = new DelegateCommand(() => _ = RecoverAsync(), CanRecover);
        }

        public bool CanCorrectSelectedRepository => HasAdministrativeAuthorization && !IsBusy && SelectedIssue is { IssueType: MirasIssueType.RepositoryUnavailable, RepositoryLocationId: not null };
        public static string Description => Resources.MirasActionRequiredDescription;
        public bool HasAdministrativeAuthorization => _administrativeAuthorizationService.IsElevatedAdministrator;
        public bool HasIssues => Issues.Count > 0;
        public bool HasSelectedIssue => SelectedIssue is not null;
        /// <summary>
        /// Gets a value indicating whether the view is being used from the administrative workflow.
        /// </summary>
        public bool IsAdministrativeContext
        {
            get => _isAdministrativeContext;
            private set
            {
                if (!SetProperty(ref _isAdministrativeContext, value)) return;
                RaisePropertyChanged(nameof(IsInfrastructureRecoveryVisible));
            }
        }
        public bool IsInfrastructureRecoveryVisible => !IsAdministrativeContext && !HasIssues;
        public ObservableCollection<MirasIssueItemViewModel> Issues { get; } = [];

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (!SetProperty(ref _isBusy, value)) return;

                SelectRepositoryCommand.RaiseCanExecuteChanged();
                RecoverCommand.RaiseCanExecuteChanged();
                RaiseRepositoryActionStates();
            }
        }

        public DelegateCommand RecreateDirectoryCommand => _recreateDirectoryCommand ??= new DelegateCommand(() => _ = ExecuteRepositoryActionAsync((issue, token) => _repositoryRecoveryActionService.RecreateDirectoryAsync(issue.RepositoryLocationId!.Value, issue.RepositoryPath, token)), () => CanCorrectSelectedRepository);

        public DelegateCommand RecoverCommand { get; }

        public DelegateCommand RelocateCommand => _relocateCommand ??= new DelegateCommand(() => _ = RelocateAsync(), () => CanCorrectSelectedRepository && _application.DialogService?.FileDialogService is not null);

        public DelegateCommand RemoveRepositoryCommand => _removeRepositoryCommand ??= new DelegateCommand(() => _ = RemoveRepositoryAsync(), () => CanCorrectSelectedRepository && SelectedIssue?.IsRepositoryDefault == false);

        public string RepositoryPath
        {
            get => _repositoryPath;
            set
            {
                if (!SetProperty(ref _repositoryPath, value ?? string.Empty)) return;

                StatusMessage = string.Empty;
                RecoverCommand.RaiseCanExecuteChanged();
            }
        }

        public DelegateCommand RetryCommand => _retryCommand ??= new DelegateCommand(() => _ = RetryAsync(), () => !IsBusy && _miras.Flow is not null);

        public MirasIssueItemViewModel? SelectedIssue
        {
            get => _selectedIssue;
            set
            {
                if (!SetProperty(ref _selectedIssue, value)) return;

                RaisePropertyChanged(nameof(HasSelectedIssue));
                RaiseRepositoryActionStates();
            }
        }

        public DelegateCommand SelectRepositoryCommand { get; }

        public string StatusMessage
        {
            get => _statusMessage;
            private set => SetProperty(ref _statusMessage, value);
        }

        public static string Title => Resources.MirasActionRequiredTitle;

        /// <inheritdoc/>
        public bool IsNavigationTarget(NavigationContext navigationContext) => true;

        /// <inheritdoc/>
        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            _navigationCancellation?.Cancel();
            _navigationCancellation?.Dispose();
            _navigationCancellation = null;
        }

        /// <inheritdoc/>
        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            _navigationCancellation?.Cancel();
            _navigationCancellation?.Dispose();
            _navigationCancellation = CancellationTokenSource.CreateLinkedTokenSource(_lifetimeService.ApplicationStopping);
            IsAdministrativeContext = navigationContext.Parameters.GetValue<bool>(NavigationParameterNames.MirasAdministrativeContext);

            RaisePropertyChanged(nameof(HasAdministrativeAuthorization));
            SelectRepositoryCommand.RaiseCanExecuteChanged();
            RecoverCommand.RaiseCanExecuteChanged();
            RaiseRepositoryActionStates();

            _ = LoadIssuesAsync(_navigationCancellation.Token);
        }

        private bool CanRecover() => HasAdministrativeAuthorization && IsInfrastructureRecoveryVisible && !IsBusy && !string.IsNullOrWhiteSpace(RepositoryPath) && _miras.RepositoryInfrastructureRecovery is not null && _miras.Flow is not null;

        private bool CanSelectRepository() => HasAdministrativeAuthorization && IsInfrastructureRecoveryVisible && !IsBusy && _application.DialogService?.FileDialogService is not null;

        private async Task ExecuteRepositoryActionAsync(Func<MirasIssueItemViewModel, CancellationToken, Task<RepositoryRecoveryActionResult>> action)
        {
            ArgumentNullException.ThrowIfNull(action);

            var issue = SelectedIssue;

            if (issue is null)
            {
                return;
            }

            var cancellationToken = _navigationCancellation?.Token ?? _lifetimeService.ApplicationStopping;

            try
            {
                IsBusy = true;
                StatusMessage = Resources.MirasActionRequiredCorrectionRunning;

                var result = await action(issue, cancellationToken).ConfigureAwait(true);

                if (!result.IsSuccess)
                {
                    StatusMessage = GetRepositoryActionStatusMessage(result.Status);
                    return;
                }

                await RetryCoreAsync(cancellationToken).ConfigureAwait(true);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch
            {
                StatusMessage = Resources.MirasActionRequiredCorrectionFailed;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task CompleteSuccessfulVerificationAsync(string statusMessage, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Administrative verification must remain inside the administration workflow.
            // Only the protected startup workflow may release the imaging workbench directly.
            if (IsAdministrativeContext)
            {
                await LoadIssuesAsync(cancellationToken).ConfigureAwait(true);
                StatusMessage = statusMessage;
                return;
            }

            await NavigateAsync(NavigationNames.Imaging, cancellationToken).ConfigureAwait(true);
        }
        private static string GetRecoveryStatusMessage(RepositoryInfrastructureRecoveryStatus status) => status switch
        {
            RepositoryInfrastructureRecoveryStatus.AdministrativeAuthorizationRequired => Resources.MirasActionRequiredAdministratorRequired,
            RepositoryInfrastructureRecoveryStatus.RepositoryValidationFailed => Resources.MirasActionRequiredRepositoryValidationFailed,
            RepositoryInfrastructureRecoveryStatus.RepositoryAlreadyConfigured => Resources.MirasActionRequiredRepositoryAlreadyConfigured,
            RepositoryInfrastructureRecoveryStatus.PersistenceUnavailable => Resources.MirasActionRequiredPersistenceUnavailable,
            RepositoryInfrastructureRecoveryStatus.Failed => Resources.MirasActionRequiredRecoveryFailed,
            RepositoryInfrastructureRecoveryStatus.NotStarted or RepositoryInfrastructureRecoveryStatus.Completed or _ => Resources.MirasActionRequiredRecoveryFailed
        };

        private static string GetRepositoryActionStatusMessage(RepositoryRecoveryActionStatus status) => status switch
        {
            RepositoryRecoveryActionStatus.Conflict => Resources.MirasActionRequiredCorrectionConflict,
            RepositoryRecoveryActionStatus.DefaultRepository => Resources.MirasActionRequiredRemoveDefaultRepository,
            RepositoryRecoveryActionStatus.InUse => Resources.MirasActionRequiredRemoveInUse,
            RepositoryRecoveryActionStatus.DuplicatePath => Resources.MirasActionRequiredDuplicatePath,
            RepositoryRecoveryActionStatus.ValidationFailed => Resources.MirasActionRequiredRepositoryValidationFailed,
            RepositoryRecoveryActionStatus.NotFound => Resources.MirasActionRequiredRepositoryNotFound,
            RepositoryRecoveryActionStatus.Unauthorized => Resources.MirasActionRequiredAdministratorRequired,
            RepositoryRecoveryActionStatus.Failed or _ => Resources.MirasActionRequiredCorrectionFailed
        };

        private byte[] GetRepositoryRowVersion(int repositoryLocationId)
        {
            if (!_repositoryRowVersions.TryGetValue(repositoryLocationId, out var rowVersion) || rowVersion.Length == 0)
            {
                throw new InvalidOperationException($"Repository location '{repositoryLocationId}' does not have an available concurrency token.");
            }

            return [.. rowVersion];
        }

        private async Task LoadIssuesAsync(CancellationToken cancellationToken)
        {
            try
            {
                IsBusy = true;
                Issues.Clear();
                SelectedIssue = null;
                _repositoryRowVersions.Clear();

                var result = _miras.Flow?.LastResult;

                if (result is null)
                {
                    StatusMessage = Resources.MirasActionRequiredResultUnavailable;
                    RaiseIssueState();
                    return;
                }

                var repositories = await _repositoryLocationAdministrationService.GetAllAsync(cancellationToken).ConfigureAwait(true);
                var repositoriesById = repositories.ToDictionary(repository => repository.Id);
                var messagesByIssueId = result.Messages.GroupBy(message => message.IssueId).ToDictionary(group => group.Key, group => group.First());

                foreach (var repository in repositories)
                {
                    _repositoryRowVersions[repository.Id] = [.. repository.RowVersion];
                }

                foreach (var issue in result.Issues)
                {
                    repositoriesById.TryGetValue(issue.RepositoryLocationId ?? 0, out var repository);
                    messagesByIssueId.TryGetValue(issue.Id, out var message);

                    var repositoryPath = repository?.RootPath
                        ?? (!string.IsNullOrWhiteSpace(issue.ExpectedFilePath)
                            ? issue.ExpectedFilePath
                            : issue.ActualFilePath);

                    Issues.Add(new MirasIssueItemViewModel(
                        issue.Id,
                        issue.IssueType,
                        issue.IssueState,
                        issue.RecommendedAction,
                        message?.Title ?? issue.IssueType.ToString(),
                        message?.Description ?? Resources.MirasActionRequiredIssueDescriptionUnavailable,
                        issue.RepositoryLocationId,
                        repository?.Name ?? Resources.MirasActionRequiredUnknownRepository,
                        repositoryPath ?? string.Empty,
                        repository?.IsEnabled,
                        repository?.IsDefault));
                }

                SelectedIssue = Issues.FirstOrDefault();
                StatusMessage = HasIssues
                    ? string.Empty
                    : HasAdministrativeAuthorization
                        ? Resources.MirasActionRequiredSelectRepositoryNotice
                        : Resources.MirasActionRequiredAdministratorRequired;

                RaiseIssueState();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch
            {
                StatusMessage = Resources.MirasActionRequiredIssueLoadFailed;
                RaiseIssueState();
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task NavigateAsync(string navigationTarget, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            using var cancellationRegistration = cancellationToken.Register(() => completion.TrySetCanceled(cancellationToken));
            var dispatcher = WpfApplication.Current?.Dispatcher ?? throw new InvalidOperationException("The WPF application dispatcher is not available.");

            await dispatcher.InvokeAsync(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                _regionManager.RequestNavigate(RegionNames.ContentRegion, navigationTarget, result =>
                {
                    if (result.Success)
                    {
                        completion.TrySetResult();
                        return;
                    }

                    var exception = result.Exception ?? new InvalidOperationException($"Navigation to '{navigationTarget}' was not completed.");
                    completion.TrySetException(new InvalidOperationException($"Navigation to '{navigationTarget}' in region '{RegionNames.ContentRegion}' failed.", exception));
                });
            });

            await completion.Task.ConfigureAwait(false);
        }

        private void RaiseIssueState()
        {
            RaisePropertyChanged(nameof(HasIssues));
            RaisePropertyChanged(nameof(IsInfrastructureRecoveryVisible));
            SelectRepositoryCommand.RaiseCanExecuteChanged();
            RecoverCommand.RaiseCanExecuteChanged();
            RaiseRepositoryActionStates();
        }

        private void RaiseRepositoryActionStates()
        {
            RaisePropertyChanged(nameof(CanCorrectSelectedRepository));
            _recreateDirectoryCommand?.RaiseCanExecuteChanged();
            _relocateCommand?.RaiseCanExecuteChanged();
            _removeRepositoryCommand?.RaiseCanExecuteChanged();
            _retryCommand?.RaiseCanExecuteChanged();
        }

        private async Task RecoverAsync()
        {
            var cancellationToken = _navigationCancellation?.Token ?? _lifetimeService.ApplicationStopping;
            var recoveryService = _miras.RepositoryInfrastructureRecovery;
            var flow = _miras.Flow;

            if (recoveryService is null || flow is null)
            {
                StatusMessage = Resources.MirasActionRequiredRecoveryUnavailable;
                return;
            }

            IsBusy = true;
            StatusMessage = Resources.MirasActionRequiredRecoveryRunning;

            try
            {
                var recoveryResult = await recoveryService.RecoverAsync(new RepositoryInfrastructureRecoveryRequest(RepositoryPath), cancellationToken).ConfigureAwait(false);

                cancellationToken.ThrowIfCancellationRequested();

                if (!recoveryResult.IsSuccessful)
                {
                    StatusMessage = GetRecoveryStatusMessage(recoveryResult.Status);
                    return;
                }

                StatusMessage = Resources.MirasActionRequiredVerificationRunning;

                /*
                 * Infrastructure recovery is intentionally narrow. A complete MIRAS
                 * assessment remains authoritative for releasing the imaging workbench.
                 */
                var verificationResult = await flow.StartAsync(cancellationToken).ConfigureAwait(false);

                cancellationToken.ThrowIfCancellationRequested();

                if (!MirasImagingReleasePolicy.CanOpenImaging(verificationResult))
                {
                    StatusMessage = Resources.MirasActionRequiredVerificationFailed;
                    await LoadIssuesAsync(cancellationToken).ConfigureAwait(true);
                    return;
                }

                await CompleteSuccessfulVerificationAsync(Resources.MirasActionRequiredRecoveryCompleted, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch
            {
                StatusMessage = Resources.MirasActionRequiredRecoveryFailed;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task RelocateAsync()
        {
            var issue = SelectedIssue;
            var fileDialogService = _application.DialogService?.FileDialogService;

            if (issue?.RepositoryLocationId is not int repositoryLocationId || fileDialogService is null)
            {
                return;
            }

            var selectedPath = fileDialogService.SelectFolder(Resources.MirasActionRequiredRelocateFolderDialogTitle, null);

            if (string.IsNullOrWhiteSpace(selectedPath))
            {
                return;
            }

            var rowVersion = GetRepositoryRowVersion(repositoryLocationId);

            await ExecuteRepositoryActionAsync((selectedIssue, token) =>
                _repositoryRecoveryActionService.RelocateAsync(
                    repositoryLocationId,
                    selectedIssue.RepositoryName,
                    selectedPath,
                    selectedIssue.IsRepositoryEnabled == true,
                    selectedIssue.IsRepositoryDefault == true,
                    rowVersion,
                    token)).ConfigureAwait(true);
        }

        private async Task RemoveRepositoryAsync()
        {
            var issue = SelectedIssue;

            if (issue?.RepositoryLocationId is not int repositoryLocationId)
            {
                return;
            }

            var rowVersion = GetRepositoryRowVersion(repositoryLocationId);

            await ExecuteRepositoryActionAsync((_, token) => _repositoryRecoveryActionService.RemoveAsync(repositoryLocationId, rowVersion, token)).ConfigureAwait(true);
        }

        private async Task RetryAsync()
        {
            var cancellationToken = _navigationCancellation?.Token ?? _lifetimeService.ApplicationStopping;

            try
            {
                IsBusy = true;
                await RetryCoreAsync(cancellationToken).ConfigureAwait(true);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch
            {
                StatusMessage = Resources.MirasActionRequiredVerificationFailed;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task RetryCoreAsync(CancellationToken cancellationToken)
        {
            var flow = _miras.Flow ?? throw new InvalidOperationException("The MIRAS flow is unavailable.");

            StatusMessage = Resources.MirasActionRequiredVerificationRunning;

            var result = await flow.StartAsync(cancellationToken).ConfigureAwait(true);

            cancellationToken.ThrowIfCancellationRequested();

            if (MirasImagingReleasePolicy.CanOpenImaging(result))
            {
                await CompleteSuccessfulVerificationAsync(Resources.MirasActionRequiredRecoveryCompleted, cancellationToken).ConfigureAwait(true);
                return;
            }

            await LoadIssuesAsync(cancellationToken).ConfigureAwait(true);
            StatusMessage = Resources.MirasActionRequiredVerificationFailed;
        }

        private void SelectRepository()
        {
            var fileDialogService = _application.DialogService?.FileDialogService;

            if (fileDialogService is null)
            {
                StatusMessage = Resources.MirasActionRequiredRecoveryUnavailable;
                return;
            }

            var initialDirectory = string.IsNullOrWhiteSpace(RepositoryPath) ? null : RepositoryPath;
            var selectedPath = fileDialogService.SelectFolder(Resources.MirasActionRequiredFolderDialogTitle, initialDirectory);

            if (!string.IsNullOrWhiteSpace(selectedPath))
            {
                RepositoryPath = selectedPath;
            }
        }
    }
}
