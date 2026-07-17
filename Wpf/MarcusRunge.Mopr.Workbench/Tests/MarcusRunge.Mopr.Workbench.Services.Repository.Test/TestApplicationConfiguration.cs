using MarcusRunge.Mopr.Workbench.Contracts.Application;

namespace MarcusRunge.Mopr.Workbench.Services.Repository.Test
{
    internal sealed class TestApplicationConfiguration : IApplicationConfiguration
    {
        public IRepositoryConfiguration Repository { get; } = new TestRepositoryConfiguration();

        public ISecurityConfiguration Security { get; } = new TestSecurityConfiguration();
    }
}