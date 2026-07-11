using MarcusRunge.Base;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Contexts;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace MarcusRunge.Mopr.Workbench.Services.Persistence.Implementations
{
    internal class SeriesRepository : CreateableBindableBase<ISeriesRepository, SeriesRepository, IPersistenceBase>, ISeriesRepository
    {
        // Backing field for the IPersistenceBase instance
        private IPersistenceBase? _base;
        // Property to access the IPersistenceBase instance, throwing an exception if it has not been initialized
        private IPersistenceBase Base => _base ?? throw new InvalidOperationException("Repository has not been initialized.");
        /// <inheritdoc/>
        public async Task AddAsync(Series entity, CancellationToken cancellationToken = default)
        {
            // Check if the entity is null and throw an ArgumentNullException if it is
            ArgumentNullException.ThrowIfNull(entity);
            // Create a new instance of the PersistenceDbContext using the Base property
            await using PersistenceDbContext context = Base.CreateDbContext();
            // Add the entity to the Series DbSet in the context
            await context.Series.AddAsync(entity, cancellationToken);
            // Save the changes to the database asynchronously
            await context.SaveChangesAsync(cancellationToken);
        }
        /// <inheritdoc/>
        public async Task DeleteAsync(Series entity, CancellationToken cancellationToken = default)
        {
            // Check if the entity is null and throw an ArgumentNullException if it is
            ArgumentNullException.ThrowIfNull(entity);
            // Create a new instance of the PersistenceDbContext using the Base property
            await using PersistenceDbContext context = Base.CreateDbContext();
            // Remove the entity from the Series DbSet in the context
            context.Series.Remove(entity);
            // Save the changes to the database asynchronously
            await context.SaveChangesAsync(cancellationToken);
        }
        /// <inheritdoc/>
        public async Task<Series?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            // Check if the id is valid and throw an ArgumentOutOfRangeException if it is not
            if (id <= 0)
                throw new ArgumentOutOfRangeException(nameof(id), "Id must be a positive integer.");
            // Create a new instance of the PersistenceDbContext using the Base property
            await using PersistenceDbContext context = Base.CreateDbContext();
            // Retrieve the Series entity with the specified id from the database asynchronously, without tracking changes
            return await context.Series.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }
        /// <inheritdoc/>
        public async Task<Series?> GetBySeriesInstanceUidAsync(string seriesInstanceUid, CancellationToken cancellationToken = default)
        {
            // Check if the seriesInstanceUid is null and throw an ArgumentNullException if it is
            ArgumentNullException.ThrowIfNull(seriesInstanceUid);
            // Create a new instance of the PersistenceDbContext using the Base property
            await using PersistenceDbContext context = Base.CreateDbContext();
            // Retrieve the Series entity with the specified seriesInstanceUid from the database asynchronously, without tracking changes
            return await context.Series.AsNoTracking().FirstOrDefaultAsync(x => x.SeriesInstanceUid == seriesInstanceUid, cancellationToken);
        }
        /// <inheritdoc/>
        public async Task<IList<Series>> GetByStudyIdAsync(int studyId, CancellationToken cancellationToken = default)
        {
            // Check if the studyId is valid and throw an ArgumentOutOfRangeException if it is not
            if (studyId <= 0)
                throw new ArgumentOutOfRangeException(nameof(studyId), "StudyId must be a positive integer.");
            // Create a new instance of the PersistenceDbContext using the Base property
            await using PersistenceDbContext context = Base.CreateDbContext();
            // Retrieve the list of Series entities with the specified studyId from the database asynchronously, without tracking changes
            return await context.Series.Where(x => x.StudyId == studyId).AsNoTracking().ToListAsync(cancellationToken);
        }
        /// <inheritdoc/>
        public async Task UpdateAsync(Series entity, CancellationToken cancellationToken = default)
        {
            // Check if the entity is null and throw an ArgumentNullException if it is
            ArgumentNullException.ThrowIfNull(entity);
            // Create a new instance of the PersistenceDbContext using the Base property
            await using PersistenceDbContext context = Base.CreateDbContext();
            // Update the entity in the Series DbSet in the context
            context.Series.Update(entity);
            // Save the changes to the database asynchronously
            await context.SaveChangesAsync(cancellationToken);
        }

        protected override void OnCreate(IPersistenceBase @base) => _base = @base;

        protected override Task OnCreateAsync(IPersistenceBase @base, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}