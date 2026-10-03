using MarcusRunge.Mopr.Workbench.Contracts.Application.Identity.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MarcusRunge.Mopr.Workbench.Contracts.Application.Identity.Services
{
    /// <summary>
    /// Provides the persistent MOPR user assigned to the current application session.
    /// </summary>
    public interface ICurrentUserContext
    {
        /// <summary>
        /// Occurs after the current persistent MOPR user has changed.
        /// </summary>
        /// <remarks>
        /// The event supplies the active persistent user or <see langword="null"/>
        /// after the application session has been cleared. Subscribers must not
        /// assume that the event is raised on a specific synchronization context.
        /// </remarks>
        event Action<CurrentUser?> CurrentUserChanged;

        /// <summary>
        /// Gets the current persistent MOPR user.
        /// </summary>
        /// <param name="cancellationToken">Cancels the context access.</param>
        /// <returns>
        /// The current persistent MOPR user, or <see langword="null"/>
        /// when no user has been assigned.
        /// </returns>
        Task<CurrentUser?> GetCurrentUserAsync(CancellationToken cancellationToken = default);
    }
}