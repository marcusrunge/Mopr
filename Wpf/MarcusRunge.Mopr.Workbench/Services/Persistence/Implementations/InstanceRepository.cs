using MarcusRunge.Base;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Contexts;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace MarcusRunge.Mopr.Workbench.Services.Persistence.Implementations
{
    internal class InstanceRepository : CreateableBindableBase<IInstanceRepository, InstanceRepository, IPersistenceBase>, IInstanceRepository
    {
        // Backing field for the IPersistenceBase instance
        private IPersistenceBase? _base;

        // Property to access the IPersistenceBase instance, throwing an exception if it has not been initialized
        private IPersistenceBase Base => _base ?? throw new InvalidOperationException("Repository has not been initialized.");

        public async Task AddAsync(Instance entity, CancellationToken cancellationToken = default)
        {
            // Check if the entity parameter is null and throw an exception if it is
            ArgumentNullException.ThrowIfNull(entity);
            // Create a new instance of the PersistenceDbContext using the Base property
            await using PersistenceDbContext context = Base.CreateDbContext();
            // Use Entity Framework Core to add the entity to the Instances DbSet and save changes to the database
            await context.Instances.AddAsync(entity, cancellationToken);
            // Save changes to the database asynchronously
            await context.SaveChangesAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task DeleteAsync(Instance entity, CancellationToken cancellationToken = default)
        {
            // Check if the entity parameter is null and throw an exception if it is
            ArgumentNullException.ThrowIfNull(entity);
            // Create a new instance of the PersistenceDbContext using the Base property
            await using PersistenceDbContext context = Base.CreateDbContext();
            // Use Entity Framework Core to remove the entity from the Instances DbSet and save changes to the database
            context.Instances.Remove(entity);
            // Save changes to the database asynchronously
            await context.SaveChangesAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<IList<Instance>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            await using PersistenceDbContext context = Base.CreateDbContext();
            return await context.Instances.AsNoTracking().ToListAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<Instance?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            // Check if the id is valid and throw an ArgumentOutOfRangeException if it is not
            if (id <= 0)
                throw new ArgumentOutOfRangeException(nameof(id), "Id must be a positive integer.");
            // Create a new instance of the PersistenceDbContext using the Base property
            await using PersistenceDbContext context = Base.CreateDbContext();
            // Use Entity Framework Core to query the Instances DbSet, including related Measurements and UnrealObjects, and return the first instance that matches the specified id
            return await context.Instances.AsNoTracking().Include(x => x.Measurements).Include(x => x.UnrealObjects).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<IList<Instance>> GetBySeriesIdAsync(int seriesId, CancellationToken cancellationToken = default)
        {
            // Check if the seriesId is valid and throw an ArgumentOutOfRangeException if it is not
            if (seriesId <= 0)
                throw new ArgumentOutOfRangeException(nameof(seriesId), "SeriesId must be a positive integer.");
            // Create a new instance of the PersistenceDbContext using the Base property
            await using PersistenceDbContext context = Base.CreateDbContext();
            // Use Entity Framework Core to query the Instances DbSet and return a list of instances that match the specified seriesId, without tracking changes
            return await context.Instances.AsNoTracking().Where(x => x.SeriesId == seriesId).ToListAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<Instance?> GetBySopInstanceUidAsync(string sopInstanceUid, CancellationToken cancellationToken = default)
        {
            // Check if the sopInstanceUid parameter is null and throw an exception if it is
            ArgumentNullException.ThrowIfNull(sopInstanceUid);
            // Create a new instance of the PersistenceDbContext using the Base property
            await using PersistenceDbContext context = Base.CreateDbContext();
            // Use Entity Framework Core to query the Instances DbSet, including related Measurements and UnrealObjects, and return the first instance that matches the specified sopInstanceUid
            return await context.Instances.AsNoTracking().Include(x => x.Measurements).Include(x => x.UnrealObjects).FirstOrDefaultAsync(x => x.SopInstanceUid == sopInstanceUid, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task UpdateAsync(Instance entity, CancellationToken cancellationToken = default)
        {
            // Check if the entity parameter is null and throw an exception if it is
            ArgumentNullException.ThrowIfNull(entity);
            // Create a new instance of the PersistenceDbContext using the Base property
            await using PersistenceDbContext context = Base.CreateDbContext();
            // Use Entity Framework Core to update the entity in the Instances DbSet and save changes to the database
            context.Instances.Update(entity);
            // Save changes to the database asynchronously
            await context.SaveChangesAsync(cancellationToken);
        }

        protected override void OnCreate(IPersistenceBase @base) => _base = @base;

        protected override Task OnCreateAsync(IPersistenceBase @base, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}