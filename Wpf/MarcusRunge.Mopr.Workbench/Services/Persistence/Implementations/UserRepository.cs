using MarcusRunge.Base;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Identity;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace MarcusRunge.Mopr.Workbench.Services.Persistence.Implementations
{
    /// <summary>
    /// Provides persistent access to MOPR users.
    /// </summary>
    internal sealed class UserRepository : CreateableBindableBase<IUserRepository, UserRepository, IPersistenceBase>, IUserRepository
    {
        private IPersistenceBase? _base;

        private IPersistenceBase Base => _base ?? throw new InvalidOperationException("Repository has not been initialized.");

        /// <inheritdoc/>
        public async Task AddAsync(User entity, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(entity);

            await using var context = Base.CreateDbContext();
            await context.Users.AddAsync(entity, cancellationToken).ConfigureAwait(false);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task DeleteAsync(User entity, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(entity);

            await using var context = Base.CreateDbContext();
            context.Users.Remove(entity);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<IList<User>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            await using var context = Base.CreateDbContext();
            return await context.Users.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            if (id <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(id), id, "Id must be a positive integer.");
            }

            await using var context = Base.CreateDbContext();
            return await context.Users.AsNoTracking().FirstOrDefaultAsync(user => user.Id == id, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<User?> GetByLoginNameAsync(string loginName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(loginName))
            {
                throw new ArgumentException("The login name must not be empty.", nameof(loginName));
            }

            var normalizedLoginName = loginName.Trim();

            await using var context = Base.CreateDbContext();
            return await context.Users.AsNoTracking().FirstOrDefaultAsync(user => user.LoginName == normalizedLoginName, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<User?> GetBySecurityIdentifierAsync(string securityIdentifier, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(securityIdentifier))
            {
                throw new ArgumentException("The security identifier must not be empty.", nameof(securityIdentifier));
            }

            var normalizedSecurityIdentifier = securityIdentifier.Trim();

            /*
             * The SID is the durable Windows-account assignment. Existing users
             * may temporarily have no SID during migration, but null values must
             * never participate in assignment resolution.
             */
            await using var context = Base.CreateDbContext();
            return await context.Users.AsNoTracking().FirstOrDefaultAsync(user => user.SecurityIdentifier != null && user.SecurityIdentifier == normalizedSecurityIdentifier, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<bool> HasPersonalUsersAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await using var context = Base.CreateDbContext();

            // The technical system identity audits protected machine-wide changes,
            // but it must never complete the personal-user bootstrap requirement.
            return await context.Users.AsNoTracking().AnyAsync(user => user.LoginName != WellKnownUserLoginNames.System, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task UpdateAsync(User entity, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(entity);

            await using var context = Base.CreateDbContext();
            context.Users.Update(entity);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        protected override void OnCreate(IPersistenceBase @base) => _base = @base ?? throw new ArgumentNullException(nameof(@base));

        /// <inheritdoc/>
        protected override Task OnCreateAsync(IPersistenceBase @base, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }
    }
}