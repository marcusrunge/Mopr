namespace MarcusRunge.Mopr.Workbench.Services.Application.Models.UserAdministration;

/// <summary>
/// Describes one persisted MOPR user for administrative display.
/// </summary>
public sealed record UserAdministrationSummary(int Id, string LoginName, string FirstName, string MiddleName, string LastName, string ShortName, bool IsActive, bool IsCurrentUser, string AcademicTitle, string Suffix, string PersonnelNumber, string SecurityIdentifier, byte[] RowVersion);
