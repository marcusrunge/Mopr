using MarcusRunge.Mopr.Workbench.Contracts.Models;
using MarcusRunge.Mopr.Workbench.Core.Mvvm;
using MarcusRunge.Mopr.Workbench.Services.Interfaces.Imaging;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.ViewModels
{
    public sealed class SeriesPanelViewModel : ViewModelBase
    {
        private readonly IImagingSelectionService _selectionService;
        private readonly IImagingStudyService _studyService;

        private string _searchText = string.Empty;
        private SeriesInfo? _selectedSeries;

        public SeriesPanelViewModel(IImagingSelectionService selectionService, IImagingStudyService studyService)
        {
            _selectionService = selectionService;
            _studyService = studyService;

            _studyService.StudyLoaded += OnStudyLoaded;

            Series = [];

            ApplyStudy(_studyService.CurrentStudy, _studyService.CurrentSeries);
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
                    _selectionService.SelectSeries(value);
                }
            }
        }

        public ObservableCollection<SeriesInfo> Series { get; }

        public override void Destroy()
        {
            _studyService.StudyLoaded -= OnStudyLoaded;

            base.Destroy();
        }

        private void ApplyStudy(StudyInfo? study, IReadOnlyList<SeriesInfo> series)
        {
            _selectionService.SelectStudy(study);

            Series.Clear();

            foreach (var item in series)
            {
                Series.Add(item);
            }

            SelectedSeries = Series.Count > 0 ? Series[0] : null;
        }

        private void OnStudyLoaded(object? sender, ImagingStudyLoadedEventArgs e) => ApplyStudy(e.Study, e.Series);
    }
}