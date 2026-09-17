using MarcusRunge.Mopr.Workbench.Contracts.Application.Identity.Models;

namespace MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Identity
{
    /// <summary>
    /// Creates a persistent MOPR user for the current operating-system identity.
    /// </summary>
    public interface IUserProvisioningService
    {
        /// <summary>
        /// Creates and signs in the persistent MOPR user assigned to the current operating-system identity.
        /// </summary>
        /// <param name="request">The user-editable provisioning values.</param>
        /// <param name="cancellationToken">Cancels the provisioning operation.</param>
        /// <returns>The structured provisioning result.</returns>
        Task<UserProvisioningResult> ProvisionAsync(UserProvisioningRequest request, CancellationToken cancellationToken = default);
    }
}