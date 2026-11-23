using MarcusRunge.Mopr.Workbench.Services.Persistence.Models.Imaging;

namespace MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts
{
    /// <summary>
    /// Persists and loads imaging-session business data without exposing Entity Framework to application services.
    /// </summary>
    public interface IImagingSessionPersistenceService
    {
        /// <summary>
        /// Loads every valid persisted measurement belonging to one DICOM study.
        /// </summary>
        Task<IReadOnlyList<PersistedImagingMeasurement>> LoadMeasurementsAsync(string studyInstanceUid, CancellationToken cancellationToken = default);

        /// <summary>
        /// Persists all supplied imaging-session changes in one database transaction.
        /// </summary>
        Task<ImagingSessionPersistenceSaveResult> SaveAsync(ImagingSessionPersistenceSaveRequest request, CancellationToken cancellationToken = default);
    }
}