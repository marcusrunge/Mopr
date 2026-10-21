using MarcusRunge.Mopr.Workbench.Contracts.Application.Identity;
using Prism.Mvvm;
using System;
using System.Linq;

namespace MarcusRunge.Mopr.Workbench.Modules.Setup.ViewModels;

/// <summary>
/// Represents one persisted MOPR user or one temporary creation preview in the administration view.
/// </summary>
public sealed class UserAdministrationItemViewModel(int id, string loginName, string firstName, string middleName, string lastName, string shortName, bool isActive, bool isCurrentUser, string academicTitle, string suffix, string personnelNumber, string securityIdentifier, byte[] rowVersion, bool isPreview = false) : BindableBase
{
    private string _academicTitle = academicTitle, _displayName = BuildDisplayName(academicTitle, firstName, middleName, lastName, suffix, loginName), _firstName = firstName, _lastName = lastName, _loginName = loginName, _middleName = middleName, _personnelNumber = personnelNumber, _securityIdentifier = securityIdentifier, _shortName = shortName, _suffix = suffix;
    private bool _isActive = isActive;

    public string AcademicTitle => _academicTitle;
    public string DisplayName => _displayName;
    public string FirstName => _firstName;
    public int Id { get; } = id;
    public bool IsActive => _isActive;
    public bool IsCurrentUser { get; } = isCurrentUser;
    public bool IsPreview { get; } = isPreview;
    public bool IsSystemUser => !IsPreview && string.Equals(LoginName, WellKnownUserLoginNames.System, StringComparison.OrdinalIgnoreCase);
    public string LastName => _lastName;
    public string LoginName => _loginName;
    public string MiddleName => _middleName;
    public string PersonnelNumber => _personnelNumber;
    public byte[] RowVersion { get; } = [.. rowVersion];
    public string SecurityIdentifier => _securityIdentifier;
    public string ShortName => _shortName;
    public string Suffix => _suffix;

    /// <summary>
    /// Updates the temporary master-list preview from the current editor values.
    /// </summary>
    public void UpdatePreview(string loginName, string firstName, string middleName, string lastName, string shortName, bool isActive, string academicTitle, string suffix, string personnelNumber, string securityIdentifier)
    {
        if (!IsPreview) throw new InvalidOperationException("Only a temporary user preview can be updated.");

        _loginName = loginName;
        _firstName = firstName;
        _middleName = middleName;
        _lastName = lastName;
        _shortName = shortName;
        _isActive = isActive;
        _academicTitle = academicTitle;
        _suffix = suffix;
        _personnelNumber = personnelNumber;
        _securityIdentifier = securityIdentifier;
        _displayName = BuildDisplayName(academicTitle, firstName, middleName, lastName, suffix, loginName);

        RaisePropertyChanged(nameof(LoginName));
        RaisePropertyChanged(nameof(FirstName));
        RaisePropertyChanged(nameof(MiddleName));
        RaisePropertyChanged(nameof(LastName));
        RaisePropertyChanged(nameof(ShortName));
        RaisePropertyChanged(nameof(IsActive));
        RaisePropertyChanged(nameof(AcademicTitle));
        RaisePropertyChanged(nameof(Suffix));
        RaisePropertyChanged(nameof(PersonnelNumber));
        RaisePropertyChanged(nameof(SecurityIdentifier));
        RaisePropertyChanged(nameof(DisplayName));
    }

    private static string BuildDisplayName(string academicTitle, string firstName, string middleName, string lastName, string suffix, string loginName)
    {
        var values = new[] { academicTitle, firstName, middleName, lastName, suffix }.Where(value => !string.IsNullOrWhiteSpace(value));
        var displayName = string.Join(" ", values);
        return string.IsNullOrWhiteSpace(displayName) ? loginName : displayName;
    }
}
