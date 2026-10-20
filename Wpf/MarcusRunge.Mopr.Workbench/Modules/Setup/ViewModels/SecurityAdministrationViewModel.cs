using MarcusRunge.Mopr.Workbench.Contracts.Application.Configuration.Services;
using MarcusRunge.Mopr.Workbench.Contracts.Enums;
using MarcusRunge.Mopr.Workbench.Contracts.Models.Configuration;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Lifetime.Services;
using MarcusRunge.Mopr.Workbench.Modules.Setup.Properties;
using Prism.Commands;
using Prism.Mvvm;
using System.Threading.Tasks;
using System.Threading;
using System;

namespace MarcusRunge.Mopr.Workbench.Modules.Setup.ViewModels;

/// <summary>
/// Provides controlled administration of the machine-wide MOPR security configuration.
/// </summary>
public sealed class SecurityAdministrationViewModel : BindableBase
{
    private readonly CancellationToken _applicationStopping;
    private readonly IMachineConfigurationService _configurationService;
    private bool _allowSelfDeletion, _allowSelfModification, _hideOtherUsersFromRegularUsers, _isBusy;
    private bool _originalAllowSelfDeletion, _originalAllowSelfModification, _originalHideOtherUsersFromRegularUsers;
    private DelegateCommand? _discardCommand, _reloadCommand, _saveCommand;
    private string _statusText = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="SecurityAdministrationViewModel"/> class.
    /// </summary>
    public SecurityAdministrationViewModel(IMachineConfigurationService configurationService, ILifetimeService lifetimeService)
    {
        _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        ArgumentNullException.ThrowIfNull(lifetimeService);
        _applicationStopping = lifetimeService.ApplicationStopping;
        _ = LoadAsync(_applicationStopping);
    }

    public bool AllowSelfDeletion { get => _allowSelfDeletion; set { if (!SetProperty(ref _allowSelfDeletion, value)) return; OnSettingChanged(); } }
    public bool AllowSelfModification { get => _allowSelfModification; set { if (!SetProperty(ref _allowSelfModification, value)) return; OnSettingChanged(); } }
    public bool CanModify => _configurationService.CanModify;
    public DelegateCommand DiscardCommand => _discardCommand ??= new DelegateCommand(ExecuteDiscard, () => !IsBusy && IsDirty);
    public bool HideOtherUsersFromRegularUsers { get => _hideOtherUsersFromRegularUsers; set { if (!SetProperty(ref _hideOtherUsersFromRegularUsers, value)) return; OnSettingChanged(); } }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (!SetProperty(ref _isBusy, value)) return;
            RaisePropertyChanged(nameof(IsInteractionEnabled));
            RaiseCommandStates();
        }
    }

    public bool IsDirty => AllowSelfDeletion != _originalAllowSelfDeletion || AllowSelfModification != _originalAllowSelfModification || HideOtherUsersFromRegularUsers != _originalHideOtherUsersFromRegularUsers;
    public bool IsInteractionEnabled => !IsBusy && CanModify;
    public DelegateCommand ReloadCommand => _reloadCommand ??= new DelegateCommand(() => _ = LoadAsync(_applicationStopping), () => !IsBusy && !IsDirty);
    public DelegateCommand SaveCommand => _saveCommand ??= new DelegateCommand(() => _ = SaveAsync(_applicationStopping), () => IsInteractionEnabled && IsDirty);
    public string StatusText { get => _statusText; private set => SetProperty(ref _statusText, value); }

    private void ApplyOriginalValues()
    {
        _allowSelfDeletion = _originalAllowSelfDeletion;
        _allowSelfModification = _originalAllowSelfModification;
        _hideOtherUsersFromRegularUsers = _originalHideOtherUsersFromRegularUsers;
        RaiseSettingProperties();
    }

    private void CaptureOriginalValues()
    {
        _originalAllowSelfDeletion = AllowSelfDeletion;
        _originalAllowSelfModification = AllowSelfModification;
        _originalHideOtherUsersFromRegularUsers = HideOtherUsersFromRegularUsers;
        RaisePropertyChanged(nameof(IsDirty));
        RaiseCommandStates();
    }

    private void ExecuteDiscard()
    {
        ApplyOriginalValues();
        StatusText = Resources.SecurityAdministration_ChangesDiscarded;
    }

    private static string GetStatusText(SecurityConfigurationUpdateStatus status) => status switch
    {
        SecurityConfigurationUpdateStatus.Unauthorized => Resources.SecurityAdministration_Unauthorized,
        SecurityConfigurationUpdateStatus.ConfigurationUnavailable => Resources.SecurityAdministration_ConfigurationUnavailable,
        SecurityConfigurationUpdateStatus.ValidationFailed => Resources.SecurityAdministration_ValidationFailed,
        SecurityConfigurationUpdateStatus.Failed or SecurityConfigurationUpdateStatus.NotStarted or SecurityConfigurationUpdateStatus.Completed or _ => Resources.SecurityAdministration_SaveFailed
    };

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        try
        {
            IsBusy = true;
            StatusText = Resources.SecurityAdministration_Loading;
            var configuration = await _configurationService.LoadAsync(cancellationToken).ConfigureAwait(true);
            _allowSelfDeletion = configuration.Security.AllowSelfDeletion;
            _allowSelfModification = configuration.Security.AllowSelfModification;
            _hideOtherUsersFromRegularUsers = configuration.Security.HideOtherUsersFromRegularUsers;
            RaiseSettingProperties();
            CaptureOriginalValues();
            StatusText = CanModify ? string.Empty : Resources.SecurityAdministration_ReadOnly;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { StatusText = Resources.SecurityAdministration_Canceled; }
        catch { StatusText = Resources.SecurityAdministration_LoadFailed; }
        finally { IsBusy = false; }
    }

    private void OnSettingChanged()
    {
        StatusText = string.Empty;
        RaisePropertyChanged(nameof(IsDirty));
        RaiseCommandStates();
    }

    private void RaiseCommandStates()
    {
        _discardCommand?.RaiseCanExecuteChanged();
        _reloadCommand?.RaiseCanExecuteChanged();
        _saveCommand?.RaiseCanExecuteChanged();
    }

    private void RaiseSettingProperties()
    {
        RaisePropertyChanged(nameof(AllowSelfDeletion));
        RaisePropertyChanged(nameof(AllowSelfModification));
        RaisePropertyChanged(nameof(HideOtherUsersFromRegularUsers));
        RaisePropertyChanged(nameof(IsDirty));
        RaiseCommandStates();
    }

    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            IsBusy = true;
            StatusText = Resources.SecurityAdministration_Saving;
            var request = new SecurityConfigurationUpdateRequest(AllowSelfDeletion, AllowSelfModification, HideOtherUsersFromRegularUsers);
            var result = await _configurationService.UpdateSecurityAsync(request, cancellationToken).ConfigureAwait(true);
            if (!result.IsSuccess) { StatusText = GetStatusText(result.Status); return; }
            CaptureOriginalValues();
            StatusText = Resources.SecurityAdministration_Saved;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { StatusText = Resources.SecurityAdministration_Canceled; }
        catch { StatusText = Resources.SecurityAdministration_SaveFailed; }
        finally { IsBusy = false; }
    }
}
