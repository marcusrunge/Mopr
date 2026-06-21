using MarcusRunge.Base;
using MarcusRunge.Mopr.Workbench.Contracts.Models;
using MarcusRunge.Mopr.Workbench.Services.Core.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Core.Contracts.Imaging;

namespace MarcusRunge.Mopr.Workbench.Services.Core.Implementations.Imaging
{
    internal sealed class ImagingSelectionService : CreateableBindableBase<IImagingSelectionService, ImagingSelectionService, IImagingServiceBase>, IImagingSelectionService
    {
        private StudyInfo? _selectedStudy;
        private SeriesInfo? _selectedSeries;

        public StudyInfo? SelectedStudy => _selectedStudy;

        public SeriesInfo? SelectedSeries => _selectedSeries;

        public event EventHandler<SeriesSelectionChangedEventArgs>? SelectedSeriesChanged;

        public void SelectStudy(StudyInfo? study)
        {
            if (_selectedStudy?.Id == study?.Id)
            {
                return;
            }

            _selectedStudy = study;

            if (_selectedSeries != null && _selectedSeries.StudyId != null && _selectedSeries.StudyId != _selectedStudy?.Id)
            {
                _selectedSeries = null;
            }

            RaiseSelectedSeriesChanged();
        }

        public void SelectSeries(SeriesInfo? series)
        {
            if (_selectedSeries?.Id == series?.Id)
            {
                return;
            }

            _selectedSeries = series;

            RaiseSelectedSeriesChanged();
        }

        public void ClearSelection()
        {
            if (_selectedStudy is null && _selectedSeries is null)
            {
                return;
            }

            _selectedStudy = null;
            _selectedSeries = null;

            RaiseSelectedSeriesChanged();
        }

        private void RaiseSelectedSeriesChanged() => SelectedSeriesChanged?.Invoke(this, new SeriesSelectionChangedEventArgs(_selectedStudy, _selectedSeries));

        protected override void OnCreate(IImagingServiceBase @base)
        {
        }

        protected override Task OnCreateAsync(IImagingServiceBase @base, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}