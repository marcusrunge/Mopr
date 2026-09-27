using MarcusRunge.Mopr.Workbench.Contracts.Application.Identity;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Identity.Models;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Identity.Services;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Lifetime.Services;
using MarcusRunge.Mopr.Workbench.Contracts.Enums;
using MarcusRunge.Mopr.Workbench.Core;
using MarcusRunge.Mopr.Workbench.Modules.Identity.Properties;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Identity;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Navigation.Regions;

namespace MarcusRunge.Mopr.Workbench.Modules.Identity.ViewModels
{
    /// <summary>
    /// Provides the guided setup of the persistent MOPR user assigned to the current Windows identity.
    /// </summary>
    public sealed class UserProvisioningViewModel : BindableBase, INavigationAware
    {
        private readonly IApplication _application;
        private readonly ILifetimeService _lifetimeService;
        private readonly IOperatingSystemIdentityProvider _operatingSystemIdentityProvider;
        private readonly IRegionManager _regionManager;
        private CancellationTokenSource? _navigationCancellation;
        private string _firstName = string.Empty;
        private bool _isBusy;
        private string _lastName = string.Empty;
        private string _loginName = string.Empty;
        private string _shortName = string.Empty;
        private string _statusMessage = string.Empty;
        private string _validationMessage = string.Empty;

