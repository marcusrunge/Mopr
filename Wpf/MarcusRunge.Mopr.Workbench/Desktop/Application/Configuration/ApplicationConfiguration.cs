using MarcusRunge.Mopr.Workbench.Contracts.Application.Configuration;

namespace MarcusRunge.Mopr.Workbench.Application.Configuration
{
    public sealed class ApplicationConfiguration : IApplicationConfiguration
    {        

        public IRepositoryConfiguration Repository { get; } = new RepositoryConfiguration();

        public ISecurityConfiguration Security { get; } = new SecurityConfiguration();
    }
}