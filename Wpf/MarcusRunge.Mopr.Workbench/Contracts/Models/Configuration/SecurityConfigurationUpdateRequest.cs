namespace MarcusRunge.Mopr.Workbench.Contracts.Models.Configuration;

/// <summary>
/// Describes an administrative update of the machine-wide MOPR security configuration.
/// </summary>
/// <param name="AllowSelfDeletion">Indicates whether users may delete their own persistent user records in a controlled personal-user workflow.</param>
/// <param name="AllowSelfModification">Indicates whether users may modify their own persistent user records.</param>
/// <param name="HideOtherUsersFromRegularUsers">Indicates whether regular users may see other persistent MOPR users.</param>
public sealed record SecurityConfigurationUpdateRequest(bool AllowSelfDeletion, bool AllowSelfModification, bool HideOtherUsersFromRegularUsers);