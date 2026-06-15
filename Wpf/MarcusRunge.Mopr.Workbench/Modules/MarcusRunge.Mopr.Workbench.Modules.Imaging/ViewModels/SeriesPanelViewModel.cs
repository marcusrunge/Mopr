using MarcusRunge.Mopr.Workbench.Contracts.Models;
using MarcusRunge.Mopr.Workbench.Core.Mvvm;
using MarcusRunge.Mopr.Workbench.Services.Interfaces;
using Prism.Mvvm;
using System.Collections.ObjectModel;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.ViewModels
{
    public sealed class SeriesPanelViewModel : ViewModelBase
    {
        private readonly IImagingSelectionService _selectionService;

        private string _searchText = string.Empty;
        private SeriesInfo? _selectedSeries;

        public SeriesPanelViewModel(IImagingSelectionService selectionService)
        {
            _selectionService = selectionService;

            Series =
            [
                new SeriesInfo(
                id: "mr-t1-axial",
                modality: "MR",
                name: "T1 axial",
                description: "T1 gewichtete axiale Serie",
                imageCount: 128,
                studyId: "demo-study",
                seriesNumber: 1),

            new SeriesInfo(
                id: "mr-t2-axial",
                modality: "MR",
                name: "T2 axial",
                description: "T2 gewichtete axiale Serie",
                imageCount: 128,
                studyId: "demo-study",
                seriesNumber: 2),

            new SeriesInfo(
                id: "mr-flair-coronal",
                modality: "MR",
                name: "FLAIR coronal",
                description: "FLAIR koronale Serie",
                imageCount: 96,
                studyId: "demo-study",
                seriesNumber: 3),

            new SeriesInfo(
                id: "ct-axial",
                modality: "CT",
                name: "CT axial",
                description: "CT axiale Rekonstruktion",
                imageCount: 320,
                studyId: "demo-study",
                seriesNumber: 4),

            new SeriesInfo(
                id: "mpr",
                modality: "MPR",
                name: "MPR",
                description: "Multiplanare Rekonstruktion",
                imageCount: 1,
                studyId: "demo-study",
                seriesNumber: 5)
            ];

            SelectedSeries = Series.Count > 0 ? Series[0] : null;
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
    }
}