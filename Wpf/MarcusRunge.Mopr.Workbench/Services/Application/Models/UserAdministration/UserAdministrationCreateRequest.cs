namespace MarcusRunge.Mopr.Workbench.Services.Application.Models.UserAdministration;

/// <summary>
/// Describes the administrative creation of one personal MOPR user.
/// </summary>
/// <param name="LoginName">The assigned operating-system login name.</param>
/// <param name="FirstName">The first name.</param>
/// <param name="MiddleName">The optional middle name.</param>
/// <param name="LastName">The last name.</param>
/// <param name="ShortName">The short name.</param>
/// <param name="AcademicTitle">The optional academic title.</param>
/// <param name="Suffix">The optional name suffix.</param>
/// <param name="PersonnelNumber">The optional organization-specific personnel number.</param>
/// <param name="SecurityIdentifier">The optional Windows security identifier.</param>
/// <param name="IsActive">Indicates whether the user may perform authenticated MOPR operations.</param>
public sealed record UserAdministrationCreateRequest(string LoginName, string FirstName, string? MiddleName, string LastName, string ShortName, string? AcademicTitle, string? Suffix, string? PersonnelNumber, string? SecurityIdentifier, bool IsActive);