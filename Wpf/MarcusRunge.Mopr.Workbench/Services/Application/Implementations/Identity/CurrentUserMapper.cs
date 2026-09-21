using MarcusRunge.Mopr.Workbench.Contracts.Application.Identity.Models;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Entities;

namespace MarcusRunge.Mopr.Workbench.Services.Application.Implementations.Identity
{
    /// <summary>
    /// Maps a validated persistent user to the immutable current-user contract.
    /// </summary>
    internal static class CurrentUserMapper
    {
        /// <summary>
        /// Maps a persistent MOPR user to the immutable current-user contract.
        /// </summary>
        /// <param name="user">The persistent MOPR user.</param>
        /// <param name="resolvedLoginName">The login name resolved from the current operating-system identity.</param>
        /// <returns>The immutable current-user contract.</returns>
        internal static CurrentUser Map(User user, string resolvedLoginName)
        {
            ArgumentNullException.ThrowIfNull(user);

            if (user.Id <= 0)
            {
                throw new InvalidOperationException("The persistent MOPR user identifier must be positive.");
            }

            return new CurrentUser(user.Id, IdentityValueNormalizer.NormalizeLoginName(resolvedLoginName), IdentityValueNormalizer.NormalizeRequiredPersistentValue(user.FirstName, nameof(user.FirstName)), IdentityValueNormalizer.NormalizeRequiredPersistentValue(user.LastName, nameof(user.LastName)), IdentityValueNormalizer.NormalizeRequiredPersistentValue(user.ShortName, nameof(user.ShortName)), user.IsActive);
        }
    }
}