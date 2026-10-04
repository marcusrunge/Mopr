using MarcusRunge.Mopr.Workbench.Contracts.Application.Administration.Services;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Identity.Models;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Identity.Services;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Lifetime.Services;
using MarcusRunge.Mopr.Workbench.Contracts.Enums;
using MarcusRunge.Mopr.Workbench.Core;
using MarcusRunge.Mopr.Workbench.Core.Events;
using MarcusRunge.Mopr.Workbench.Modules.Identity.Properties;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts;

namespace MarcusRunge.Mopr.Workbench.Modules.Identity.ViewModels
{
    /// <summary>
    /// Provides the guided setup of the first persistent personal MOPR user.
    /// </summary>
    public sealed class UserProvisioningViewModel : BindableBase, INavigationAware
    {
        private readonly IAdministrativeAuthorizationService _administrativeAuthorizationService;
        private readonly IApplication _application;
        private readonly IEventAggregator _eventAggregator;
        private readonly ILifetimeService _lifetimeService;
        private readonly IOperatingSystemIdentityProvider _operatingSystemIdentityProvider;
        private readonly IRegionManager _regionManager;

        private string _academicTitle = string.Empty;
        private string _firstName = string.Empty;
        private bool _isBusy;
        private string _lastName = string.Empty;
        private string _loginName = string.Empty;
        private CancellationTokenSource? _navigationCancellation;
        private string _personnelNumber = string.Empty;
        private string _shortName = string.Empty;
        private string _statusMessage = string.Empty;
        private string _validationMessage = string.Empty;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserProvisioningViewModel"/> class.
        /// </summary>
        public UserProvisioningViewModel(IApplication application, IAdministrativeAuthorizationService administrativeAuthorizationService, IOperatingSystemIdentityProvider operatingSystemIdentityProvider, ILifetimeService lifetimeService, IRegionManager regionManager, IEventAggregator eventAggregator)
        {
            _application = application ?? throw new ArgumentNullException(nameof(application));
            _administrativeAuthorizationService = administrativeAuthorizationService ?? throw new ArgumentNullException(nameof(administrativeAuthorizationService));
            _operatingSystemIdentityProvider = operatingSystemIdentityProvider ?? throw new ArgumentNullException(nameof(operatingSystemIdentityProvider));
            _lifetimeService = lifetimeService ?? throw new ArgumentNullException(nameof(lifetimeService));
            _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));

