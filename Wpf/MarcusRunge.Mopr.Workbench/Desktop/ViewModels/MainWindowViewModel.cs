using MarcusRunge.Mopr.Workbench.Contracts;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Administration.Services;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Identity.Models;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Identity.Services;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Lifetime.Services;
using MarcusRunge.Mopr.Workbench.Core;
using MarcusRunge.Mopr.Workbench.Core.Mvvm;
using MarcusRunge.Mopr.Workbench.Modules.Imaging.Models;
using MarcusRunge.Mopr.Workbench.Modules.Imaging.ViewModels;
using MarcusRunge.Mopr.Workbench.Properties;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Core.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Core.Contracts.Imaging;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Navigation;
using Prism.Navigation.Regions;
using System;
using System.Threading;
using System.Threading.Tasks;
using WpfApplication = System.Windows.Application;

namespace MarcusRunge.Mopr.Workbench.ViewModels
{
    /// <summary>
    /// Provides application-wide shell state and navigation.
    /// </summary>
    public sealed class MainWindowViewModel : ViewModelBase, IWindowClosingGuard
    {
        private readonly IAdministrativeAuthorizationService _administrativeAuthorizationService;
        private readonly CancellationToken _applicationStopping;
        private readonly ICurrentUserContext? _currentUserContext;
        private readonly Prism.Dialogs.IDialogService _dialogService;
        private readonly IImagingSessionService _imagingSessionService;
        private readonly IApplication _application;
        private readonly IRegionManager _regionManager;
        private DelegateCommand? _closeNavigationMenuCommand;
        private string _currentUserDetails = string.Empty;
        private string _currentUserDisplayName = string.Empty;
        private bool _hasCurrentUser;
        private bool _isNavigationMenuOpen;
        private DelegateCommand? _openAdministrationCommand;
        private string _title = Resources.MainWindowTitle;
        private DelegateCommand? _toggleNavigationMenuCommand;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindowViewModel"/> class.
        /// </summary>
        public MainWindowViewModel(IApplication application, ICore core, ILifetimeService lifetimeService, IAdministrativeAuthorizationService administrativeAuthorizationService, IRegionManager regionManager, Prism.Dialogs.IDialogService dialogService)
        {
            ArgumentNullException.ThrowIfNull(application);
            ArgumentNullException.ThrowIfNull(core);
            ArgumentNullException.ThrowIfNull(lifetimeService);
            ArgumentNullException.ThrowIfNull(administrativeAuthorizationService);
            ArgumentNullException.ThrowIfNull(regionManager);
            ArgumentNullException.ThrowIfNull(dialogService);

            _application = application;
            _administrativeAuthorizationService = administrativeAuthorizationService;
            _dialogService = dialogService;
            _imagingSessionService = core.ImagingService?.ImagingSessionService ?? throw new InvalidOperationException("The imaging session service is not available.");
            _applicationStopping = lifetimeService.ApplicationStopping;
            _currentUserContext = application.IdentityService?.CurrentUserContext;
            _regionManager = regionManager;

            if (_currentUserContext is null)
            {
                return;
            }

            // Subscribe before reading the initial snapshot so a sign-in occurring
            // during shell construction cannot be missed.
            _currentUserContext.CurrentUserChanged += OnCurrentUserChanged;
            _ = LoadCurrentUserAsync(_applicationStopping);
        }

        public bool CanOpenAdministration => HasCurrentUser && _administrativeAuthorizationService.IsElevatedAdministrator;
        public DelegateCommand CloseNavigationMenuCommand => _closeNavigationMenuCommand ??= new DelegateCommand(() => IsNavigationMenuOpen = false);
        public string CurrentUserDetails { get => _currentUserDetails; private set => SetProperty(ref _currentUserDetails, value); }
        public string CurrentUserDisplayName { get => _currentUserDisplayName; private set => SetProperty(ref _currentUserDisplayName, value); }
        public bool HasCurrentUser { get => _hasCurrentUser; private set => SetProperty(ref _hasCurrentUser, value); }
        public bool IsNavigationMenuOpen { get => _isNavigationMenuOpen; set => SetProperty(ref _isNavigationMenuOpen, value); }
        public string NavigationAdministrationText => Resources.ShellNavigationAdministration;
        public string NavigationCloseText => Resources.ShellNavigationClose;
        public string NavigationMenuText => Resources.ShellNavigationMenu;
        public DelegateCommand OpenAdministrationCommand => _openAdministrationCommand ??= new DelegateCommand(ExecuteOpenAdministration, () => CanOpenAdministration);
        public DelegateCommand ToggleNavigationMenuCommand => _toggleNavigationMenuCommand ??= new DelegateCommand(() => IsNavigationMenuOpen = !IsNavigationMenuOpen);
        public string Title { get => _title; set => SetProperty(ref _title, value); }

