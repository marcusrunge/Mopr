using MarcusRunge.Mopr.Workbench.Contracts.Application.Configuration;

namespace MarcusRunge.Mopr.Workbench.Application.Configuration
{
    public sealed class SecurityConfiguration : ISecurityConfiguration
    {
        public bool AllowSelfDeletion { get; set; } = false;
        public bool AllowSelfModification { get; set; } = true;
        public bool HideOtherUsersFromRegularUsers { get; set; } = true;
        public bool RestrictAdministrationToDomainAdministrators { get; set; } = true;
    }
}