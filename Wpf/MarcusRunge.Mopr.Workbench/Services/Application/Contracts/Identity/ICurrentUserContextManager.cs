using MarcusRunge.Mopr.Workbench.Contracts.Application.Identity.Models;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Identity.Services;

namespace MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Identity
{
    /// <summary>
    /// Provides controlled access to the current-user context within the identity-service graph.
    /// </summary>
    internal interface ICurrentUserContextManager : ICurrentUserContext
    {
        /// <summary>
        /// Clears the current persistent MOPR user.
        /// </summary>
        /// <param name="cancellationToken">Cancels the context update.</param>
        Task ClearAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Sets the current persistent MOPR user.
        /// </summary>
        /// <param name="user">The persistent MOPR user to publish.</param>
        /// <param name="cancellationToken">Cancels the context update.</param>
        Task SetCurrentUserAsync(CurrentUser user, CancellationToken cancellationToken = default);
    }
}