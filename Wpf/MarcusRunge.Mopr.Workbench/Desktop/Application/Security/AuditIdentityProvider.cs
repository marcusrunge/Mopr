using MarcusRunge.Mopr.Workbench.Contracts.Application.Security;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MarcusRunge.Mopr.Workbench.Application.Security
{
    /// <summary>
    /// Resolves the current Windows user against the persistent MOPR user repository.
    /// </summary>
    internal sealed class AuditIdentityProvider(IPersistence persistence, ICurrentLoginNameProvider loginNameProvider) : IAuditIdentityProvider
    {
        private readonly ICurrentLoginNameProvider _loginNameProvider = loginNameProvider ?? throw new ArgumentNullException(nameof(loginNameProvider));
        private readonly IPersistence _persistence = persistence ?? throw new ArgumentNullException(nameof(persistence));

        /// <inheritdoc/>
        public async Task<int?> GetCurrentUserIdAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var loginName = _loginNameProvider.GetCurrentLoginName();
            if (string.IsNullOrWhiteSpace(loginName))
            {
                return null;
            }

            var userRepository = _persistence.User;
            if (userRepository is null)
            {
                return null;
            }

            // Import operations must be attributed only to an already persisted
            // application user. This boundary never creates users implicitly.
            var user = await userRepository.GetByLoginNameAsync(loginName, cancellationToken).ConfigureAwait(false);
            return user is { Id: > 0 } ? user.Id : null;
        }
    }
}