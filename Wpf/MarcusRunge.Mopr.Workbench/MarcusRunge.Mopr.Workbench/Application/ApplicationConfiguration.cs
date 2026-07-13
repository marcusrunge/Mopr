using MarcusRunge.Mopr.Workbench.Contracts.Application;

namespace MarcusRunge.Mopr.Workbench.Application
{
    public sealed class ApplicationConfiguration : IApplicationConfiguration
    {        

        public IRepositoryConfiguration Repository { get; } = new RepositoryConfiguration();

        public ISecurityConfiguration Security { get; } = new SecurityConfiguration();
    }
}