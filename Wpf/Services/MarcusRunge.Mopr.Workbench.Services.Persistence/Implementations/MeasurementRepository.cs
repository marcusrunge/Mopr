using MarcusRunge.Base;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Contexts;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace MarcusRunge.Mopr.Workbench.Services.Persistence.Implementations
{
    internal class MeasurementRepository : CreateableBindableBase<IMeasurementRepository, MeasurementRepository, IPersistenceBase>, IMeasurementRepository
    {
        // Backing field for the IPersistenceBase instance
        private IPersistenceBase? _base;

        // Property to access the IPersistenceBase instance, throwing an exception if it has not been initialized
        private IPersistenceBase Base => _base ?? throw new InvalidOperationException("Repository has not been initialized.");

        /// <inheritdoc/>
        public async Task AddAsync(Measurement entity, CancellationToken cancellationToken = default)
        {
            // Check if the entity is null and throw an ArgumentNullException if it is
            ArgumentNullException.ThrowIfNull(entity);
            // Create a new instance of the PersistenceDbContext using the Base property
            await using PersistenceDbContext context = Base.CreateDbContext();
            // Add the entity to the Measurements DbSet in the context
            await context.Measurements.AddAsync(entity, cancellationToken);
            // Save the changes to the database asynchronously
            await context.SaveChangesAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task DeleteAsync(Measurement entity, CancellationToken cancellationToken = default)
        {
            // Check if the entity is null and throw an ArgumentNullException if it is
            ArgumentNullException.ThrowIfNull(entity);
            // Create a new instance of the PersistenceDbContext using the Base property
            await using PersistenceDbContext context = Base.CreateDbContext();
            // Remove the entity from the Measurements DbSet in the context
            context.Measurements.Remove(entity);
            // Save the changes to the database asynchronously
            await context.SaveChangesAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<Measurement?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            // Check if the id is valid and throw an ArgumentOutOfRangeException if it is not
            if (id <= 0)
                throw new ArgumentOutOfRangeException(nameof(id), "Id must be a positive integer.");
            // Create a new instance of the PersistenceDbContext using the Base property
            await using PersistenceDbContext context = Base.CreateDbContext();
            // Use AsNoTracking for better performance when the entity is not being updated
            return await context.Measurements.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<IList<Measurement>> GetByInstanceIdAsync(int instanceId, CancellationToken cancellationToken = default)
        {
            // Check if the instanceId is valid and throw an ArgumentOutOfRangeException if it is not
            if (instanceId <= 0)
                throw new ArgumentOutOfRangeException(nameof(instanceId), "Instance ID must be a positive integer.");
            // Create a new instance of the PersistenceDbContext using the Base property
            await using PersistenceDbContext context = Base.CreateDbContext();
            // Use AsNoTracking for better performance when the entities are not being updated
            return await context.Measurements.Where(x => x.InstanceId == instanceId).AsNoTracking().ToListAsync(cancellationToken);
        }

        public async Task UpdateAsync(Measurement entity, CancellationToken cancellationToken = default)
        {
            // Check if the entity is null and throw an ArgumentNullException if it is
            ArgumentNullException.ThrowIfNull(entity);
            // Create a new instance of the PersistenceDbContext using the Base property
            await using PersistenceDbContext context = Base.CreateDbContext();
            // Update the entity in the Measurements DbSet in the context
            context.Measurements.Update(entity);
            // Save the changes to the database asynchronously
            await context.SaveChangesAsync(cancellationToken);
        }

        protected override void OnCreate(IPersistenceBase @base) => _base = @base;

        protected override Task OnCreateAsync(IPersistenceBase @base, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}