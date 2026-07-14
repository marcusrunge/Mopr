namespace MarcusRunge.Mopr.Workbench.Contracts.Application
{
    public interface ISecurityConfiguration
    {
        bool AllowSelfDeletion { get; }
        bool AllowSelfModification { get; }
        bool HideOtherUsersFromRegularUsers { get; }
        bool RestrictAdministrationToDomainAdministrators { get; }
    }
}