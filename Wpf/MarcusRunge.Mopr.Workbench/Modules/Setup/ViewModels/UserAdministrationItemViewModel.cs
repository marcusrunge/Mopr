using System.Linq;

namespace MarcusRunge.Mopr.Workbench.Modules.Setup.ViewModels;

/// <summary>
/// Represents one persisted MOPR user in the administration view.
/// </summary>
public sealed class UserAdministrationItemViewModel(int id, string loginName, string firstName, string middleName, string lastName, string shortName, bool isActive, bool isCurrentUser, string academicTitle, string suffix, string personnelNumber, string securityIdentifier, byte[] rowVersion)
{
    public string AcademicTitle { get; } = academicTitle;
    public string DisplayName { get; } = BuildDisplayName(academicTitle, firstName, middleName, lastName, suffix, loginName);
    public string FirstName { get; } = firstName;
    public int Id { get; } = id;
    public bool IsActive { get; } = isActive;
    public bool IsCurrentUser { get; } = isCurrentUser;
    public string LastName { get; } = lastName;
    public string LoginName { get; } = loginName;
    public string MiddleName { get; } = middleName;
    public string PersonnelNumber { get; } = personnelNumber;
    public byte[] RowVersion { get; } = [.. rowVersion];
    public string SecurityIdentifier { get; } = securityIdentifier;
    public string ShortName { get; } = shortName;
    public string Suffix { get; } = suffix;

    private static string BuildDisplayName(string academicTitle, string firstName, string middleName, string lastName, string suffix, string loginName)
    {
        var values = new[] { academicTitle, firstName, middleName, lastName, suffix }.Where(value => !string.IsNullOrWhiteSpace(value));
        var displayName = string.Join(" ", values);
        return string.IsNullOrWhiteSpace(displayName) ? loginName : displayName;
    }
}
