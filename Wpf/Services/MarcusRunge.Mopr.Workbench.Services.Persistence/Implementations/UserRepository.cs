using MarcusRunge.Base;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Contexts;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace MarcusRunge.Mopr.Workbench.Services.Persistence.Implementations
{
    internal class UserRepository : CreateableBindableBase<IUserRepository, UserRepository, IPersistenceBase>, IUserRepository
    {
        // Backing field for the IPersistenceBase instance
        private IPersistenceBase? _base;

        // Property to access the IPersistenceBase instance, throwing an exception if it has not been initialized
        private IPersistenceBase Base => _base ?? throw new InvalidOperationException("Repository has not been initialized.");

        /// <inheritdoc/>
        public async Task AddAsync(User entity, CancellationToken cancellationToken = default)
        {
            // Check if the entity is null and throw an ArgumentNullException if it is
            ArgumentNullException.ThrowIfNull(entity);
            // Create a new instance of the PersistenceDbContext using the Base property
            await using PersistenceDbContext context = Base.CreateDbContext();
            // Add the entity to the Users DbSet in the context
            await context.Users.AddAsync(entity, cancellationToken);
            // Save the changes to the database asynchronously
            await context.SaveChangesAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task DeleteAsync(User entity, CancellationToken cancellationToken = default)
        {
            // Check if the entity is null and throw an ArgumentNullException if it is
            ArgumentNullException.ThrowIfNull(entity);
            // Create a new instance of the PersistenceDbContext using the Base property
            await using PersistenceDbContext context = Base.CreateDbContext();
            // Remove the entity from the Users DbSet in the context
            context.Users.Remove(entity);
            // Save the changes to the database asynchronously
            await context.SaveChangesAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<IList<User>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            // Create a new instance of the PersistenceDbContext using the Base property
            await using PersistenceDbContext context = Base.CreateDbContext();
            // Query the Users DbSet for all users, using AsNoTracking for better performance
            return await context.Users.AsNoTracking().ToListAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            // Check if the id is valid and throw an ArgumentOutOfRangeException if it is not
            if (id <= 0)
                throw new ArgumentOutOfRangeException(nameof(id), "Id must be a positive integer.");
            // Create a new instance of the PersistenceDbContext using the Base property
            await using PersistenceDbContext context = Base.CreateDbContext();
            // Query the Users DbSet for a user with the specified id, using AsNoTracking for better performance
            return await context.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<User?> GetByLoginNameAsync(string loginName, CancellationToken cancellationToken = default)
        {
            // Check if the loginName is null and throw an ArgumentNullException if it is
            ArgumentNullException.ThrowIfNull(loginName);
            // Create a new instance of the PersistenceDbContext using the Base property
            await using PersistenceDbContext context = Base.CreateDbContext();
            // Query the Users DbSet for a user with the specified loginName, using AsNoTracking for better performance
            return await context.Users.AsNoTracking().FirstOrDefaultAsync(x => x.LoginName == loginName, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task UpdateAsync(User entity, CancellationToken cancellationToken = default)
        {
            // Check if the entity is null and throw an ArgumentNullException if it is
            ArgumentNullException.ThrowIfNull(entity);
            // Create a new instance of the PersistenceDbContext using the Base property
            await using PersistenceDbContext context = Base.CreateDbContext();
            // Update the entity in the Users DbSet in the context
            context.Users.Update(entity);
            // Save the changes to the database asynchronously
            await context.SaveChangesAsync(cancellationToken);
        }

        protected override void OnCreate(IPersistenceBase @base) => _base = @base;

        protected override Task OnCreateAsync(IPersistenceBase @base, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}