using MarcusRunge.Base;
using MarcusRunge.Mopr.Workbench.Contracts.Enums;
using MarcusRunge.Mopr.Workbench.Contracts.Models.Measurements;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Contexts;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Entities;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Models.Imaging;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Serialization;
using Microsoft.EntityFrameworkCore;

namespace MarcusRunge.Mopr.Workbench.Services.Persistence.Implementations
{
    /// <summary>
    /// Provides atomic persistence for imaging-session business data.
    /// </summary>
    internal sealed class ImagingSessionPersistenceService : CreateableBindableBase<IImagingSessionPersistenceService, ImagingSessionPersistenceService, IPersistenceBase>, IImagingSessionPersistenceService
    {
        private IPersistenceBase? _base;

        private IPersistenceBase Base => _base ?? throw new InvalidOperationException("The persistence service has not been initialized.");

        /// <inheritdoc/>
        public async Task<IReadOnlyList<PersistedImagingMeasurement>> LoadMeasurementsAsync(string studyInstanceUid, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(studyInstanceUid)) throw new ArgumentException("The study instance UID must not be empty.", nameof(studyInstanceUid));

            cancellationToken.ThrowIfCancellationRequested();
            await using PersistenceDbContext context = Base.CreateDbContext();
            var entities = await context.Measurements.AsNoTracking()
                .Where(measurement => measurement.Instance != null && measurement.Instance.Series != null && measurement.Instance.Series.Study != null && measurement.Instance.Series.Study.StudyInstanceUid == studyInstanceUid)
                .Select(measurement => new
                {
                    Measurement = measurement,
                    StudyInstanceUid = measurement.Instance!.Series!.Study!.StudyInstanceUid!,
                    SeriesInstanceUid = measurement.Instance.Series.SeriesInstanceUid!,
                    SopInstanceUid = measurement.Instance.SopInstanceUid!
                })
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var measurements = new List<PersistedImagingMeasurement>(entities.Count);

            foreach (var entity in entities)
            {
                cancellationToken.ThrowIfCancellationRequested();
                MeasurementData data = DeserializeRequired(entity.Measurement);
                measurements.Add(new PersistedImagingMeasurement(entity.Measurement.EntityId, entity.StudyInstanceUid, entity.SeriesInstanceUid, entity.SopInstanceUid, entity.Measurement.MeasurementType, data, entity.Measurement.Title, entity.Measurement.Description, entity.Measurement.CreatedByUserId, entity.Measurement.CreatedAtUtc, entity.Measurement.ModifiedByUserId, entity.Measurement.ModifiedAtUtc));
            }

            return measurements;
        }

