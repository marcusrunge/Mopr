using MarcusRunge.Mopr.Workbench.Services.Persistence.Models;

namespace MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts
{
    /// <summary>
    /// Defines the contract for verifying the structural and referential
    /// integrity of Persistence.
    /// </summary>
    public interface IPersistenceIntegrityService
    {
        /// <summary>
        /// Verifies persisted required values, uniqueness constraints,
        /// relationships and audit references.
        /// </summary>
        /// <param name="request">The verification request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The structured Persistence integrity result.</returns>
        Task<PersistenceIntegrityResult> VerifyAsync(PersistenceIntegrityRequest request, CancellationToken cancellationToken = default);
    }
}