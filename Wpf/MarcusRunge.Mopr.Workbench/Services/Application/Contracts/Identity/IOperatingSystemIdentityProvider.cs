using MarcusRunge.Mopr.Workbench.Services.Application.Models;

namespace MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Identity
{
    /// <summary>
    /// Provides the authenticated identity of the current operating-system user.
    /// </summary>
    public interface IOperatingSystemIdentityProvider
    {
        /// <summary>
        /// Gets the authenticated identity of the current operating-system user.
        /// </summary>
        /// <param name="cancellationToken">Cancels the identity resolution.</param>
        /// <returns>
        /// The authenticated operating-system identity, or <see langword="null"/>
        /// when no usable identity is available.
        /// </returns>
        Task<OperatingSystemIdentity?> GetCurrentIdentityAsync(CancellationToken cancellationToken = default);
    }
}