        /// <inheritdoc/>
        public async Task<ImagingSessionPersistenceSaveResult> SaveAsync(ImagingSessionPersistenceSaveRequest request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            cancellationToken.ThrowIfCancellationRequested();
            ValidateRequest(request);
            await using PersistenceDbContext context = Base.CreateDbContext();
            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
            var persistedEntityIds = new List<Guid>(request.MeasurementChanges.Count);

            try
            {
                await ValidateAuditUserAsync(context, request.AuditUserId, cancellationToken).ConfigureAwait(false);

                foreach (var change in request.MeasurementChanges)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await ApplyMeasurementChangeAsync(context, request, change, cancellationToken).ConfigureAwait(false);
                    persistedEntityIds.Add(change.EntityId);
                }

                await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                return new ImagingSessionPersistenceSaveResult(persistedEntityIds);
            }
            catch
            {
                await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
                throw;
            }
        }

        /// <inheritdoc/>
        protected override void OnCreate(IPersistenceBase @base) => _base = @base ?? throw new ArgumentNullException(nameof(@base));

        /// <inheritdoc/>
        protected override Task OnCreateAsync(IPersistenceBase @base, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }

        private static async Task ApplyMeasurementChangeAsync(PersistenceDbContext context, ImagingSessionPersistenceSaveRequest request, ImagingMeasurementPersistenceChange change, CancellationToken cancellationToken)
        {
            if (!string.Equals(change.StudyInstanceUid, request.StudyInstanceUid, StringComparison.Ordinal)) throw new InvalidOperationException($"Measurement '{change.EntityId}' belongs to study '{change.StudyInstanceUid}' instead of the requested study '{request.StudyInstanceUid}'.");

            switch (change.State)
            {
                case ImagingSessionChangeState.Created:
                    await CreateMeasurementAsync(context, request.AuditUserId, change, cancellationToken).ConfigureAwait(false);
                    return;
                case ImagingSessionChangeState.Modified:
                    await UpdateMeasurementAsync(context, request.AuditUserId, change, cancellationToken).ConfigureAwait(false);
                    return;
                case ImagingSessionChangeState.Deleted:
                    await DeleteMeasurementAsync(context, change, cancellationToken).ConfigureAwait(false);
                    return;
                default:
                    throw new ArgumentOutOfRangeException(nameof(change), change.State, "The measurement change state is not supported.");
            }
        }

        private static async Task CreateMeasurementAsync(PersistenceDbContext context, int auditUserId, ImagingMeasurementPersistenceChange change, CancellationToken cancellationToken)
        {
            if (await context.Measurements.AnyAsync(entity => entity.EntityId == change.EntityId, cancellationToken).ConfigureAwait(false)) throw new InvalidOperationException($"Measurement '{change.EntityId}' already exists.");
            Instance instance = await ResolveInstanceAsync(context, change, cancellationToken).ConfigureAwait(false);
            var entity = new Measurement
            {
                EntityId = change.EntityId,
                InstanceId = instance.Id,
                MeasurementType = change.MeasurementType,
                DataJson = MeasurementDataSerializer.Serialize(change.Data!),
                Title = change.Title,
                Description = change.Description,
                CreatedByUserId = auditUserId
            };
            await context.Measurements.AddAsync(entity, cancellationToken).ConfigureAwait(false);
        }

        private static async Task DeleteMeasurementAsync(PersistenceDbContext context, ImagingMeasurementPersistenceChange change, CancellationToken cancellationToken)
        {
            Measurement entity = await FindTrackedMeasurementAsync(context, change, cancellationToken).ConfigureAwait(false);
            context.Measurements.Remove(entity);
        }

        private static MeasurementData DeserializeRequired(Measurement measurement)
        {
            if (string.IsNullOrWhiteSpace(measurement.DataJson)) throw new InvalidDataException($"Measurement '{measurement.EntityId}' contains no serialized data.");
            return MeasurementDataSerializer.Deserialize(measurement.MeasurementType, measurement.DataJson) ?? throw new InvalidDataException($"Measurement '{measurement.EntityId}' contains unsupported or invalid serialized data.");
        }

        private static async Task<Measurement> FindTrackedMeasurementAsync(PersistenceDbContext context, ImagingMeasurementPersistenceChange change, CancellationToken cancellationToken)
        {
            Measurement? entity = await context.Measurements
                .Include(measurement => measurement.Instance)!
                .ThenInclude(instance => instance!.Series)!
                .ThenInclude(series => series!.Study)
                .SingleOrDefaultAsync(measurement => measurement.EntityId == change.EntityId, cancellationToken)
                .ConfigureAwait(false) ?? throw new InvalidOperationException($"Measurement '{change.EntityId}' does not exist.");
            ValidatePersistedLocation(entity, change);
            return entity;
        }

        private static async Task<Instance> ResolveInstanceAsync(PersistenceDbContext context, ImagingMeasurementPersistenceChange change, CancellationToken cancellationToken)
        {
            Instance? instance = await context.Instances
                .Include(entity => entity.Series)!
                .ThenInclude(series => series!.Study)
                .SingleOrDefaultAsync(entity => entity.SopInstanceUid == change.SopInstanceUid, cancellationToken)
                .ConfigureAwait(false);

            if (instance?.Series?.Study is null || !string.Equals(instance.Series.SeriesInstanceUid, change.SeriesInstanceUid, StringComparison.Ordinal) || !string.Equals(instance.Series.Study.StudyInstanceUid, change.StudyInstanceUid, StringComparison.Ordinal)) throw new InvalidOperationException($"The DICOM instance '{change.SopInstanceUid}' does not belong to study '{change.StudyInstanceUid}' and series '{change.SeriesInstanceUid}'.");
            return instance;
        }

        private static async Task UpdateMeasurementAsync(PersistenceDbContext context, int auditUserId, ImagingMeasurementPersistenceChange change, CancellationToken cancellationToken)
        {
            Measurement entity = await FindTrackedMeasurementAsync(context, change, cancellationToken).ConfigureAwait(false);
            entity.MeasurementType = change.MeasurementType;
            entity.DataJson = MeasurementDataSerializer.Serialize(change.Data!);
            entity.Title = change.Title;
            entity.Description = change.Description;
            entity.ModifiedByUserId = auditUserId;
        }

        private static async Task ValidateAuditUserAsync(PersistenceDbContext context, int auditUserId, CancellationToken cancellationToken)
        {
            bool isValidAuditUser = await context.Users.AsNoTracking().AnyAsync(user => user.Id == auditUserId && user.IsActive, cancellationToken).ConfigureAwait(false);
            if (!isValidAuditUser) throw new InvalidOperationException($"Audit user '{auditUserId}' does not exist or is inactive.");
        }

        private static void ValidatePersistedLocation(Measurement entity, ImagingMeasurementPersistenceChange change)
        {
            if (entity.Instance?.Series?.Study is null || !string.Equals(entity.Instance.SopInstanceUid, change.SopInstanceUid, StringComparison.Ordinal) || !string.Equals(entity.Instance.Series.SeriesInstanceUid, change.SeriesInstanceUid, StringComparison.Ordinal) || !string.Equals(entity.Instance.Series.Study.StudyInstanceUid, change.StudyInstanceUid, StringComparison.Ordinal)) throw new InvalidOperationException($"Measurement '{change.EntityId}' is not assigned to the requested DICOM study, series and instance.");
        }

        private static void ValidateRequest(ImagingSessionPersistenceSaveRequest request)
        {
            var entityIds = new HashSet<Guid>();
            foreach (var change in request.MeasurementChanges)
            {
                if (change is null) throw new ArgumentException("The measurement change collection must not contain null entries.", nameof(request));
                if (!entityIds.Add(change.EntityId)) throw new ArgumentException($"Measurement '{change.EntityId}' occurs more than once in the save request.", nameof(request));
            }
        }
    }
}
