namespace MarcusRunge.Mopr.Workbench.Contracts.Application.Configuration
{
    public interface IApplicationConfiguration
    {      
        ISecurityConfiguration Security { get; }
        IRepositoryConfiguration Repository { get; }
    }
}