using MarcusRunge.Base;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Entities;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Enums;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Models;

namespace MarcusRunge.Mopr.Workbench.Services.Persistence.Implementations
{
    internal sealed class PersistenceIntegrityService : CreateableBindableBase<IPersistenceIntegrityService, PersistenceIntegrityService, IPersistenceBase>, IPersistenceIntegrityService
    {
        private IPersistenceBase? _base;
        private IPersistenceBase Base => _base ?? throw new InvalidOperationException("Service has not been initialized.");
        private IPersistence Persistence => Base as IPersistence ?? throw new InvalidOperationException("The Persistence base does not implement IPersistence.");
        private IInstanceRepository InstanceRepository => Persistence.Instance ?? throw new InvalidOperationException("The instance repository has not been initialized.");
        private IMeasurementRepository MeasurementRepository => Persistence.Measurement ?? throw new InvalidOperationException("The measurement repository has not been initialized.");
        private ISeriesRepository SeriesRepository => Persistence.Series ?? throw new InvalidOperationException("The series repository has not been initialized.");
        private IStudyRepository StudyRepository => Persistence.Study ?? throw new InvalidOperationException("The study repository has not been initialized.");
        private IUnrealObjectRepository UnrealObjectRepository => Persistence.UnrealObject ?? throw new InvalidOperationException("The Unreal object repository has not been initialized.");
        private IUserRepository UserRepository => Persistence.User ?? throw new InvalidOperationException("The user repository has not been initialized.");

