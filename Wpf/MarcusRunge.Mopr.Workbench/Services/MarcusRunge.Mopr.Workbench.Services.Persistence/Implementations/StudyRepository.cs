using MarcusRunge.Base;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Contexts;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace MarcusRunge.Mopr.Workbench.Services.Persistence.Implementations
{
    internal class StudyRepository : CreateableBindableBase<IStudyRepository, StudyRepository, IPersistenceBase>, IStudyRepository
    {
        // Backing field for the IPersistenceBase instance
        private IPersistenceBase? _base;

        // Property to access the IPersistenceBase instance, throwing an exception if it has not been initialized
        private IPersistenceBase Base => _base ?? throw new InvalidOperationException("Repository has not been initialized.");

        /// <inheritdoc/>
        public async Task AddAsync(Study entity, CancellationToken cancellationToken = default)
        {
            // Check if the entity is null and throw an exception if it is
            ArgumentNullException.ThrowIfNull(entity);
            // Create a new instance of the PersistenceDbContext using the _base field
            await using PersistenceDbContext context = Base.CreateDbContext();
            // Add the entity to the Studies DbSet in the context
            await context.Studies.AddAsync(entity, cancellationToken);
            // Save the changes to the database
            await context.SaveChangesAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task DeleteAsync(Study entity, CancellationToken cancellationToken = default)
        {
            // Check if the entity is null and throw an exception if it is
            ArgumentNullException.ThrowIfNull(entity);
            // Create a new instance of the PersistenceDbContext using the _base field
            await using PersistenceDbContext context = Base.CreateDbContext();
            // Remove the entity from the Studies DbSet in the context
            context.Studies.Remove(entity);
            // Save the changes to the database
            await context.SaveChangesAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<IList<Study>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            // Create a new instance of the PersistenceDbContext using the _base field
            await using PersistenceDbContext context = Base.CreateDbContext();
            // Retrieve all Study entities from the Studies DbSet in the context without tracking changes
            return await context.Studies.AsNoTracking().ToListAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<Study?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            // Check if the id is valid and throw an ArgumentOutOfRangeException if it is not
            if (id <= 0)
                throw new ArgumentOutOfRangeException(nameof(id), "Id must be a positive integer.");
            // Create a new instance of the PersistenceDbContext using the _base field
            await using PersistenceDbContext context = Base.CreateDbContext();
            // Retrieve a Study entity by its ID from the Studies DbSet in the context, including its Series
            return await context.Studies.AsNoTracking().Include(x => x.Series).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<Study?> GetByStudyInstanceUidAsync(string studyInstanceUid, CancellationToken cancellationToken = default)
        {
            // Check if the studyInstanceUid is null and throw an exception if it is
            ArgumentNullException.ThrowIfNull(studyInstanceUid);
            // Create a new instance of the PersistenceDbContext using the _base field
            await using PersistenceDbContext context = Base.CreateDbContext();
            // Retrieve a Study entity by its StudyInstanceUid from the Studies DbSet in the context, including its Series
            return await context.Studies.AsNoTracking().Include(x => x.Series).FirstOrDefaultAsync(x => x.StudyInstanceUid == studyInstanceUid, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task UpdateAsync(Study entity, CancellationToken cancellationToken = default)
        {
            // Check if the entity is null and throw an exception if it is
            ArgumentNullException.ThrowIfNull(entity);
            // Create a new instance of the PersistenceDbContext using the _base field
            await using PersistenceDbContext context = Base.CreateDbContext();
            // Update the entity in the Studies DbSet in the context
            context.Studies.Update(entity);
            // Save the changes to the database
            await context.SaveChangesAsync(cancellationToken);
        }

        protected override void OnCreate(IPersistenceBase @base) => _base = @base;

        protected override Task OnCreateAsync(IPersistenceBase @base, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}