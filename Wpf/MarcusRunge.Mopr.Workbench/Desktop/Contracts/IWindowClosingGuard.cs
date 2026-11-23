using System.Threading;
using System.Threading.Tasks;

namespace MarcusRunge.Mopr.Workbench.Contracts
{
    /// <summary>
    /// Guards an application-window close request before WPF begins shutdown.
    /// </summary>
    public interface IWindowClosingGuard
    {
        /// <summary>
        /// Resolves pending work and returns whether the window may close.
        /// </summary>
        Task<bool> CanCloseWindowAsync(CancellationToken cancellationToken = default);
    }
}