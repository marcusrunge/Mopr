using MarcusRunge.Mopr.Workbench.Services.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace MarcusRunge.Mopr.Workbench.Services.Persistence.Contexts
{
    internal sealed class PersistenceDbContext(DbContextOptions<PersistenceDbContext> options) : DbContext(options)
    {
        internal DbSet<Instance> Instances => Set<Instance>();
        internal DbSet<Measurement> Measurements => Set<Measurement>();
        internal DbSet<Series> Series => Set<Series>();
        internal DbSet<Study> Studies => Set<Study>();
        internal DbSet<UnrealObject> UnrealObjects => Set<UnrealObject>();
        internal DbSet<User> Users => Set<User>();

        protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.ApplyConfigurationsFromAssembly(typeof(PersistenceDbContext).Assembly);
    }
}