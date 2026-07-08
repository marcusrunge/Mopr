using MarcusRunge.Mopr.Workbench.Services.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace MarcusRunge.Mopr.Workbench.Services.Persistence.Contexts
{
    internal sealed class PersistenceDbContext(DbContextOptions<PersistenceDbContext> options) : DbContext(options)
    {
        public DbSet<Instance> Instances => Set<Instance>();
        public DbSet<Measurement> Measurements => Set<Measurement>();
        public DbSet<Series> Series => Set<Series>();
        public DbSet<Study> Studies => Set<Study>();
        public DbSet<User> Users => Set<User>();

        protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.ApplyConfigurationsFromAssembly(typeof(PersistenceDbContext).Assembly);
    }
}