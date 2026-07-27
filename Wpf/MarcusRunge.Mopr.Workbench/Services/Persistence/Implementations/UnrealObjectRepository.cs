using MarcusRunge.Base;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Contexts;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace MarcusRunge.Mopr.Workbench.Services.Persistence.Implementations
{
    internal class UnrealObjectRepository : CreateableBindableBase<IUnrealObjectRepository, UnrealObjectRepository, IPersistenceBase>, IUnrealObjectRepository
    {
        private IPersistenceBase? _base;
        private IPersistenceBase Base => _base ?? throw new InvalidOperationException("Repository has not been initialized.");

        /// <inheritdoc/>
        public async Task AddAsync(UnrealObject entity, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(entity);

            await using PersistenceDbContext context = Base.CreateDbContext();
            await context.UnrealObjects.AddAsync(entity, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task DeleteAsync(UnrealObject entity, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(entity);

            await using PersistenceDbContext context = Base.CreateDbContext();
            context.UnrealObjects.Remove(entity);
            await context.SaveChangesAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<IList<UnrealObject>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            await using PersistenceDbContext context = Base.CreateDbContext();
            return await context.UnrealObjects.AsNoTracking().ToListAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<UnrealObject?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            if (id <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(id), "Id must be a positive integer.");
            }

            await using PersistenceDbContext context = Base.CreateDbContext();
            return await context.UnrealObjects.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<IList<UnrealObject>> GetByInstanceIdAsync(int instanceId, CancellationToken cancellationToken = default)
        {
            if (instanceId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(instanceId), "Instance ID must be a positive integer.");
            }

            await using PersistenceDbContext context = Base.CreateDbContext();
            return await context.UnrealObjects.AsNoTracking().Where(item => item.InstanceId == instanceId).ToListAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task UpdateAsync(UnrealObject entity, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(entity);

            await using PersistenceDbContext context = Base.CreateDbContext();
            context.UnrealObjects.Update(entity);
            await context.SaveChangesAsync(cancellationToken);
        }

        protected override void OnCreate(IPersistenceBase @base) => _base = @base;

        protected override Task OnCreateAsync(IPersistenceBase @base, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}