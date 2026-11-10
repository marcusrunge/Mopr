using MarcusRunge.Mopr.Workbench.Contracts.Models;
using MarcusRunge.Mopr.Workbench.Core.Mvvm;
using MarcusRunge.Mopr.Workbench.Services.Core.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Core.Contracts.Imaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.ViewModels
{
    public sealed class SeriesPanelViewModel : ViewModelBase
    {
        private readonly List<SeriesInfo> _allSeries = [];
        private readonly ICore _core;
        private bool _isApplyingExternalSelection;
        private string _searchText = string.Empty;
        private SeriesInfo? _selectedSeries;

        public SeriesPanelViewModel(ICore core)
        {
            _core = core ?? throw new ArgumentNullException(nameof(core));

            _core.ImagingService!.ImagingStudyService!.StudyLoaded += OnStudyLoaded;
            _core.ImagingService.ImagingSelectionService!.SelectedSeriesChanged += OnSelectedSeriesChanged;

            Series = [];

            ApplyStudy(_core.ImagingService.ImagingStudyService.CurrentStudy, _core.ImagingService.ImagingStudyService.CurrentSeries);
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (!SetProperty(ref _searchText, value ?? string.Empty)) return;
                ApplyFilter();
            }
        }

        public SeriesInfo? SelectedSeries
        {
            get => _selectedSeries;
            set
            {
                if (!SetProperty(ref _selectedSeries, value)) return;
                if (!_isApplyingExternalSelection) _core.ImagingService!.ImagingSelectionService!.SelectSeries(value);
            }
        }

        public ObservableCollection<SeriesInfo> Series { get; }

        public override void Destroy()
        {
            _core.ImagingService!.ImagingStudyService!.StudyLoaded -= OnStudyLoaded;
            _core.ImagingService.ImagingSelectionService!.SelectedSeriesChanged -= OnSelectedSeriesChanged;
            base.Destroy();
        }

        private void ApplyFilter()
        {
            var selectedSeriesId = SelectedSeries?.Id;
            string[] searchTerms = SearchText.Split([' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            IEnumerable<SeriesInfo> filteredSeries = _allSeries;
            if (searchTerms.Length > 0) filteredSeries = filteredSeries.Where(series => searchTerms.All(term => Matches(series, term)));

            Series.Clear();
            foreach (var series in filteredSeries) Series.Add(series);

            /*
             * Preserve the current selection while the selected series remains visible.
             * Otherwise select the first matching series so the global imaging selection
             * cannot point to an item hidden by the active filter.
             */
            SeriesInfo? visibleSelection = selectedSeriesId is null ? null : Series.FirstOrDefault(series => string.Equals(series.Id, selectedSeriesId, StringComparison.OrdinalIgnoreCase));
            SelectedSeries = visibleSelection ?? Series.FirstOrDefault();
        }

        private void ApplyStudy(StudyInfo? study, IReadOnlyList<SeriesInfo> series)
        {
            _core.ImagingService!.ImagingSelectionService!.SelectStudy(study);

            _allSeries.Clear();
            _allSeries.AddRange(series);
            ApplyFilter();
        }

        private static bool Contains(string? value, string term) => !string.IsNullOrWhiteSpace(value) && value.Contains(term, StringComparison.CurrentCultureIgnoreCase);

        private static bool Matches(SeriesInfo series, string term) => Contains(series.Name, term) || Contains(series.Description, term) || Contains(series.Modality, term) || Contains(series.SeriesNumber?.ToString(), term) || Contains(series.Id, term);

        private void OnSelectedSeriesChanged(object? sender, SeriesSelectionChangedEventArgs e)
        {
            try
            {
                _isApplyingExternalSelection = true;
                SelectedSeries = e.SelectedSeries is null ? null : Series.FirstOrDefault(series => string.Equals(series.Id, e.SelectedSeries.Id, StringComparison.OrdinalIgnoreCase));
            }
            finally
            {
                _isApplyingExternalSelection = false;
            }
        }

        private void OnStudyLoaded(object? sender, ImagingStudyLoadedEventArgs e) => ApplyStudy(e.Study, e.Series);
    }
}