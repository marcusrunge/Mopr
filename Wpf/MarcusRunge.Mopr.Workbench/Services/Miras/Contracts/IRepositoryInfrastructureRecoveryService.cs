using MarcusRunge.Mopr.Workbench.Services.Miras.Models;

namespace MarcusRunge.Mopr.Workbench.Services.Miras.Contracts
{
    /// <summary>
    /// Restores missing machine-wide repository infrastructure through an
    /// explicit and administratively authorized action.
    /// </summary>
    public interface IRepositoryInfrastructureRecoveryService
    {
        /// <summary>
        /// Restores a missing default repository location and its technical
        /// audit identity.
        /// </summary>
        /// <param name="request">The administrator-confirmed recovery request.</param>
        /// <param name="cancellationToken">Cancels the recovery operation.</param>
        /// <returns>The structured recovery result.</returns>
        Task<RepositoryInfrastructureRecoveryResult> RecoverAsync(RepositoryInfrastructureRecoveryRequest request, CancellationToken cancellationToken = default);
    }
}