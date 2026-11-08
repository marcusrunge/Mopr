using MarcusRunge.Base;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Studies;
using MarcusRunge.Mopr.Workbench.Services.Application.Models.Studies;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Models.Studies;

namespace MarcusRunge.Mopr.Workbench.Services.Application.Implementations.Studies
{
    /// <summary>
    /// Provides the read-only application catalog of imported DICOM studies and series.
    /// </summary>
    internal sealed class StudyCatalogService : CreateableBindableBase<IStudyCatalogService, StudyCatalogService, IStudiesServiceBase>, IStudyCatalogService
    {
        private IStudiesServiceBase? _base;

        private IStudiesServiceBase Base => _base ?? throw new InvalidOperationException("Service has not been initialized.");

        private IPersistence Persistence => Base.ApplicationBase?.Persistence ?? throw new InvalidOperationException("Persistence has not been initialized.");

        /// <inheritdoc/>
        public async Task<IReadOnlyList<StudyCatalogItem>> GetStudiesAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var repository = Persistence.StudyCatalog ?? throw new InvalidOperationException("The study catalog repository has not been initialized.");
            var persistedStudies = await repository.GetAllAsync(cancellationToken).ConfigureAwait(false);

            /*
             * Application models form the stable boundary for UI modules. Persistence
             * read models must not escape this service even though both projections
             * currently contain closely related values.
             */
            return [.. persistedStudies.Select(MapStudy)];
        }

        /// <inheritdoc/>
        protected override void OnCreate(IStudiesServiceBase @base) => _base = @base ?? throw new ArgumentNullException(nameof(@base));

        /// <inheritdoc/>
        protected override Task OnCreateAsync(IStudiesServiceBase @base, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(@base);
            cancellationToken.ThrowIfCancellationRequested();

            _base = @base;
            return Task.CompletedTask;
        }

        private static SeriesCatalogItem MapSeries(PersistedSeriesCatalogItem series)
        {
            ArgumentNullException.ThrowIfNull(series);

            return new SeriesCatalogItem(SeriesId: series.SeriesId, SeriesInstanceUid: series.SeriesInstanceUid, SeriesNumber: series.SeriesNumber, Description: series.Description, Modality: series.Modality, ImageCount: series.ImageCount, AvailableImageCount: series.AvailableImageCount);
        }

        private static StudyCatalogItem MapStudy(PersistedStudyCatalogItem study)
        {
            ArgumentNullException.ThrowIfNull(study);

            var series = study.Series.Select(MapSeries).ToArray();

            return new StudyCatalogItem(StudyId: study.StudyId, StudyInstanceUid: study.StudyInstanceUid, PatientName: study.PatientName, PatientBirthDate: study.PatientBirthDate, PatientId: study.PatientId, StudyDate: study.StudyDate, Description: study.Description, AccessionNumber: study.AccessionNumber, Series: series);
        }
    }
}