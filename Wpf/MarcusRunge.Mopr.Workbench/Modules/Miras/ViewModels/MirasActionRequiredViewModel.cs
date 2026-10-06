using MarcusRunge.Mopr.Workbench.Contracts.Application.Administration.Services;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Lifetime.Services;
using MarcusRunge.Mopr.Workbench.Core;
using MarcusRunge.Mopr.Workbench.Modules.Miras.Properties;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Miras;
using MarcusRunge.Mopr.Workbench.Services.Miras.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Miras.Enums;
using MarcusRunge.Mopr.Workbench.Services.Miras.Models;
using WpfApplication = System.Windows.Application;

namespace MarcusRunge.Mopr.Workbench.Modules.Miras.ViewModels
{
    /// <summary>
    /// Provides the protected MIRAS startup state and controls the explicit
    /// administrative recovery of missing repository infrastructure.
    /// </summary>
    public sealed class MirasActionRequiredViewModel : BindableBase, INavigationAware
    {
        private readonly IAdministrativeAuthorizationService _administrativeAuthorizationService;
        private readonly IApplication _application;
        private readonly ILifetimeService _lifetimeService;
        private readonly IMiras _miras;
        private readonly IRegionManager _regionManager;

        private bool _isBusy;
        private CancellationTokenSource? _navigationCancellation;
        private string _repositoryPath = string.Empty;
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

            SelectRepositoryCommand = new DelegateCommand(SelectRepository, CanSelectRepository);
            RecoverCommand = new DelegateCommand(() => _ = RecoverAsync(), CanRecover);
        }

        /// <summary>
        /// Gets the user-oriented explanation of the protected state.
        /// </summary>
        public static string Description => Resources.MirasActionRequiredDescription;

        /// <summary>
        /// Gets a value indicating whether the current process may perform the protected recovery.
        /// </summary>
        public bool HasAdministrativeAuthorization => _administrativeAuthorizationService.IsElevatedAdministrator;

        /// <summary>
        /// Gets a value indicating whether an operation is currently running.
        /// </summary>
        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (!SetProperty(ref _isBusy, value))
                {
                    return;
                }

                SelectRepositoryCommand.RaiseCanExecuteChanged();
                RecoverCommand.RaiseCanExecuteChanged();
            }
        }

        /// <summary>
        /// Gets the command that restores the missing repository infrastructure.
        /// </summary>
        public DelegateCommand RecoverCommand { get; }

        /// <summary>
        /// Gets or sets the administrator-selected repository root path.
        /// </summary>
        public string RepositoryPath
        {
            get => _repositoryPath;
            set
            {
                if (!SetProperty(ref _repositoryPath, value ?? string.Empty))
                {
                    return;
                }

                StatusMessage = string.Empty;
                RecoverCommand.RaiseCanExecuteChanged();
            }
        }

        /// <summary>
        /// Gets the command that opens the platform folder-selection dialog.
        /// </summary>
        public DelegateCommand SelectRepositoryCommand { get; }

        /// <summary>
        /// Gets the current user-facing operation status.
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessage;
            private set => SetProperty(ref _statusMessage, value);
        }

        /// <summary>
        /// Gets the user-oriented title.
        /// </summary>
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

            RaisePropertyChanged(nameof(HasAdministrativeAuthorization));
            SelectRepositoryCommand.RaiseCanExecuteChanged();
            RecoverCommand.RaiseCanExecuteChanged();

            StatusMessage = HasAdministrativeAuthorization ? Resources.MirasActionRequiredSelectRepositoryNotice : Resources.MirasActionRequiredAdministratorRequired;
        }

        private bool CanRecover() => HasAdministrativeAuthorization && !IsBusy && !string.IsNullOrWhiteSpace(RepositoryPath) && _miras.RepositoryInfrastructureRecovery is not null && _miras.Flow is not null;

        private bool CanSelectRepository() => HasAdministrativeAuthorization && !IsBusy && _application.DialogService?.FileDialogService is not null;

        private static string GetRecoveryStatusMessage(RepositoryInfrastructureRecoveryStatus status) => status switch
        {
            RepositoryInfrastructureRecoveryStatus.AdministrativeAuthorizationRequired => Resources.MirasActionRequiredAdministratorRequired,
            RepositoryInfrastructureRecoveryStatus.RepositoryValidationFailed => Resources.MirasActionRequiredRepositoryValidationFailed,
            RepositoryInfrastructureRecoveryStatus.RepositoryAlreadyConfigured => Resources.MirasActionRequiredRepositoryAlreadyConfigured,
            RepositoryInfrastructureRecoveryStatus.PersistenceUnavailable => Resources.MirasActionRequiredPersistenceUnavailable,
            RepositoryInfrastructureRecoveryStatus.Failed => Resources.MirasActionRequiredRecoveryFailed,
            RepositoryInfrastructureRecoveryStatus.NotStarted or RepositoryInfrastructureRecoveryStatus.Completed or _ => Resources.MirasActionRequiredRecoveryFailed
        };

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
                    return;
                }

                StatusMessage = Resources.MirasActionRequiredRecoveryCompleted;
                await NavigateAsync(NavigationNames.Imaging, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch
            {
                /*
                 * Technical details remain within MIRAS diagnostics. The protected
                 * user interface never exposes database paths or exception details.
                 */
                StatusMessage = Resources.MirasActionRequiredRecoveryFailed;
            }
            finally
            {
                IsBusy = false;
            }
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