            ProvisionCommand = new DelegateCommand(() => _ = ProvisionAsync(), CanProvision);
        }

        /// <summary>
        /// Gets or sets the optional academic title.
        /// </summary>
        public string AcademicTitle
        {
            get => _academicTitle;
            set
            {
                if (SetProperty(ref _academicTitle, value ?? string.Empty))
                {
                    ClearValidationMessage();
                }
            }
        }

        /// <summary>
        /// Gets or sets the first name entered by the administrator.
        /// </summary>
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

        /// <summary>
        /// Gets a value indicating whether the current process may provision the first persistent MOPR user.
        /// </summary>
        public bool HasAdministrativeAuthorization => _administrativeAuthorizationService.IsElevatedAdministrator;

        /// <summary>
        /// Gets a value indicating whether a status message is available.
        /// </summary>
        public bool HasStatusMessage => !string.IsNullOrWhiteSpace(StatusMessage);

        /// <summary>
        /// Gets a value indicating whether a validation message is available.
        /// </summary>
        public bool HasValidationMessage => !string.IsNullOrWhiteSpace(ValidationMessage);

        /// <summary>
        /// Gets a value indicating whether elevated administrative authorization is required.
        /// </summary>
        public bool IsAdministrativeAuthorizationRequired => !HasAdministrativeAuthorization;

        /// <summary>
        /// Gets a value indicating whether an identity or provisioning operation is running.
        /// </summary>
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

        /// <summary>
        /// Gets or sets the last name entered by the administrator.
        /// </summary>
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

        /// <summary>
        /// Gets the automatically resolved Windows login name.
        /// </summary>
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

        /// <summary>
        /// Gets or sets the optional organization-specific personnel number.
        /// </summary>
        public string PersonnelNumber
        {
            get => _personnelNumber;
            set
            {
                if (SetProperty(ref _personnelNumber, value ?? string.Empty))
                {
                    ClearValidationMessage();
                }
            }
        }

        /// <summary>
        /// Gets the command that provisions the first persistent MOPR user.
        /// </summary>
        public DelegateCommand ProvisionCommand { get; }

        /// <summary>
        /// Gets or sets the short name entered by the administrator.
        /// </summary>
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

        /// <summary>
        /// Gets the current user-facing operation status.
        /// </summary>
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

        /// <summary>
        /// Gets the current user-facing validation message.
        /// </summary>
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
            RaisePropertyChanged(nameof(IsAdministrativeAuthorizationRequired));
            ProvisionCommand.RaiseCanExecuteChanged();

            _ = LoadOperatingSystemIdentityAsync(_navigationCancellation.Token);
        }

        private static string CreateValidationMessage(IReadOnlyList<UserProvisioningValidationIssue> issues)
        {
            var messages = issues.Select(GetValidationMessage).Distinct().ToArray();

            return messages.Length == 0
                ? Resources.IdentityProvisioningValidationSummary
                : $"{Resources.IdentityProvisioningValidationSummary}{Environment.NewLine}{string.Join(Environment.NewLine, messages.Select(message => $"• {message}"))}";
        }

        private static string GetValidationMessage(UserProvisioningValidationIssue issue) => issue switch
        {
            UserProvisioningValidationIssue.AcademicTitleTooLong => Resources.IdentityProvisioningAcademicTitleTooLong,
            UserProvisioningValidationIssue.FirstNameRequired => Resources.IdentityProvisioningFirstNameRequired,
            UserProvisioningValidationIssue.FirstNameTooLong => Resources.IdentityProvisioningFirstNameTooLong,
            UserProvisioningValidationIssue.LastNameRequired => Resources.IdentityProvisioningLastNameRequired,
            UserProvisioningValidationIssue.LastNameTooLong => Resources.IdentityProvisioningLastNameTooLong,
            UserProvisioningValidationIssue.PersonnelNumberTooLong => Resources.IdentityProvisioningPersonnelNumberTooLong,
            UserProvisioningValidationIssue.ShortNameRequired => Resources.IdentityProvisioningShortNameRequired,
            UserProvisioningValidationIssue.ShortNameTooLong => Resources.IdentityProvisioningShortNameTooLong,
            UserProvisioningValidationIssue.ShortNameInvalid => Resources.IdentityProvisioningShortNameInvalid,
            _ => Resources.IdentityProvisioningValidationSummary
        };

        private bool CanProvision() =>
            HasAdministrativeAuthorization &&
            !IsBusy &&
            !string.IsNullOrWhiteSpace(LoginName) &&
            !string.IsNullOrWhiteSpace(FirstName) &&
            !string.IsNullOrWhiteSpace(LastName) &&
            !string.IsNullOrWhiteSpace(ShortName);

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
            if (!HasAdministrativeAuthorization)
            {
                ProvisionCommand.RaiseCanExecuteChanged();
                return;
            }

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
                var request = new UserProvisioningRequest(
                    FirstName,
                    LastName,
                    ShortName,
                    AcademicTitle,
                    PersonnelNumber);

                var result = await provisioningService.ProvisionAsync(request, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();

                switch (result.Status)
                {
                    case UserProvisioningStatus.Completed:
                    case UserProvisioningStatus.UserAlreadyExists:
                        // The product service has already populated the current-user
                        // context. The composition root owns MIRAS and final navigation.
                        _eventAggregator.GetEvent<InitialUserProvisioningCompletedEvent>().Publish();
                        break;

                    case UserProvisioningStatus.UserDisabled:
                        Navigate(NavigationNames.IdentityBlocked);
                        break;

                    case UserProvisioningStatus.AdministrativeAuthorizationRequired:
                        StatusMessage = string.Empty;
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
    }
}