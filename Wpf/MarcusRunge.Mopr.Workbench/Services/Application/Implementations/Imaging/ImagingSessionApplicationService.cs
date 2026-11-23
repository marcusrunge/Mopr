using MarcusRunge.Base;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Security.Services;
using MarcusRunge.Mopr.Workbench.Contracts.Enums;
using MarcusRunge.Mopr.Workbench.Contracts.Imaging;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Imaging;
using MarcusRunge.Mopr.Workbench.Services.Application.Models.Imaging;
using MarcusRunge.Mopr.Workbench.Services.Core.Contracts.Imaging;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Models.Imaging;

namespace MarcusRunge.Mopr.Workbench.Services.Application.Implementations.Imaging;

/// <summary>
/// Coordinates the authoritative imaging change tracker with atomic persistence operations.
/// </summary>
internal sealed class ImagingSessionApplicationService : CreateableBindableBase<IImagingSessionApplicationService, ImagingSessionApplicationService, IApplicationBase>, IImagingSessionApplicationService
{
    private const string MeasurementEntityType = "Measurement";
    private IApplicationBase? _base;

    private IApplicationBase Base => _base ?? throw new InvalidOperationException("The application service has not been initialized.");
    private IAuditIdentityProvider AuditIdentityProvider => Base.AuditIdentityProvider ?? throw new InvalidOperationException("The audit identity provider has not been initialized.");
    private IImagingSessionChangeTracker ChangeTracker => Base.ImagingService?.ImagingSessionChangeTracker ?? throw new InvalidOperationException("The imaging session change tracker is not available.");
    private IImagingStudyService StudyService => Base.ImagingService?.ImagingStudyService ?? throw new InvalidOperationException("The imaging study service is not available.");
    private IImagingSessionPersistenceService PersistenceService => Base.Persistence?.ImagingSession ?? throw new InvalidOperationException("The imaging session persistence service is not available.");

    /// <inheritdoc/>
    public async Task<IReadOnlyList<LoadedImagingMeasurement>> LoadMeasurementsAsync(string studyInstanceUid, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(studyInstanceUid)) throw new ArgumentException("The study instance UID must not be empty.", nameof(studyInstanceUid));

        cancellationToken.ThrowIfCancellationRequested();
        IReadOnlyList<PersistedImagingMeasurement> measurements = await PersistenceService.LoadMeasurementsAsync(studyInstanceUid.Trim(), cancellationToken).ConfigureAwait(false);
        return [.. measurements.Select(MapLoadedMeasurement)];
    }

    /// <inheritdoc/>
    public async Task<ImagingSessionSaveResult> SaveAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IReadOnlyList<IImagingSessionChange> snapshot = ChangeTracker.UnsavedChanges;
        if (snapshot.Count == 0) return new ImagingSessionSaveResult(0, 0);

        var currentStudy = StudyService.CurrentStudy ?? throw new InvalidOperationException("No imaging study is currently open.");
        string studyInstanceUid = currentStudy.Id;
        ValidateSnapshotStudy(snapshot, studyInstanceUid);
        int auditUserId = await ResolveAuditUserIdAsync(cancellationToken).ConfigureAwait(false);
        var persistenceChanges = snapshot.Select(MapMeasurementChange).ToArray();
        var request = new ImagingSessionPersistenceSaveRequest(studyInstanceUid, auditUserId, persistenceChanges);
        ImagingSessionPersistenceSaveResult persistenceResult = await PersistenceService.SaveAsync(request, cancellationToken).ConfigureAwait(false);
        var persistedEntityIds = persistenceResult.PersistedMeasurementEntityIds.ToHashSet();
        var acknowledgedChangeCount = 0;

        foreach (var change in snapshot)
        {
            if (persistedEntityIds.Contains(change.Identity.EntityId) && ChangeTracker.MarkSaved(change.Identity, change.Version)) acknowledgedChangeCount++;
        }

        return new ImagingSessionSaveResult(snapshot.Count, acknowledgedChangeCount);
    }

    /// <inheritdoc/>
    protected override void OnCreate(IApplicationBase @base) => _base = @base ?? throw new ArgumentNullException(nameof(@base));

    /// <inheritdoc/>
    protected override Task OnCreateAsync(IApplicationBase @base, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }

    private static LoadedImagingMeasurement MapLoadedMeasurement(PersistedImagingMeasurement measurement) => new(measurement.EntityId, measurement.StudyInstanceUid, measurement.SeriesInstanceUid, measurement.SopInstanceUid, measurement.MeasurementType, measurement.Data, measurement.Title, measurement.Description, measurement.CreatedByUserId, measurement.CreatedAtUtc, measurement.ModifiedByUserId, measurement.ModifiedAtUtc);

    private static ImagingMeasurementPersistenceChange MapMeasurementChange(IImagingSessionChange change)
    {
        if (!string.Equals(change.Identity.EntityType, MeasurementEntityType, StringComparison.Ordinal)) throw new NotSupportedException($"Imaging-session entity type '{change.Identity.EntityType}' is not supported by the current persistence integration.");

        ImagingMeasurementChangeData data = change.State == ImagingSessionChangeState.Deleted
            ? GetMeasurementData(change.OriginalValue, change.Identity)
            : GetMeasurementData(change.CurrentValue, change.Identity);
        string seriesInstanceUid = change.Identity.SeriesInstanceUid ?? throw new InvalidOperationException($"Measurement '{change.Identity.EntityId}' has no series instance UID.");
        string sopInstanceUid = change.Identity.SopInstanceUid ?? throw new InvalidOperationException($"Measurement '{change.Identity.EntityId}' has no SOP instance UID.");
        return new ImagingMeasurementPersistenceChange(change.Identity.EntityId, change.State, change.Identity.StudyInstanceUid, seriesInstanceUid, sopInstanceUid, data.MeasurementType, change.State == ImagingSessionChangeState.Deleted ? null : data.Data, data.Title, data.Description);
    }

    private async Task<int> ResolveAuditUserIdAsync(CancellationToken cancellationToken)
    {
        int? auditUserId = await AuditIdentityProvider.GetCurrentUserIdAsync(cancellationToken).ConfigureAwait(false);
        if (auditUserId is not { } validAuditUserId || validAuditUserId <= 0) throw new InvalidOperationException("No valid persistent audit identity is available for the current user.");
        return validAuditUserId;
    }

    private static ImagingMeasurementChangeData GetMeasurementData(object? value, ImagingSessionChangeIdentity identity) => value as ImagingMeasurementChangeData ?? throw new InvalidOperationException($"Measurement '{identity.EntityId}' does not contain an ImagingMeasurementChangeData payload.");

    private static void ValidateSnapshotStudy(IReadOnlyList<IImagingSessionChange> snapshot, string studyInstanceUid)
    {
        foreach (var change in snapshot)
        {
            if (!string.Equals(change.Identity.StudyInstanceUid, studyInstanceUid, StringComparison.Ordinal)) throw new InvalidOperationException($"Change '{change.Identity.EntityType}:{change.Identity.EntityId}' belongs to study '{change.Identity.StudyInstanceUid}' instead of the open study '{studyInstanceUid}'.");
        }
    }
}