        public UserProvisioningViewModel(IApplication application, IOperatingSystemIdentityProvider operatingSystemIdentityProvider, ILifetimeService lifetimeService, IRegionManager regionManager)
        {
            _application = application ?? throw new ArgumentNullException(nameof(application));
            _operatingSystemIdentityProvider = operatingSystemIdentityProvider ?? throw new ArgumentNullException(nameof(operatingSystemIdentityProvider));
            _lifetimeService = lifetimeService ?? throw new ArgumentNullException(nameof(lifetimeService));
            _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));
            ProvisionCommand = new DelegateCommand(() => _ = ProvisionAsync(), CanProvision);
        }

        public string FirstName
        {
            get => _firstName;
            set
            {
                if (SetProperty(ref _firstName, value ?? string.Empty))
                {
                    ClearValidationMessage();
                    ProvisionCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public bool HasStatusMessage => !string.IsNullOrWhiteSpace(StatusMessage);

        public bool HasValidationMessage => !string.IsNullOrWhiteSpace(ValidationMessage);

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    ProvisionCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public string LastName
        {
            get => _lastName;
            set
            {
                if (SetProperty(ref _lastName, value ?? string.Empty))
                {
                    ClearValidationMessage();
                    ProvisionCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public string LoginName
        {
            get => _loginName;
            private set
            {
                if (SetProperty(ref _loginName, value))
                {
                    ProvisionCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public DelegateCommand ProvisionCommand { get; }

        public string ShortName
        {
            get => _shortName;
            set
            {
                if (SetProperty(ref _shortName, value ?? string.Empty))
                {
                    ClearValidationMessage();
                    ProvisionCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            private set
            {
                if (SetProperty(ref _statusMessage, value))
                {
                    RaisePropertyChanged(nameof(HasStatusMessage));
                }
            }
        }

        public string ValidationMessage
        {
            get => _validationMessage;
            private set
            {
                if (SetProperty(ref _validationMessage, value))
                {
                    RaisePropertyChanged(nameof(HasValidationMessage));
                }
            }
        }

        public bool IsNavigationTarget(NavigationContext navigationContext) => true;

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            _navigationCancellation?.Cancel();
            _navigationCancellation?.Dispose();
            _navigationCancellation = null;
        }

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            _navigationCancellation?.Cancel();
            _navigationCancellation?.Dispose();
            _navigationCancellation = CancellationTokenSource.CreateLinkedTokenSource(_lifetimeService.ApplicationStopping);
            _ = LoadOperatingSystemIdentityAsync(_navigationCancellation.Token);
        }

        private bool CanProvision() => !IsBusy && !string.IsNullOrWhiteSpace(LoginName) && !string.IsNullOrWhiteSpace(FirstName) && !string.IsNullOrWhiteSpace(LastName) && !string.IsNullOrWhiteSpace(ShortName);

        private void ClearValidationMessage()
        {
            if (!string.IsNullOrEmpty(ValidationMessage))
            {
                ValidationMessage = string.Empty;
            }
        }

        private async Task LoadOperatingSystemIdentityAsync(CancellationToken cancellationToken)
        {
            IsBusy = true;
            StatusMessage = Resources.IdentityProvisioningLoadingIdentity;
            ValidationMessage = string.Empty;

            try
            {
                var identity = await _operatingSystemIdentityProvider.GetCurrentIdentityAsync(cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();

                if (identity is null)
                {
                    Navigate(NavigationNames.IdentityUnavailable);
                    return;
                }

                LoginName = identity.LoginName;
                StatusMessage = string.Empty;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch
            {
                Navigate(NavigationNames.IdentityUnavailable);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void Navigate(string target) => _regionManager.RequestNavigate(RegionNames.ContentRegion, target);

        private async Task ProvisionAsync()
        {
            var cancellationToken = _navigationCancellation?.Token ?? _lifetimeService.ApplicationStopping;
            var provisioningService = _application.IdentityService?.UserProvisioningService;

            if (provisioningService is null)
            {
                Navigate(NavigationNames.IdentityUnavailable);
                return;
            }

            IsBusy = true;
            StatusMessage = Resources.IdentityProvisioningSaving;
            ValidationMessage = string.Empty;

            try
            {
                var request = new UserProvisioningRequest(FirstName, LastName, ShortName);
                var result = await provisioningService.ProvisionAsync(request, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();

                switch (result.Status)
                {
                    case UserProvisioningStatus.Completed:
                    case UserProvisioningStatus.UserAlreadyExists:
                        Navigate(NavigationNames.Imaging);
                        break;

                    case UserProvisioningStatus.UserDisabled:
                        Navigate(NavigationNames.IdentityBlocked);
                        break;

                    case UserProvisioningStatus.ValidationFailed:
                        ValidationMessage = CreateValidationMessage(result.ValidationIssues);
                        StatusMessage = string.Empty;
                        break;

                    case UserProvisioningStatus.NotStarted:
                    case UserProvisioningStatus.OperatingSystemIdentityUnavailable:
                    case UserProvisioningStatus.PersistenceUnavailable:
                    case UserProvisioningStatus.Failed:
                    default:
                        Navigate(NavigationNames.IdentityUnavailable);
                        break;
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch
            {
                Navigate(NavigationNames.IdentityUnavailable);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private static string CreateValidationMessage(IReadOnlyList<UserProvisioningValidationIssue> issues)
        {
            var messages = issues.Select(GetValidationMessage).Distinct().ToArray();
            return messages.Length == 0 ? Resources.IdentityProvisioningValidationSummary : $"{Resources.IdentityProvisioningValidationSummary}{Environment.NewLine}{string.Join(Environment.NewLine, messages.Select(message => $"• {message}"))}";
        }

        private static string GetValidationMessage(UserProvisioningValidationIssue issue) => issue switch
        {
            UserProvisioningValidationIssue.FirstNameRequired => Resources.IdentityProvisioningFirstNameRequired,
            UserProvisioningValidationIssue.FirstNameTooLong => Resources.IdentityProvisioningFirstNameTooLong,
            UserProvisioningValidationIssue.LastNameRequired => Resources.IdentityProvisioningLastNameRequired,
            UserProvisioningValidationIssue.LastNameTooLong => Resources.IdentityProvisioningLastNameTooLong,
            UserProvisioningValidationIssue.ShortNameRequired => Resources.IdentityProvisioningShortNameRequired,
            UserProvisioningValidationIssue.ShortNameTooLong => Resources.IdentityProvisioningShortNameTooLong,
            UserProvisioningValidationIssue.ShortNameInvalid => Resources.IdentityProvisioningShortNameInvalid,
            _ => Resources.IdentityProvisioningValidationSummary
        };
    }
}