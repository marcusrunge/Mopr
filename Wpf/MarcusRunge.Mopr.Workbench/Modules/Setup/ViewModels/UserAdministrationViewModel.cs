using MarcusRunge.Mopr.Workbench.Contracts.Application.Lifetime.Services;
using MarcusRunge.Mopr.Workbench.Contracts.Application.UserAdministration.Services;
using MarcusRunge.Mopr.Workbench.Modules.Setup.Properties;
using Prism.Mvvm;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MarcusRunge.Mopr.Workbench.Modules.Setup.ViewModels;

/// <summary>
/// Provides read-only MOPR user discovery and detail selection.
/// </summary>
public sealed class UserAdministrationViewModel : BindableBase
{
    private readonly CancellationToken _applicationStopping;
    private readonly IUserAdministrationService _administrationService;
    private bool _isBusy;
    private UserAdministrationItemViewModel? _selectedUser;
    private string _statusText = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserAdministrationViewModel"/> class.
    /// </summary>
    public UserAdministrationViewModel(IUserAdministrationService administrationService, ILifetimeService lifetimeService)
    {
        _administrationService = administrationService ?? throw new ArgumentNullException(nameof(administrationService));
        ArgumentNullException.ThrowIfNull(lifetimeService);
        _applicationStopping = lifetimeService.ApplicationStopping;
        _ = LoadUsersAsync(_applicationStopping);
    }

    public bool HasSelectedUser => SelectedUser is not null;
    public bool IsBusy { get => _isBusy; private set => SetProperty(ref _isBusy, value); }
    public UserAdministrationItemViewModel? SelectedUser { get => _selectedUser; set { if (!SetProperty(ref _selectedUser, value)) return; RaisePropertyChanged(nameof(HasSelectedUser)); } }
    public string StatusText { get => _statusText; private set => SetProperty(ref _statusText, value); }
    public ObservableCollection<UserAdministrationItemViewModel> Users { get; } = [];

    private async Task LoadUsersAsync(CancellationToken cancellationToken)
    {
        try
        {
            IsBusy = true;
            StatusText = Resources.UserAdministration_Loading;
            var summaries = await _administrationService.GetAllAsync(cancellationToken).ConfigureAwait(true);
            Users.Clear();
            foreach (var user in summaries) Users.Add(new UserAdministrationItemViewModel(user.Id, user.LoginName, user.FirstName, user.MiddleName, user.LastName, user.ShortName, user.IsActive, user.IsCurrentUser, user.AcademicTitle, user.Suffix, user.PersonnelNumber, user.SecurityIdentifier, user.RowVersion));
            SelectedUser = Users.FirstOrDefault(user => user.IsCurrentUser) ?? Users.FirstOrDefault();
            StatusText = Users.Count == 0 ? Resources.UserAdministration_NoUsers : string.Empty;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { StatusText = Resources.UserAdministration_Canceled; }
        catch { StatusText = Resources.UserAdministration_LoadFailed; }
        finally { IsBusy = false; }
    }
}
