using MarcusRunge.Mopr.Workbench.Contracts.Application.Lifetime.Services;
using MarcusRunge.Mopr.Workbench.Modules.Setup.Properties;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.UserAdministration;
using MarcusRunge.Mopr.Workbench.Services.Application.Enums;
using MarcusRunge.Mopr.Workbench.Services.Application.Models.UserAdministration;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MarcusRunge.Mopr.Workbench.Modules.Setup.ViewModels;

/// <summary>
/// Provides MOPR user discovery, creation, editing, activation management, and persistence.
/// </summary>
public sealed class UserAdministrationViewModel : BindableBase
{
    private readonly IUserAdministrationService _administrationService;
    private readonly CancellationToken _applicationStopping;
    private string _academicTitle = string.Empty, _firstName = string.Empty, _lastName = string.Empty, _loginName = string.Empty, _middleName = string.Empty, _personnelNumber = string.Empty, _securityIdentifier = string.Empty, _shortName = string.Empty, _statusText = string.Empty, _suffix = string.Empty;
    private DelegateCommand? _cancelCreateCommand, _discardCommand, _newCommand, _reloadCommand, _saveCommand;
    private bool _isActive, _isBusy, _isCreating;
    private UserAdministrationItemViewModel? _selectedUser;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserAdministrationViewModel"/> class.
    /// </summary>
    public UserAdministrationViewModel(IApplication application, ILifetimeService lifetimeService)
    {
        _administrationService = (application ?? throw new ArgumentNullException(nameof(application))).UserAdministration ?? throw new InvalidOperationException("The user-administration service has not been initialized.");
        ArgumentNullException.ThrowIfNull(lifetimeService);
        _applicationStopping = lifetimeService.ApplicationStopping;
        _ = LoadUsersAsync(_applicationStopping);
    }

