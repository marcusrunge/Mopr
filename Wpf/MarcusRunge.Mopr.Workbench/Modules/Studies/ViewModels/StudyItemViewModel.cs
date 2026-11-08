using MarcusRunge.Base;
using MarcusRunge.Mopr.Workbench.Services.Application.Models.Studies;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace MarcusRunge.Mopr.Workbench.Modules.Studies.ViewModels
{
    /// <summary>
    /// Exposes one imported DICOM study and its selectable series.
    /// </summary>
    public sealed class StudyItemViewModel : BindableBase
    {
        private SeriesItemViewModel? _selectedSeries;

        public StudyItemViewModel(StudyCatalogItem model)
        {
            ArgumentNullException.ThrowIfNull(model);
            Model = model;
            Series = new ObservableCollection<SeriesItemViewModel>(model.Series.Select(series => new SeriesItemViewModel(series)));
            SelectedSeries = Series.FirstOrDefault(series => series.IsAvailable);
        }

        public string AccessionNumber => string.IsNullOrWhiteSpace(Model.AccessionNumber) ? Properties.Resources.NotAvailable : Model.AccessionNumber;
        public int ImageCount => Model.ImageCount;
        public StudyCatalogItem Model { get; }
        public string PatientBirthDateText => Model.PatientBirthDate?.ToString("d") ?? Properties.Resources.NotAvailable;
        public string PatientId => string.IsNullOrWhiteSpace(Model.PatientId) ? Properties.Resources.NotAvailable : Model.PatientId;
        public string PatientName => string.IsNullOrWhiteSpace(Model.PatientName) ? Properties.Resources.UnknownPatient : Model.PatientName;
        public ObservableCollection<SeriesItemViewModel> Series { get; }
        public int SeriesCount => Model.SeriesCount;

        public SeriesItemViewModel? SelectedSeries
        {
            get => _selectedSeries;
            set => SetProperty(ref _selectedSeries, value);
        }

        public string StudyDateText => Model.StudyDate?.ToString("g") ?? Properties.Resources.NotAvailable;
        public string StudyDescription => string.IsNullOrWhiteSpace(Model.Description) ? Properties.Resources.UnknownStudyDescription : Model.Description;
        public int StudyId => Model.StudyId;
        public string StudyInstanceUid => Model.StudyInstanceUid;
    }
}