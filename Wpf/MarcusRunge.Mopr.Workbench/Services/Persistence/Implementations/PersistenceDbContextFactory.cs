using MarcusRunge.Mopr.Workbench.Services.Persistence.Contexts;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts;
using Microsoft.EntityFrameworkCore;

internal sealed class PersistenceDbContextFactory(IDbContextFactory<PersistenceDbContext> factory) : IPersistenceDbContextFactory
{
    PersistenceDbContext IPersistenceDbContextFactory.CreateDbContext() => factory.CreateDbContext();
}