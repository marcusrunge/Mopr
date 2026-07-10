using MarcusRunge.Mopr.Workbench.Services.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MarcusRunge.Mopr.Workbench.Services.Persistence
{
    internal sealed class PersistenceDbContextFactory : IDesignTimeDbContextFactory<PersistenceDbContext>
    {
        public PersistenceDbContext CreateDbContext(string[] args)
        {
            DbContextOptionsBuilder<PersistenceDbContext> optionsBuilder = new();

            optionsBuilder.UseSqlServer(@"Server=(localdb)\MSSQLLocalDB;Database=MoprDb;Integrated Security=True;TrustServerCertificate=True;");

            return new PersistenceDbContext(optionsBuilder.Options);
        }
    }
}