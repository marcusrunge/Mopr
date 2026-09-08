using MarcusRunge.Mopr.Workbench.Contracts.Application.Configuration.Models;

namespace MarcusRunge.Mopr.Workbench.Services.Repository.Test
{
    internal sealed class TestSecurityConfiguration : ISecurityConfiguration
    {
        public bool AllowSelfDeletion => false;

        public bool AllowSelfModification => true;

        public bool HideOtherUsersFromRegularUsers => true;
    }
}