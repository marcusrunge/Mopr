using MarcusRunge.Mopr.Workbench.Contracts.Application.Identity.Models;

namespace MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Identity
{
    /// <summary>
    /// Resolves the current operating-system identity to a persistent MOPR user.
    /// </summary>
    public interface IUserSignInService
    {
        /// <summary>
        /// Resolves and signs in the persistent MOPR user assigned to the current operating-system identity.
        /// </summary>
        /// <param name="cancellationToken">Cancels the sign-in operation.</param>
        /// <returns>The structured sign-in result.</returns>
        Task<UserSignInResult> SignInAsync(CancellationToken cancellationToken = default);
    }
}