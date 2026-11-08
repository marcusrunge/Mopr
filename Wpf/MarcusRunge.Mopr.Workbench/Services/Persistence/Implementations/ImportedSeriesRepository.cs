using MarcusRunge.Base;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Contexts;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Models.Studies;
using Microsoft.EntityFrameworkCore;

namespace MarcusRunge.Mopr.Workbench.Services.Persistence.Implementations
{
    /// <summary>
    /// Reads the persisted series and repository assignments required for safe application resolution.
    /// </summary>
    internal sealed class ImportedSeriesRepository : CreateableBindableBase<IImportedSeriesRepository, ImportedSeriesRepository, IPersistenceBase>, IImportedSeriesRepository
    {
        private IPersistenceBase? _base;
        private IPersistenceBase Base => _base ?? throw new InvalidOperationException("Repository has not been initialized.");

        /// <inheritdoc/>
        public async Task<PersistedImportedSeries?> GetAsync(int seriesId, string seriesInstanceUid, CancellationToken cancellationToken = default)
        {
            if (seriesId <= 0) throw new ArgumentOutOfRangeException(nameof(seriesId), "The series ID must be a positive integer.");
            ArgumentException.ThrowIfNullOrWhiteSpace(seriesInstanceUid);
            cancellationToken.ThrowIfCancellationRequested();
            await using PersistenceDbContext context = Base.CreateDbContext();

            /*
             * Both identifiers must match the same persisted series. Instance rows are
             * ordered deterministically so downstream imaging never depends on database
             * provider row order.
             */
            return await context.Series.AsNoTracking().Where(series => series.Id == seriesId && series.SeriesInstanceUid == seriesInstanceUid).Select(series => new PersistedImportedSeries(series.Id, series.SeriesInstanceUid!, series.StudyId, series.Study!.StudyInstanceUid!, series.Instances.OrderBy(instance => instance.InstanceNumber).ThenBy(instance => instance.Id).Select(instance => new PersistedImportedSeriesInstance(instance.Id, instance.SopInstanceUid!, instance.InstanceNumber, instance.RepositoryLocationId, instance.RepositoryLocation != null && instance.RepositoryLocation.IsEnabled, instance.RepositoryLocation == null ? null : instance.RepositoryLocation.RootPath, instance.RelativeFilePath)).ToList())).SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        protected override void OnCreate(IPersistenceBase @base) => _base = @base ?? throw new ArgumentNullException(nameof(@base));

        /// <inheritdoc/>
        protected override Task OnCreateAsync(IPersistenceBase @base, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(@base);
            cancellationToken.ThrowIfCancellationRequested();
            _base = @base;
            return Task.CompletedTask;
        }
    }
}