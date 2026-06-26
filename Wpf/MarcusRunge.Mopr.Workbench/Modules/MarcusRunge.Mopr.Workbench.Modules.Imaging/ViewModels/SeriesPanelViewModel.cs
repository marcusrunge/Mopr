using MarcusRunge.Mopr.Workbench.Contracts.Models;
using MarcusRunge.Mopr.Workbench.Core.Mvvm;
using MarcusRunge.Mopr.Workbench.Services.Core.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Core.Contracts.Imaging;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.ViewModels
{
    public sealed class SeriesPanelViewModel : ViewModelBase
    {
        private readonly ICore _core;

        private bool _isApplyingExternalSelection;
        private string _searchText = string.Empty;
        private SeriesInfo? _selectedSeries;

        public SeriesPanelViewModel(ICore core)
        {
            _core = core;

            _core.ImagingService!.ImagingStudyService!.StudyLoaded += OnStudyLoaded;
            _core.ImagingService!.ImagingSelectionService!.SelectedSeriesChanged += OnSelectedSeriesChanged;

            Series = [];

            ApplyStudy(_core.ImagingService!.ImagingStudyService!.CurrentStudy, _core.ImagingService!.ImagingStudyService!.CurrentSeries);
        }

        public string SearchText
        {
            get => _searchText;
            set => SetProperty(ref _searchText, value);
        }

        public SeriesInfo? SelectedSeries
        {
            get => _selectedSeries;
            set
            {
                if (SetProperty(ref _selectedSeries, value))
                {
                    if (!_isApplyingExternalSelection)
                    {
                        _core.ImagingService!.ImagingSelectionService!.SelectSeries(value);
                    }
                }
            }
        }

        public ObservableCollection<SeriesInfo> Series { get; }

        public override void Destroy()
        {
            _core.ImagingService!.ImagingStudyService!.StudyLoaded -= OnStudyLoaded;
            _core.ImagingService!.ImagingSelectionService!.SelectedSeriesChanged -= OnSelectedSeriesChanged;
            base.Destroy();
        }

        private void ApplyStudy(StudyInfo? study, IReadOnlyList<SeriesInfo> series)
        {
            _core.ImagingService!.ImagingSelectionService!.SelectStudy(study);

            Series.Clear();

            foreach (var item in series)
            {
                System.Diagnostics.Debug.WriteLine($"Series: Id={item.Id}, Name={item.Name}, SeriesNumber={item.SeriesNumber}, Count={item.ImageCount}");

                Series.Add(item);
            }

            SelectedSeries = Series.Count > 0 ? Series[0] : null;
        }

        private void OnSelectedSeriesChanged(object? sender, SeriesSelectionChangedEventArgs e)
        {
            try
            {
                _isApplyingExternalSelection = true;

                SelectedSeries = e.SelectedSeries;
            }
            finally
            {
                _isApplyingExternalSelection = false;
            }
        }

        private void OnStudyLoaded(object? sender, ImagingStudyLoadedEventArgs e) => ApplyStudy(e.Study, e.Series);
    }
}