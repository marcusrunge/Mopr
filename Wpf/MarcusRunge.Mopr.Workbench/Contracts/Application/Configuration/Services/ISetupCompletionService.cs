using MarcusRunge.Mopr.Workbench.Contracts.Application.Configuration.Models;
using System.Threading;
using System.Threading.Tasks;

namespace MarcusRunge.Mopr.Workbench.Contracts.Application.Configuration.Services
{
    /// <summary>
    /// Coordinates the technical completion of the machine-wide MOPR setup.
    /// </summary>
    public interface ISetupCompletionService
    {
        /// <summary>
        /// Completes the machine-wide MOPR setup after validating and applying all technical prerequisites.
        /// </summary>
        /// <param name="request">The setup-completion request.</param>
        /// <param name="cancellationToken">Cancels the setup-completion operation.</param>
        /// <returns>The structured result of the setup-completion operation.</returns>
        Task<SetupCompletionResult> CompleteAsync(SetupCompletionRequest request, CancellationToken cancellationToken = default);
    }
}