    public string AcademicTitle { get => _academicTitle; set { if (!SetProperty(ref _academicTitle, value ?? string.Empty)) return; OnEditorChanged(); } }
    public bool CanChangeActivation => IsEditorEnabled && (IsCreating || SelectedUser is { IsCurrentUser: false });
    public bool CanSelectUser => !IsBusy && !IsCreating && !IsDirty;
    public DelegateCommand CancelCreateCommand => _cancelCreateCommand ??= new DelegateCommand(ExecuteCancelCreate, () => !IsBusy && IsCreating);
    public DelegateCommand DiscardCommand => _discardCommand ??= new DelegateCommand(ExecuteDiscard, () => !IsBusy && !IsCreating && IsDirty);
    public string FirstName { get => _firstName; set { if (!SetProperty(ref _firstName, value ?? string.Empty)) return; OnEditorChanged(); } }
    public bool HasEditorContext => IsCreating || SelectedUser is not null;
    public bool HasSelectedUser => SelectedUser is not null;
    public bool IsActive { get => _isActive; set { if (!IsCreating && SelectedUser?.IsCurrentUser == true && !value || !SetProperty(ref _isActive, value)) return; OnEditorChanged(); } }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (!SetProperty(ref _isBusy, value)) return;
            RaiseEditorState();
        }
    }

    public bool IsCreating
    {
        get => _isCreating;
        private set
        {
            if (!SetProperty(ref _isCreating, value)) return;
            RaisePropertyChanged(nameof(HasEditorContext));
            RaiseEditorState();
        }
    }

    public bool IsDirty => IsCreating ? !string.IsNullOrWhiteSpace(LoginName) || !string.IsNullOrWhiteSpace(FirstName) || !string.IsNullOrWhiteSpace(MiddleName) || !string.IsNullOrWhiteSpace(LastName) || !string.IsNullOrWhiteSpace(ShortName) || !string.IsNullOrWhiteSpace(AcademicTitle) || !string.IsNullOrWhiteSpace(Suffix) || !string.IsNullOrWhiteSpace(PersonnelNumber) || !string.IsNullOrWhiteSpace(SecurityIdentifier) || !IsActive : SelectedUser is not null && (!string.Equals(AcademicTitle.Trim(), SelectedUser.AcademicTitle, StringComparison.Ordinal) || !string.Equals(FirstName.Trim(), SelectedUser.FirstName, StringComparison.Ordinal) || !string.Equals(MiddleName.Trim(), SelectedUser.MiddleName, StringComparison.Ordinal) || !string.Equals(LastName.Trim(), SelectedUser.LastName, StringComparison.Ordinal) || !string.Equals(Suffix.Trim(), SelectedUser.Suffix, StringComparison.Ordinal) || !string.Equals(ShortName.Trim(), SelectedUser.ShortName, StringComparison.Ordinal) || !string.Equals(PersonnelNumber.Trim(), SelectedUser.PersonnelNumber, StringComparison.OrdinalIgnoreCase) || !string.Equals(LoginName.Trim(), SelectedUser.LoginName, StringComparison.OrdinalIgnoreCase) || !string.Equals(SecurityIdentifier.Trim(), SelectedUser.SecurityIdentifier, StringComparison.OrdinalIgnoreCase) || IsActive != SelectedUser.IsActive);
    public bool IsEditorEnabled => !IsBusy && (IsCreating || SelectedUser is { IsSystemUser: false });
    public string LastName { get => _lastName; set { if (!SetProperty(ref _lastName, value ?? string.Empty)) return; OnEditorChanged(); } }
    public string LoginName { get => _loginName; set { if (!SetProperty(ref _loginName, value ?? string.Empty)) return; OnEditorChanged(); } }
    public string MiddleName { get => _middleName; set { if (!SetProperty(ref _middleName, value ?? string.Empty)) return; OnEditorChanged(); } }
    public DelegateCommand NewCommand => _newCommand ??= new DelegateCommand(ExecuteNew, () => !IsBusy && !IsCreating && !IsDirty);
    public string PersonnelNumber { get => _personnelNumber; set { if (!SetProperty(ref _personnelNumber, value ?? string.Empty)) return; OnEditorChanged(); } }
    public DelegateCommand ReloadCommand => _reloadCommand ??= new DelegateCommand(ExecuteReload, () => !IsBusy && !IsCreating && !IsDirty);
    public DelegateCommand SaveCommand => _saveCommand ??= new DelegateCommand(ExecuteSave, CanSave);
    public string SecurityIdentifier { get => _securityIdentifier; set { if (!SetProperty(ref _securityIdentifier, value ?? string.Empty)) return; OnEditorChanged(); } }

    public UserAdministrationItemViewModel? SelectedUser
    {
        get => _selectedUser;
        set
        {
            if (IsCreating || IsDirty || !SetProperty(ref _selectedUser, value)) return;
            ApplySelection(value);
        }
    }

    public string ShortName { get => _shortName; set { if (!SetProperty(ref _shortName, value ?? string.Empty)) return; OnEditorChanged(); } }
    public string StatusText { get => _statusText; private set => SetProperty(ref _statusText, value); }
    public string Suffix { get => _suffix; set { if (!SetProperty(ref _suffix, value ?? string.Empty)) return; OnEditorChanged(); } }
    public ObservableCollection<UserAdministrationItemViewModel> Users { get; } = [];

    private void ApplySelection(UserAdministrationItemViewModel? user)
    {
        _academicTitle = user?.AcademicTitle ?? string.Empty;
        _firstName = user?.FirstName ?? string.Empty;
        _middleName = user?.MiddleName ?? string.Empty;
        _lastName = user?.LastName ?? string.Empty;
        _suffix = user?.Suffix ?? string.Empty;
        _shortName = user?.ShortName ?? string.Empty;
        _personnelNumber = user?.PersonnelNumber ?? string.Empty;
        _loginName = user?.LoginName ?? string.Empty;
        _securityIdentifier = user?.SecurityIdentifier ?? string.Empty;
        _isActive = user?.IsActive ?? false;
        RaiseEditorProperties();
        StatusText = string.Empty;
    }

    private bool CanSave() => !IsBusy && IsEditorEnabled && (IsCreating || IsDirty) && !string.IsNullOrWhiteSpace(LoginName) && !string.IsNullOrWhiteSpace(FirstName) && !string.IsNullOrWhiteSpace(LastName) && !string.IsNullOrWhiteSpace(ShortName) && (IsCreating || SelectedUser?.IsCurrentUser != true || IsActive);

    private void ExecuteCancelCreate()
    {
        IsCreating = false;
        var user = Users.FirstOrDefault(item => item.IsCurrentUser) ?? Users.FirstOrDefault();
        _selectedUser = user;
        RaisePropertyChanged(nameof(SelectedUser));
        ApplySelection(user);
        StatusText = Resources.UserAdministration_CreationCanceled;
    }

    private void ExecuteDiscard()
    {
        ApplySelection(SelectedUser);
        StatusText = Resources.UserAdministration_ChangesDiscarded;
    }

    private void ExecuteNew()
    {
        _selectedUser = null;
        RaisePropertyChanged(nameof(SelectedUser));
        IsCreating = true;
        _academicTitle = _firstName = _lastName = _loginName = _middleName = _personnelNumber = _securityIdentifier = _shortName = _suffix = string.Empty;
        _isActive = true;
        RaiseEditorProperties();
        StatusText = Resources.UserAdministration_NewInstructions;
    }

    private void ExecuteReload() => _ = LoadUsersAsync(_applicationStopping, SelectedUser?.Id);
    private async void ExecuteSave() => await SaveAsync(_applicationStopping);

    private static string GetCreateStatusText(UserAdministrationCreateStatus status) => status switch
    {
        UserAdministrationCreateStatus.SystemUserReserved => Resources.UserAdministration_SystemUserReserved,
        UserAdministrationCreateStatus.DuplicateLoginName => Resources.UserAdministration_DuplicateLoginName,
        UserAdministrationCreateStatus.DuplicateSecurityIdentifier => Resources.UserAdministration_DuplicateSecurityIdentifier,
        UserAdministrationCreateStatus.DuplicatePersonnelNumber => Resources.UserAdministration_DuplicatePersonnelNumber,
        UserAdministrationCreateStatus.Unauthorized => Resources.UserAdministration_Unauthorized,
        UserAdministrationCreateStatus.ValidationFailed => Resources.UserAdministration_ValidationFailed,
        UserAdministrationCreateStatus.Conflict => Resources.UserAdministration_CreateConflict,
        UserAdministrationCreateStatus.Failed or UserAdministrationCreateStatus.NotStarted or UserAdministrationCreateStatus.Completed or _ => Resources.UserAdministration_CreateFailed
    };

    private static string GetUpdateStatusText(UserAdministrationUpdateStatus status) => status switch
    {
        UserAdministrationUpdateStatus.NotFound => Resources.UserAdministration_NotFound,
        UserAdministrationUpdateStatus.Conflict => Resources.UserAdministration_Conflict,
        UserAdministrationUpdateStatus.SystemUserProtected => Resources.UserAdministration_SystemUserProtected,
        UserAdministrationUpdateStatus.SelfDeactivationNotAllowed => Resources.UserAdministration_SelfDeactivationNotAllowed,
        UserAdministrationUpdateStatus.SelfModificationNotAllowed => Resources.UserAdministration_SelfModificationNotAllowed,
        UserAdministrationUpdateStatus.DuplicateLoginName => Resources.UserAdministration_DuplicateLoginName,
        UserAdministrationUpdateStatus.DuplicateSecurityIdentifier => Resources.UserAdministration_DuplicateSecurityIdentifier,
        UserAdministrationUpdateStatus.DuplicatePersonnelNumber => Resources.UserAdministration_DuplicatePersonnelNumber,
        UserAdministrationUpdateStatus.Unauthorized => Resources.UserAdministration_Unauthorized,
        UserAdministrationUpdateStatus.ValidationFailed => Resources.UserAdministration_ValidationFailed,
        UserAdministrationUpdateStatus.Failed or UserAdministrationUpdateStatus.NotStarted or UserAdministrationUpdateStatus.Completed or _ => Resources.UserAdministration_SaveFailed
    };

    private async Task LoadUsersAsync(CancellationToken cancellationToken, int? selectedUserId = null)
    {
        try
        {
            IsBusy = true;
            StatusText = Resources.UserAdministration_Loading;
            var summaries = await _administrationService.GetAllAsync(cancellationToken).ConfigureAwait(true);
            Users.Clear();
            foreach (var user in summaries) Users.Add(Map(user));
            _selectedUser = selectedUserId.HasValue ? Users.FirstOrDefault(user => user.Id == selectedUserId) : Users.FirstOrDefault(user => user.IsCurrentUser) ?? Users.FirstOrDefault();
            RaisePropertyChanged(nameof(SelectedUser));
            IsCreating = false;
            ApplySelection(_selectedUser);
            StatusText = Users.Count == 0 ? Resources.UserAdministration_NoUsers : string.Empty;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { StatusText = Resources.UserAdministration_Canceled; }
        catch { StatusText = Resources.UserAdministration_LoadFailed; }
        finally { IsBusy = false; }
    }

    private static UserAdministrationItemViewModel Map(UserAdministrationSummary user) => new(user.Id, user.LoginName, user.FirstName, user.MiddleName, user.LastName, user.ShortName, user.IsActive, user.IsCurrentUser, user.AcademicTitle, user.Suffix, user.PersonnelNumber, user.SecurityIdentifier, user.RowVersion);
    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private void OnEditorChanged()
    {
        StatusText = string.Empty;
        RaisePropertyChanged(nameof(IsDirty));
        RaisePropertyChanged(nameof(CanSelectUser));
        RaiseCommandStates();
    }

    private void RaiseCommandStates()
    {
        _cancelCreateCommand?.RaiseCanExecuteChanged();
        _discardCommand?.RaiseCanExecuteChanged();
        _newCommand?.RaiseCanExecuteChanged();
        _reloadCommand?.RaiseCanExecuteChanged();
        _saveCommand?.RaiseCanExecuteChanged();
    }

    private void RaiseEditorProperties()
    {
        RaisePropertyChanged(nameof(AcademicTitle));
        RaisePropertyChanged(nameof(FirstName));
        RaisePropertyChanged(nameof(MiddleName));
        RaisePropertyChanged(nameof(LastName));
        RaisePropertyChanged(nameof(Suffix));
        RaisePropertyChanged(nameof(ShortName));
        RaisePropertyChanged(nameof(PersonnelNumber));
        RaisePropertyChanged(nameof(LoginName));
        RaisePropertyChanged(nameof(SecurityIdentifier));
        RaisePropertyChanged(nameof(IsActive));
        RaisePropertyChanged(nameof(HasSelectedUser));
        RaisePropertyChanged(nameof(HasEditorContext));
        RaiseEditorState();
    }

    private void RaiseEditorState()
    {
        RaisePropertyChanged(nameof(IsDirty));
        RaisePropertyChanged(nameof(IsEditorEnabled));
        RaisePropertyChanged(nameof(CanChangeActivation));
        RaisePropertyChanged(nameof(CanSelectUser));
        RaiseCommandStates();
    }

    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            IsBusy = true;

            if (IsCreating)
            {
                StatusText = Resources.UserAdministration_Creating;
                var createRequest = new UserAdministrationCreateRequest(LoginName.Trim(), FirstName.Trim(), NormalizeOptional(MiddleName), LastName.Trim(), ShortName.Trim(), NormalizeOptional(AcademicTitle), NormalizeOptional(Suffix), NormalizeOptional(PersonnelNumber), NormalizeOptional(SecurityIdentifier), IsActive);
                var createResult = await _administrationService.CreateAsync(createRequest, cancellationToken).ConfigureAwait(true);
                if (!createResult.IsSuccess || createResult.User is null) { StatusText = GetCreateStatusText(createResult.Status); return; }
                await LoadUsersAsync(cancellationToken, createResult.User.Id).ConfigureAwait(true);
                StatusText = Resources.UserAdministration_Created;
                return;
            }

            var selectedUser = SelectedUser;
            if (selectedUser is null) return;

            StatusText = Resources.UserAdministration_Saving;
            var updateRequest = new UserAdministrationUpdateRequest(selectedUser.Id, LoginName.Trim(), FirstName.Trim(), NormalizeOptional(MiddleName), LastName.Trim(), ShortName.Trim(), NormalizeOptional(AcademicTitle), NormalizeOptional(Suffix), NormalizeOptional(PersonnelNumber), NormalizeOptional(SecurityIdentifier), IsActive, [.. selectedUser.RowVersion]);
            var updateResult = await _administrationService.UpdateAsync(updateRequest, cancellationToken).ConfigureAwait(true);
            if (!updateResult.IsSuccess || updateResult.User is null) { StatusText = GetUpdateStatusText(updateResult.Status); return; }
            await LoadUsersAsync(cancellationToken, updateResult.User.Id).ConfigureAwait(true);
            StatusText = Resources.UserAdministration_Saved;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { StatusText = Resources.UserAdministration_Canceled; }
        catch { StatusText = IsCreating ? Resources.UserAdministration_CreateFailed : Resources.UserAdministration_SaveFailed; }
        finally { IsBusy = false; }
    }
}
