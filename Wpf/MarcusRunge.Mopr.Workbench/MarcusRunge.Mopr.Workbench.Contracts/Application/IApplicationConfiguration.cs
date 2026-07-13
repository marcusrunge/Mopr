namespace MarcusRunge.Mopr.Workbench.Contracts.Application
{
    public interface IApplicationConfiguration
    {      
        ISecurityConfiguration Security { get; }
        IRepositoryConfiguration Repository { get; }
    }
}