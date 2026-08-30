using MarcusRunge.Mopr.Workbench.Contracts.Miras.Enums;
using MarcusRunge.Mopr.Workbench.Contracts.Miras.Models;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace MarcusRunge.Mopr.Workbench.Services.Core.Contracts.Miras
{
    /// <summary>
    /// Controls application-level MIRAS checks independently of the user interface.
    /// </summary>
    public interface IMirasFlowService : INotifyPropertyChanged
    {
        /// <summary>
        /// Gets a value indicating whether cancellation can currently be requested.
        /// </summary>
        bool CanCancel { get; }

        /// <summary>
        /// Gets a value indicating whether a new MIRAS check can be started.
        /// </summary>
        bool CanStart { get; }

        /// <summary>
        /// Gets the current application-level execution state.
        /// </summary>
        MirasFlowState CurrentState { get; }

        /// <summary>
        /// Gets a value indicating whether the most recent run ended because an
        /// unexpected exception escaped from the MIRAS service.
        /// </summary>
        bool HasUnexpectedError { get; }

        /// <summary>
        /// Gets a value indicating whether a MIRAS check is currently running.
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// Gets the result returned by the most recently completed MIRAS check.
        /// </summary>
        /// <remarks>
        /// This value is cleared when a new run starts. Cancellation and unexpected
        /// exceptions do not create synthetic MIRAS results.
        /// </remarks>
        MirasOperationResult? LastResult { get; }

        /// <summary>
        /// Starts a MIRAS check or joins the check that is already running.
        /// </summary>
        /// <param name="cancellationToken">
        /// Cancels the new run when this invocation starts it. An invocation that
        /// joins an existing run observes the existing shared task.
        /// </param>
        /// <returns>
        /// The shared task representing the active MIRAS check.
        /// </returns>
        Task<MirasOperationResult> StartAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Requests cancellation of the active MIRAS check.
        /// </summary>
        /// <remarks>
        /// Calling this method while no check is running has no effect.
        /// </remarks>
        void Cancel();
    }
}