using MarcusRunge.Base;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Contexts;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Models.Studies;
using Microsoft.EntityFrameworkCore;

namespace MarcusRunge.Mopr.Workbench.Services.Persistence.Implementations
{
    /// <summary>
    /// Reads the persisted DICOM study and series catalog without tracking entities.
    /// </summary>
    internal sealed class StudyCatalogRepository : CreateableBindableBase<IStudyCatalogRepository, StudyCatalogRepository, IPersistenceBase>, IStudyCatalogRepository
    {
        private IPersistenceBase? _base;

        private IPersistenceBase Base => _base ?? throw new InvalidOperationException("Repository has not been initialized.");

        /// <inheritdoc/>
        public async Task<IReadOnlyList<PersistedStudyCatalogItem>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await using PersistenceDbContext context = Base.CreateDbContext();

            /*
             * Project directly into read models so the catalog does not materialize or
             * expose tracked Persistence entities. Availability here means that the
             * persisted repository assignment is usable; physical files are verified
             * only when opening a series or by MIRAS.
             */
            var studies = await context.Studies.AsNoTracking().OrderByDescending(study => study.StudyDate).ThenBy(study => study.PatientName).ThenBy(study => study.Id).Select(study => new PersistedStudyCatalogItem(study.Id, study.StudyInstanceUid!, study.PatientName, study.PatientBirthDate, study.PatientId, study.StudyDate, study.Description, study.AccessionNumber, study.Series.OrderBy(series => series.SeriesNumber).ThenBy(series => series.Description).ThenBy(series => series.Id).Select(series => new PersistedSeriesCatalogItem(series.Id, series.SeriesInstanceUid!, series.SeriesNumber, series.Description, series.Modality, series.Instances.Count, series.Instances.Count(instance => instance.RepositoryLocation != null && instance.RepositoryLocation.IsEnabled && instance.RepositoryLocation.RootPath != null && instance.RepositoryLocation.RootPath != string.Empty && instance.RelativeFilePath != null && instance.RelativeFilePath != string.Empty))).ToList())).ToListAsync(cancellationToken).ConfigureAwait(false);

            return studies;
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