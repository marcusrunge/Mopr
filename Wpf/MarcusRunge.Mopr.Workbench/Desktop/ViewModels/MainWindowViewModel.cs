using MarcusRunge.Mopr.Workbench.Contracts.Application.Identity.Models;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Identity.Services;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Lifetime.Services;
using MarcusRunge.Mopr.Workbench.Core.Mvvm;
using MarcusRunge.Mopr.Workbench.Properties;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts;
using System;
using System.Threading;
using System.Threading.Tasks;
using WpfApplication = System.Windows.Application;

namespace MarcusRunge.Mopr.Workbench.ViewModels
{
    /// <summary>
    /// Provides application-wide shell state.
    /// </summary>
    public sealed class MainWindowViewModel : ViewModelBase
    {
        private readonly CancellationToken _applicationStopping;
        private readonly ICurrentUserContext? _currentUserContext;
        private string _currentUserDetails = string.Empty;
        private string _currentUserDisplayName = string.Empty;
        private bool _hasCurrentUser;
        private string _title = Resources.MainWindowTitle;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindowViewModel"/> class.
        /// </summary>
        /// <param name="application">The application-service facade.</param>
        /// <param name="lifetimeService">The application lifetime service.</param>
        public MainWindowViewModel(IApplication application, ILifetimeService lifetimeService)
        {
            ArgumentNullException.ThrowIfNull(application);
            ArgumentNullException.ThrowIfNull(lifetimeService);

            _applicationStopping = lifetimeService.ApplicationStopping;
            _currentUserContext = application.IdentityService?.CurrentUserContext;

            if (_currentUserContext is null)
            {
                return;
            }

            // Subscribe before reading the initial snapshot so a sign-in occurring
            // during shell construction cannot be missed.
            _currentUserContext.CurrentUserChanged += OnCurrentUserChanged;
            _ = LoadCurrentUserAsync(_applicationStopping);
        }

        /// <summary>
        /// Gets the details of the current persistent MOPR user.
        /// </summary>
        public string CurrentUserDetails
        {
            get => _currentUserDetails;
            private set => SetProperty(ref _currentUserDetails, value);
        }

        /// <summary>
        /// Gets the display name of the current persistent MOPR user.
        /// </summary>
        public string CurrentUserDisplayName
        {
            get => _currentUserDisplayName;
            private set => SetProperty(ref _currentUserDisplayName, value);
        }

        /// <summary>
        /// Gets a value indicating whether an active persistent MOPR user is available.
        /// </summary>
        public bool HasCurrentUser
        {
            get => _hasCurrentUser;
            private set => SetProperty(ref _hasCurrentUser, value);
        }

        /// <summary>
        /// Gets or sets the window title.
        /// </summary>
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
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