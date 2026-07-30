using MarcusRunge.Base;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Contexts;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Entities;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;

namespace MarcusRunge.Mopr.Workbench.Services.Persistence.Implementations
{
    internal sealed class DicomImportPersistenceService : CreateableBindableBase<IDicomImportPersistenceService, DicomImportPersistenceService, IPersistenceBase>, IDicomImportPersistenceService
    {
        private IPersistenceBase? _base;

        private IPersistenceBase Base => _base ?? throw new InvalidOperationException("Service has not been initialized.");

        /// <inheritdoc/>
        public async Task PersistAsync(DicomImportPersistenceRequest request, CancellationToken cancellationToken = default)
        {
            ValidateRequest(request);
            cancellationToken.ThrowIfCancellationRequested();

            await using PersistenceDbContext context = Base.CreateDbContext();
            await using IDbContextTransaction? transaction = await BeginTransactionAsync(context, cancellationToken);

            try
            {
                /*
                 * Every entity is queried and tracked by this one DbContext. Existing
                 * relationships and newly created entities therefore participate in
                 * the same unit of work and cannot be persisted independently.
                 */
                User user = await context.Users.SingleOrDefaultAsync(item => item.Id == request.CreatedByUserId, cancellationToken)
                    ?? throw new InvalidOperationException($"The user with ID '{request.CreatedByUserId}' does not exist.");

                RepositoryLocation repositoryLocation = await context.RepositoryLocations.SingleOrDefaultAsync(item => item.Id == request.RepositoryLocationId, cancellationToken)
                    ?? throw new InvalidOperationException($"Repository location with ID '{request.RepositoryLocationId}' does not exist.");

                if (!repositoryLocation.IsEnabled)
                {
                    throw new InvalidOperationException($"Repository location '{repositoryLocation.Id}' is disabled and cannot be used as an import target.");
                }

                Study? study = await context.Studies.SingleOrDefaultAsync(item => item.StudyInstanceUid == request.StudyInstanceUid, cancellationToken);

                if (study is null)
                {
                    study = new Study
                    {
                        StudyInstanceUid = request.StudyInstanceUid,
                        CreatedByUser = user
                    };

                    await context.Studies.AddAsync(study, cancellationToken);
                }

                Series? series = await context.Series.SingleOrDefaultAsync(item => item.SeriesInstanceUid == request.SeriesInstanceUid, cancellationToken);

                if (series is null)
                {
                    /*
                     * The tracked navigation establishes the relationship without an
                     * intermediate SaveChangesAsync merely to obtain the Study ID.
                     */
                    series = new Series
                    {
                        SeriesInstanceUid = request.SeriesInstanceUid,
                        Study = study,
                        CreatedByUser = user
                    };

                    await context.Series.AddAsync(series, cancellationToken);
                }
                else if (study.Id <= 0 || series.StudyId != study.Id)
                {
                    /*
                     * An existing Series cannot belong to a newly created Study and
                     * cannot be reassigned to another existing Study during import.
                     */
                    throw new InvalidOperationException($"Series '{request.SeriesInstanceUid}' belongs to a different study.");
                }

                Instance? instance = await context.Instances.SingleOrDefaultAsync(item => item.SopInstanceUid == request.SopInstanceUid, cancellationToken);

                if (instance is null)
                {
                    /*
                     * Both navigations are tracked by the current DbContext. EF Core
                     * resolves generated keys and foreign keys within the single
                     * SaveChangesAsync operation below.
                     */
                    instance = new Instance
                    {
                        SopInstanceUid = request.SopInstanceUid,
                        RelativeFilePath = request.RelativeFilePath,
                        RepositoryLocation = repositoryLocation,
                        Series = series,
                        CreatedByUser = user
                    };

                    await context.Instances.AddAsync(instance, cancellationToken);
                }
                else
                {
                    ValidateExistingInstance(instance, series, repositoryLocation, request);

                    if (!string.Equals(instance.RelativeFilePath, request.RelativeFilePath, StringComparison.Ordinal))
                    {
                        instance.RelativeFilePath = request.RelativeFilePath;
                        instance.ModifiedByUser = user;
                    }
                }

                /*
                 * This is intentionally the only SaveChangesAsync call. Study, Series
                 * and Instance must either be accepted together or not be persisted.
                 */
                await context.SaveChangesAsync(cancellationToken);

                if (transaction is not null)
                {
                    await transaction.CommitAsync(cancellationToken);
                }
            }
            catch (Exception exception)
            {
                if (transaction is not null)
                {
                    /*
                     * Rollback ignores an already signalled caller cancellation because
                     * restoring database consistency has priority once the unit of work
                     * has started. If rollback also fails, both failures are preserved.
                     */
                    try
                    {
                        await transaction.RollbackAsync(CancellationToken.None);
                    }
                    catch (Exception rollbackException)
                    {
                        AggregateException aggregateException = new(
                            "The atomic DICOM Persistence operation failed and its database transaction could not be rolled back.",
                            exception,
                            rollbackException);

                        Base.OnExceptionThrown(aggregateException);
                        throw aggregateException;
                    }
                }

                /*
                 * OperationCanceledException and all regular Persistence failures retain
                 * their original type and stack trace when rollback succeeds or when the
                 * active provider does not use relational transactions.
                 */
                throw;
            }
        }

