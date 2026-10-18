namespace MarcusRunge.Mopr.Workbench.Services.Application.Models.UserAdministration;

/// <summary>
/// Describes one persisted MOPR user for administrative display and editing.
/// </summary>
/// <param name="Id">The persistent user identifier.</param>
/// <param name="LoginName">The assigned operating-system login name.</param>
/// <param name="FirstName">The first name.</param>
/// <param name="MiddleName">The optional middle name.</param>
/// <param name="LastName">The last name.</param>
/// <param name="ShortName">The short name.</param>
/// <param name="IsActive">Indicates whether the user may perform authenticated MOPR operations.</param>
/// <param name="IsCurrentUser">Indicates whether the user represents the current application identity.</param>
/// <param name="AcademicTitle">The optional academic title.</param>
/// <param name="Suffix">The optional name suffix.</param>
/// <param name="PersonnelNumber">The optional organization-specific personnel number.</param>
/// <param name="SecurityIdentifier">The optional Windows security identifier.</param>
/// <param name="RowVersion">The optimistic-concurrency token.</param>
public sealed record UserAdministrationSummary(int Id, string LoginName, string FirstName, string MiddleName, string LastName, string ShortName, bool IsActive, bool IsCurrentUser, string AcademicTitle, string Suffix, string PersonnelNumber, string SecurityIdentifier, byte[] RowVersion);