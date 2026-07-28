using MarcusRunge.Base;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Contexts;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace MarcusRunge.Mopr.Workbench.Services.Persistence.Implementations
{
    internal class RepositoryLocationRepository : CreateableBindableBase<IRepositoryLocationRepository, RepositoryLocationRepository, IPersistenceBase>, IRepositoryLocationRepository
    {
        private IPersistenceBase? _base;
        private IPersistenceBase Base => _base ?? throw new InvalidOperationException("Repository has not been initialized.");

        /// <inheritdoc/>
        public async Task AddAsync(RepositoryLocation entity, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(entity);
            Validate(entity);

            await using PersistenceDbContext context = Base.CreateDbContext();

            /*
             * At most one location may act as the default import target.
             * When a new default is added, any existing default is demoted in
             * the same SaveChanges operation.
             */
            if (entity.IsDefault)
            {
                await ClearExistingDefaultAsync(context, null, cancellationToken);
            }

            await context.RepositoryLocations.AddAsync(entity, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task DeleteAsync(RepositoryLocation entity, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(entity);

            await using PersistenceDbContext context = Base.CreateDbContext();

            bool isReferenced = await context.Instances.AsNoTracking().AnyAsync(item => item.RepositoryLocationId == entity.Id, cancellationToken);

            if (isReferenced)
            {
                throw new InvalidOperationException($"Repository location '{entity.Id}' cannot be deleted because persisted DICOM instances reference it.");
            }

            context.RepositoryLocations.Remove(entity);
            await context.SaveChangesAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<IList<RepositoryLocation>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            await using PersistenceDbContext context = Base.CreateDbContext();
            return await context.RepositoryLocations.AsNoTracking().OrderBy(item => item.Name).ToListAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<RepositoryLocation?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            if (id <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(id), "Id must be a positive integer.");
            }

            await using PersistenceDbContext context = Base.CreateDbContext();
            return await context.RepositoryLocations.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<RepositoryLocation?> GetByRootPathAsync(string rootPath, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);

            string normalizedRootPath = NormalizeRootPath(rootPath);

            await using PersistenceDbContext context = Base.CreateDbContext();
            return await context.RepositoryLocations.AsNoTracking().FirstOrDefaultAsync(item => item.RootPath == normalizedRootPath, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<RepositoryLocation?> GetDefaultAsync(CancellationToken cancellationToken = default)
        {
            await using PersistenceDbContext context = Base.CreateDbContext();
            return await context.RepositoryLocations.AsNoTracking().SingleOrDefaultAsync(item => item.IsDefault, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<IList<RepositoryLocation>> GetEnabledAsync(CancellationToken cancellationToken = default)
        {
            await using PersistenceDbContext context = Base.CreateDbContext();
            return await context.RepositoryLocations.AsNoTracking().Where(item => item.IsEnabled).OrderBy(item => item.Name).ToListAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task UpdateAsync(RepositoryLocation entity, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(entity);
            Validate(entity);

            await using PersistenceDbContext context = Base.CreateDbContext();

            if (entity.IsDefault)
            {
                await ClearExistingDefaultAsync(context, entity.Id, cancellationToken);
            }

            context.RepositoryLocations.Update(entity);
            await context.SaveChangesAsync(cancellationToken);
        }

        protected override void OnCreate(IPersistenceBase @base) => _base = @base;

        protected override Task OnCreateAsync(IPersistenceBase @base, CancellationToken cancellationToken) => Task.CompletedTask;

        private static async Task ClearExistingDefaultAsync(PersistenceDbContext context, int? excludedLocationId, CancellationToken cancellationToken)
        {
            IList<RepositoryLocation> existingDefaults = await context.RepositoryLocations
                .Where(item => item.IsDefault && (!excludedLocationId.HasValue || item.Id != excludedLocationId.Value))
                .ToListAsync(cancellationToken);

            foreach (RepositoryLocation existingDefault in existingDefaults)
            {
                existingDefault.IsDefault = false;
            }
        }

        private static string NormalizeRootPath(string rootPath)
        {
            string fullPath = Path.GetFullPath(rootPath);

            /*
             * Removing the ending separator ensures logically identical roots
             * do not produce different persisted values.
             */
            return Path.TrimEndingDirectorySeparator(fullPath);
        }

        private static void Validate(RepositoryLocation entity)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(entity.Name);
            ArgumentException.ThrowIfNullOrWhiteSpace(entity.RootPath);

            if (!Path.IsPathFullyQualified(entity.RootPath))
            {
                throw new ArgumentException("The repository root path must be an absolute local or UNC path.", nameof(entity));
            }

            entity.Name = entity.Name.Trim();
            entity.RootPath = NormalizeRootPath(entity.RootPath);

            if (entity.IsDefault && !entity.IsEnabled)
            {
                throw new ArgumentException("The default repository location must be enabled.", nameof(entity));
            }
        }
    }
}