        protected override void OnCreate(IPersistenceBase @base)
        {
            ArgumentNullException.ThrowIfNull(@base);
            _base = @base;
        }

        protected override Task OnCreateAsync(IPersistenceBase @base, CancellationToken cancellationToken) => Task.CompletedTask;

        private static async Task<IDbContextTransaction?> BeginTransactionAsync(PersistenceDbContext context, CancellationToken cancellationToken) =>
            /*
            * The EF Core In-Memory provider does not implement relational
            * transactions. Relational providers use Serializable isolation so UID
            * resolution and creation form one protected database operation.
            */
            context.Database.IsRelational() ? await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken) : null;

        private static void ValidateExistingInstance(Instance instance, Series series, RepositoryLocation repositoryLocation, DicomImportPersistenceRequest request)
        {
            if (instance.SeriesId != series.Id)
            {
                throw new InvalidOperationException($"Instance '{request.SopInstanceUid}' belongs to a different series.");
            }

            /*
             * An existing SOP instance has exactly one authoritative physical
             * location. Import must not silently move or duplicate that assignment.
             */
            if (instance.RepositoryLocationId != repositoryLocation.Id)
            {
                throw new InvalidOperationException($"Instance '{request.SopInstanceUid}' belongs to repository location '{instance.RepositoryLocationId}' and cannot be imported into repository location '{repositoryLocation.Id}'.");
            }
        }
        private static void ValidatePathSegment(string value, string parameterName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);

            /*
             * Medical UIDs are persisted as individual repository path segments.
             * Rooted values, separators and traversal segments must be rejected even
             * when a caller provides a matching RelativeFilePath.
             */
            if (Path.IsPathFullyQualified(value) || value is "." or ".." || value.IndexOfAny([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar]) >= 0 || value.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                throw new ArgumentException($"Value '{value}' is not a valid DICOM repository path segment.", parameterName);
            }
        }
        private static void ValidateRequest(DicomImportPersistenceRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (request.CreatedByUserId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(request), "The ID of the user executing the import must be a positive integer.");
            }

            if (request.RepositoryLocationId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(request), "The repository-location ID must be a positive integer.");
            }

            ValidatePathSegment(request.StudyInstanceUid, nameof(request.StudyInstanceUid));
            ValidatePathSegment(request.SeriesInstanceUid, nameof(request.SeriesInstanceUid));
            ValidatePathSegment(request.SopInstanceUid, nameof(request.SopInstanceUid));
            ArgumentException.ThrowIfNullOrWhiteSpace(request.RelativeFilePath);

            string expectedRelativeFilePath = Path.Combine(request.StudyInstanceUid, request.SeriesInstanceUid, $"{request.SopInstanceUid}.dcm");

            /*
             * Persistence accepts only the canonical Study-Series-SOP path produced by
             * the Repository layer. This rejects rooted paths, traversal, redundant
             * segments and alternative physical locations before any value is saved.
             */
            if (!string.Equals(request.RelativeFilePath, expectedRelativeFilePath, StringComparison.Ordinal))
            {
                throw new ArgumentException($"The DICOM repository file path '{request.RelativeFilePath}' does not match the canonical relative path '{expectedRelativeFilePath}'.", nameof(request));
            }
        }
    }
}