        /// <inheritdoc/>
        public async Task<bool> CanCloseWindowAsync(CancellationToken cancellationToken = default)
        {
            if (!_imagingSessionService.HasUnsavedChanges) return true;

            var completionSource = new TaskCompletionSource<UnsavedChangesDecision>(TaskCreationOptions.RunContinuationsAsynchronously);
            _dialogService.ShowDialog("UnsavedChangesDialogView", result =>
            {
                UnsavedChangesDecision decision = result.Parameters.TryGetValue(UnsavedChangesDialogViewModel.DecisionParameterName, out UnsavedChangesDecision selectedDecision) ? selectedDecision : UnsavedChangesDecision.Cancel;
                completionSource.TrySetResult(decision);
            });
            UnsavedChangesDecision decision = await completionSource.Task.WaitAsync(cancellationToken);

            if (decision == UnsavedChangesDecision.Cancel) return false;
            if (decision == UnsavedChangesDecision.Discard)
            {
                _imagingSessionService.DiscardChangesAndClear();
                return true;
            }

            try
            {
                await (_application.ImagingSession ?? throw new InvalidOperationException("The imaging session application service is not available.")).SaveAsync(cancellationToken);
                return !_imagingSessionService.HasUnsavedChanges;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return false;
            }
            catch (Exception exception)
            {
                _application.ReportException(exception);
                return false;
            }
        }

        /// <inheritdoc/>
        public override void Destroy()
        {
            _currentUserContext?.CurrentUserChanged -= OnCurrentUserChanged;
            base.Destroy();
        }

        private void ApplyCurrentUser(CurrentUser? currentUser)
        {
            var activeUser = currentUser is { Id: > 0, IsActive: true } ? currentUser : null;
            CurrentUserDisplayName = activeUser?.DisplayName ?? string.Empty;
            CurrentUserDetails = activeUser is null ? string.Empty : $"{activeUser.ShortName} · {activeUser.LoginName}";
            HasCurrentUser = activeUser is not null;
            RaisePropertyChanged(nameof(CanOpenAdministration));
            _openAdministrationCommand?.RaiseCanExecuteChanged();
        }

        private void DispatchCurrentUser(CurrentUser? currentUser)
        {
            var dispatcher = WpfApplication.Current?.Dispatcher;
            if (dispatcher is null || dispatcher.CheckAccess())
            {
                ApplyCurrentUser(currentUser);
                return;
            }

            _ = dispatcher.InvokeAsync(() => ApplyCurrentUser(currentUser));
        }

        private void ExecuteOpenAdministration()
        {
            // Menu visibility is only a usability feature. The Setup target performs
            // an independent authorization check for every Administration request.
            _administrativeAuthorizationService.DemandElevatedAdministrator();
            var parameters = new NavigationParameters { { SetupNavigation.OperatingModeParameter, SetupOperatingMode.Administration.ToString() } };
            IsNavigationMenuOpen = false;
            _regionManager.RequestNavigate(RegionNames.ContentRegion, NavigationNames.Setup, parameters);
        }

        private async Task LoadCurrentUserAsync(CancellationToken cancellationToken)
        {
            try
            {
                var currentUser = await _currentUserContext!.GetCurrentUserAsync(cancellationToken).ConfigureAwait(false);
                DispatchCurrentUser(currentUser);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Application shutdown does not require a final shell update.
            }
        }

        private void OnCurrentUserChanged(CurrentUser? currentUser) => DispatchCurrentUser(currentUser);
    }
}
