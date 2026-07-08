using MarcusRunge.Mopr.Workbench.Services.Persistence.Contexts;

namespace MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts
{
    internal interface IPersistenceDbContextFactory
    {
        internal PersistenceDbContext CreateDbContext();
    }
}