        /// <inheritdoc/>
        public async Task<PersistenceIntegrityResult> VerifyAsync(PersistenceIntegrityRequest request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            cancellationToken.ThrowIfCancellationRequested();

            PersistenceIntegrityResult result = new();

            try
            {
                /*
                 * Every entity set is loaded independently. A hierarchy-only
                 * traversal would hide orphaned children because they cannot be
                 * reached through their missing parents.
                 */
                IReadOnlyList<User> users = [.. await UserRepository.GetAllAsync(cancellationToken)];
                IReadOnlyList<Study> studies = [.. await StudyRepository.GetAllAsync(cancellationToken)];
                IReadOnlyList<Series> seriesItems = [.. await SeriesRepository.GetAllAsync(cancellationToken)];
                IReadOnlyList<Instance> instances = [.. await InstanceRepository.GetAllAsync(cancellationToken)];
                IReadOnlyList<Measurement> measurements = [.. await MeasurementRepository.GetAllAsync(cancellationToken)];
                IReadOnlyList<UnrealObject> unrealObjects = [.. await UnrealObjectRepository.GetAllAsync(cancellationToken)];

                result.ScannedEntities = users.Count + studies.Count + seriesItems.Count + instances.Count + measurements.Count + unrealObjects.Count;

                HashSet<int> userIds = users.Select(item => item.Id).ToHashSet();
                HashSet<int> studyIds = studies.Select(item => item.Id).ToHashSet();
                HashSet<int> seriesIds = seriesItems.Select(item => item.Id).ToHashSet();
                HashSet<int> instanceIds = instances.Select(item => item.Id).ToHashSet();

                if (request.VerifyRequiredValues)
                {
                    RegisterMissingRequiredValues(users, studies, seriesItems, instances, result, cancellationToken);
                }

                if (request.VerifyUniqueValues)
                {
                    RegisterDuplicateValues(users, studies, seriesItems, instances, result, cancellationToken);
                }

                if (request.VerifyRelationships)
                {
                    RegisterMissingParents(seriesItems, instances, measurements, unrealObjects, studyIds, seriesIds, instanceIds, result, cancellationToken);
                }

                if (request.VerifyAuditReferences)
                {
                    RegisterInvalidAuditReferences(studies, seriesItems, instances, measurements, unrealObjects, userIds, result, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                result.Errors.Add($"Persistence integrity verification could not be completed: {exception.Message}");
                Base.OnExceptionThrown(exception);
            }

            return result;
        }

        protected override void OnCreate(IPersistenceBase @base) => _base = @base;

        protected override Task OnCreateAsync(IPersistenceBase @base, CancellationToken cancellationToken) => Task.CompletedTask;

        private static void RegisterDuplicateValues(IReadOnlyList<User> users, IReadOnlyList<Study> studies, IReadOnlyList<Series> seriesItems, IReadOnlyList<Instance> instances, PersistenceIntegrityResult result, CancellationToken cancellationToken)
        {
            RegisterDuplicateValues(users, item => item.Id, item => item.LoginName, PersistenceIntegrityEntityType.User, nameof(User.LoginName), result, cancellationToken);
            RegisterDuplicateValues(studies, item => item.Id, item => item.StudyInstanceUid, PersistenceIntegrityEntityType.Study, nameof(Study.StudyInstanceUid), result, cancellationToken);
            RegisterDuplicateValues(seriesItems, item => item.Id, item => item.SeriesInstanceUid, PersistenceIntegrityEntityType.Series, nameof(Series.SeriesInstanceUid), result, cancellationToken);
            RegisterDuplicateValues(instances, item => item.Id, item => item.SopInstanceUid, PersistenceIntegrityEntityType.Instance, nameof(Instance.SopInstanceUid), result, cancellationToken);
        }

        private static void RegisterDuplicateValues<TEntity>(IReadOnlyList<TEntity> entities, Func<TEntity, int> idSelector, Func<TEntity, string?> valueSelector, PersistenceIntegrityEntityType entityType, string propertyName, PersistenceIntegrityResult result, CancellationToken cancellationToken)
        {
            IEnumerable<IGrouping<string, TEntity>> duplicateGroups = entities
                .Where(item => !string.IsNullOrWhiteSpace(valueSelector(item)))
                .GroupBy(item => valueSelector(item)!, StringComparer.Ordinal)
                .Where(group => group.Count() > 1);

            foreach (IGrouping<string, TEntity> group in duplicateGroups)
            {
                cancellationToken.ThrowIfCancellationRequested();

                foreach (TEntity entity in group)
                {
                    int entityId = idSelector(entity);

                    result.Issues.Add(new PersistenceIntegrityIssue
                    {
                        IssueType = PersistenceIntegrityIssueType.DuplicateUniqueValue,
                        EntityType = entityType,
                        EntityId = entityId,
                        PropertyName = propertyName,
                        Value = group.Key,
                        TechnicalDetails = $"Persisted {entityType} entity '{entityId}' contains duplicate value '{group.Key}' in property '{propertyName}'. The value occurs {group.Count()} times. No persisted entity was changed."
                    });
                }
            }
        }

        private static void RegisterInvalidAuditReference(AuditableEntityBase entity, PersistenceIntegrityEntityType entityType, HashSet<int> userIds, PersistenceIntegrityResult result)
        {
            /*
             * Creation audit data is mandatory for every auditable entity.
             * Modification audit data is optional, but its user must exist
             * whenever a ModifiedByUserId has been persisted.
             */
            if (!userIds.Contains(entity.CreatedByUserId))
            {
                result.Issues.Add(new PersistenceIntegrityIssue
                {
                    IssueType = PersistenceIntegrityIssueType.InvalidAuditReference,
                    EntityType = entityType,
                    EntityId = entity.Id,
                    PropertyName = nameof(AuditableEntityBase.CreatedByUserId),
                    ReferencedEntityType = PersistenceIntegrityEntityType.User,
                    ReferencedEntityId = entity.CreatedByUserId,
                    Value = entity.CreatedByUserId.ToString(),
                    TechnicalDetails = $"Persisted {entityType} entity '{entity.Id}' references missing creation user '{entity.CreatedByUserId}'. No audit relationship was changed."
                });
            }

            if (entity.ModifiedByUserId is int modifiedByUserId && !userIds.Contains(modifiedByUserId))
            {
                result.Issues.Add(new PersistenceIntegrityIssue
                {
                    IssueType = PersistenceIntegrityIssueType.InvalidAuditReference,
                    EntityType = entityType,
                    EntityId = entity.Id,
                    PropertyName = nameof(AuditableEntityBase.ModifiedByUserId),
                    ReferencedEntityType = PersistenceIntegrityEntityType.User,
                    ReferencedEntityId = modifiedByUserId,
                    Value = modifiedByUserId.ToString(),
                    TechnicalDetails = $"Persisted {entityType} entity '{entity.Id}' references missing modification user '{modifiedByUserId}'. No audit relationship was changed."
                });
            }
        }

        private static void RegisterInvalidAuditReferences(IReadOnlyList<Study> studies, IReadOnlyList<Series> seriesItems, IReadOnlyList<Instance> instances, IReadOnlyList<Measurement> measurements, IReadOnlyList<UnrealObject> unrealObjects, HashSet<int> userIds, PersistenceIntegrityResult result, CancellationToken cancellationToken)
        {
            foreach (Study study in studies)
            {
                cancellationToken.ThrowIfCancellationRequested();
                RegisterInvalidAuditReference(study, PersistenceIntegrityEntityType.Study, userIds, result);
            }

            foreach (Series series in seriesItems)
            {
                cancellationToken.ThrowIfCancellationRequested();
                RegisterInvalidAuditReference(series, PersistenceIntegrityEntityType.Series, userIds, result);
            }

            foreach (Instance instance in instances)
            {
                cancellationToken.ThrowIfCancellationRequested();
                RegisterInvalidAuditReference(instance, PersistenceIntegrityEntityType.Instance, userIds, result);
            }

            foreach (Measurement measurement in measurements)
            {
                cancellationToken.ThrowIfCancellationRequested();
                RegisterInvalidAuditReference(measurement, PersistenceIntegrityEntityType.Measurement, userIds, result);
            }

            foreach (UnrealObject unrealObject in unrealObjects)
            {
                cancellationToken.ThrowIfCancellationRequested();
                RegisterInvalidAuditReference(unrealObject, PersistenceIntegrityEntityType.UnrealObject, userIds, result);
            }
        }

        private static void RegisterMissingParent(int entityId, PersistenceIntegrityEntityType entityType, string propertyName, int referencedEntityId, PersistenceIntegrityEntityType referencedEntityType, HashSet<int> validReferencedIds, PersistenceIntegrityResult result)
        {
            if (validReferencedIds.Contains(referencedEntityId))
            {
                return;
            }

            result.Issues.Add(new PersistenceIntegrityIssue
            {
                IssueType = PersistenceIntegrityIssueType.MissingParent,
                EntityType = entityType,
                EntityId = entityId,
                PropertyName = propertyName,
                ReferencedEntityType = referencedEntityType,
                ReferencedEntityId = referencedEntityId,
                Value = referencedEntityId.ToString(),
                TechnicalDetails = $"Persisted {entityType} entity '{entityId}' references missing {referencedEntityType} entity '{referencedEntityId}' through property '{propertyName}'. No persisted relationship was changed."
            });
        }

        private static void RegisterMissingParents(IReadOnlyList<Series> seriesItems, IReadOnlyList<Instance> instances, IReadOnlyList<Measurement> measurements, IReadOnlyList<UnrealObject> unrealObjects, HashSet<int> studyIds, HashSet<int> seriesIds, HashSet<int> instanceIds, PersistenceIntegrityResult result, CancellationToken cancellationToken)
        {
            foreach (Series series in seriesItems)
            {
                cancellationToken.ThrowIfCancellationRequested();
                RegisterMissingParent(series.Id, PersistenceIntegrityEntityType.Series, nameof(Series.StudyId), series.StudyId, PersistenceIntegrityEntityType.Study, studyIds, result);
            }

            foreach (Instance instance in instances)
            {
                cancellationToken.ThrowIfCancellationRequested();
                RegisterMissingParent(instance.Id, PersistenceIntegrityEntityType.Instance, nameof(Instance.SeriesId), instance.SeriesId, PersistenceIntegrityEntityType.Series, seriesIds, result);
            }

            foreach (Measurement measurement in measurements)
            {
                cancellationToken.ThrowIfCancellationRequested();
                RegisterMissingParent(measurement.Id, PersistenceIntegrityEntityType.Measurement, nameof(Measurement.InstanceId), measurement.InstanceId, PersistenceIntegrityEntityType.Instance, instanceIds, result);
            }

            foreach (UnrealObject unrealObject in unrealObjects)
            {
                cancellationToken.ThrowIfCancellationRequested();
                RegisterMissingParent(unrealObject.Id, PersistenceIntegrityEntityType.UnrealObject, nameof(UnrealObject.InstanceId), unrealObject.InstanceId, PersistenceIntegrityEntityType.Instance, instanceIds, result);
            }
        }

        private static void RegisterMissingRequiredValue(int entityId, PersistenceIntegrityEntityType entityType, string propertyName, string? value, PersistenceIntegrityResult result)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            result.Issues.Add(new PersistenceIntegrityIssue
            {
                IssueType = PersistenceIntegrityIssueType.MissingRequiredValue,
                EntityType = entityType,
                EntityId = entityId,
                PropertyName = propertyName,
                TechnicalDetails = $"Persisted {entityType} entity '{entityId}' has no value in required property '{propertyName}'. No persisted value was changed."
            });
        }

        private static void RegisterMissingRequiredValues(IReadOnlyList<User> users, IReadOnlyList<Study> studies, IReadOnlyList<Series> seriesItems, IReadOnlyList<Instance> instances, PersistenceIntegrityResult result, CancellationToken cancellationToken)
        {
            foreach (User user in users)
            {
                cancellationToken.ThrowIfCancellationRequested();
                RegisterMissingRequiredValue(user.Id, PersistenceIntegrityEntityType.User, nameof(User.LoginName), user.LoginName, result);
            }

            foreach (Study study in studies)
            {
                cancellationToken.ThrowIfCancellationRequested();
                RegisterMissingRequiredValue(study.Id, PersistenceIntegrityEntityType.Study, nameof(Study.StudyInstanceUid), study.StudyInstanceUid, result);
            }

            foreach (Series series in seriesItems)
            {
                cancellationToken.ThrowIfCancellationRequested();
                RegisterMissingRequiredValue(series.Id, PersistenceIntegrityEntityType.Series, nameof(Series.SeriesInstanceUid), series.SeriesInstanceUid, result);
            }

            foreach (Instance instance in instances)
            {
                cancellationToken.ThrowIfCancellationRequested();
                RegisterMissingRequiredValue(instance.Id, PersistenceIntegrityEntityType.Instance, nameof(Instance.SopInstanceUid), instance.SopInstanceUid, result);
            }
        }
    }
}