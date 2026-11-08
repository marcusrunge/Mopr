using MarcusRunge.Base;
using MarcusRunge.Mopr.Workbench.Services.Application.Models.Studies;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace MarcusRunge.Mopr.Workbench.Modules.Studies.ViewModels
{
    /// <summary>
    /// Exposes one imported DICOM study and the multi-selection state of its series.
    /// </summary>
    public sealed class StudyItemViewModel : BindableBase
    {
        public StudyItemViewModel(StudyCatalogItem model)
        {
            ArgumentNullException.ThrowIfNull(model);
            Model = model;
            Series = new ObservableCollection<SeriesItemViewModel>(model.Series.Select(series => new SeriesItemViewModel(series)));

            foreach (var series in Series)
            {
                series.PropertyChanged += OnSeriesPropertyChanged;
                series.IsSelected = series.IsAvailable;
            }
        }

        public string AccessionNumber => string.IsNullOrWhiteSpace(Model.AccessionNumber) ? Properties.Resources.NotAvailable : Model.AccessionNumber;
        public int AvailableSeriesCount => Series.Count(series => series.IsAvailable);
        public bool HasSelectedSeries => SelectedSeriesCount > 0;
        public int ImageCount => Model.ImageCount;
        public StudyCatalogItem Model { get; }
        public string PatientBirthDateText => Model.PatientBirthDate?.ToString("d") ?? Properties.Resources.NotAvailable;
        public string PatientId => string.IsNullOrWhiteSpace(Model.PatientId) ? Properties.Resources.NotAvailable : Model.PatientId;
        public string PatientName => string.IsNullOrWhiteSpace(Model.PatientName) ? Properties.Resources.UnknownPatient : Model.PatientName;
        public ObservableCollection<SeriesItemViewModel> Series { get; }
        public int SeriesCount => Model.SeriesCount;
        public int SelectedSeriesCount => Series.Count(series => series.IsSelected);
        public string SelectionSummary => string.Format(Properties.Resources.SeriesSelectionSummaryFormat, SelectedSeriesCount, AvailableSeriesCount);
        public string StudyDateText => Model.StudyDate?.ToString("g") ?? Properties.Resources.NotAvailable;
        public string StudyDescription => string.IsNullOrWhiteSpace(Model.Description) ? Properties.Resources.UnknownStudyDescription : Model.Description;
        public int StudyId => Model.StudyId;
        public string StudyInstanceUid => Model.StudyInstanceUid;

        public void ClearSelection()
        {
            foreach (var series in Series) series.IsSelected = false;
        }

        public void SelectAllAvailable()
        {
            foreach (var series in Series) series.IsSelected = series.IsAvailable;
        }

        private void OnSeriesPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(SeriesItemViewModel.IsSelected)) return;
            RaisePropertyChanged(nameof(HasSelectedSeries));
            RaisePropertyChanged(nameof(SelectedSeriesCount));
            RaisePropertyChanged(nameof(SelectionSummary));
        }
    }
}