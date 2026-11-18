using System.Threading;
using System.Threading.Tasks;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.Services
{
    /// <summary>
    /// Loads persisted measurements into the in-memory store of the currently open study.
    /// </summary>
    public interface IImagingMeasurementHydrationService
    {
        /// <summary>
        /// Clears the current in-memory measurement state and hydrates persisted measurements for one study.
        /// </summary>
        Task HydrateAsync(string studyInstanceUid, CancellationToken cancellationToken = default);
    }
}
