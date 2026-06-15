using MarcusRunge.Mopr.Workbench.Core.Mvvm;
using Prism.Mvvm;
using System.Collections.ObjectModel;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.ViewModels
{
    public sealed class SeriesPanelViewModel : ViewModelBase
    {
        private string _searchText = string.Empty;
        private SeriesItemViewModel? _selectedSeries;

        public SeriesPanelViewModel()
        {
            Series =
            [
                new SeriesItemViewModel("MR", "T1 axial", "T1 gewichtete axiale Serie", 128),
                new SeriesItemViewModel("MR", "T2 axial", "T2 gewichtete axiale Serie", 128),
                new SeriesItemViewModel("MR", "FLAIR coronal", "FLAIR koronale Serie", 96),
                new SeriesItemViewModel("CT", "CT axial", "CT axiale Rekonstruktion", 320),
                new SeriesItemViewModel("MPR", "MPR", "Multiplanare Rekonstruktion", 1)
            ];

            SelectedSeries = Series.Count > 0 ? Series[0] : null;
        }

        public ObservableCollection<SeriesItemViewModel> Series { get; }

        public string SearchText
        {
            get => _searchText;
            set => SetProperty(ref _searchText, value);
        }

        public SeriesItemViewModel? SelectedSeries
        {
            get => _selectedSeries;
            set => SetProperty(ref _selectedSeries, value);
        }
    }

    public sealed class SeriesItemViewModel(
        string modality,
        string name,
        string description,
        int imageCount) : BindableBase
    {
        public string Modality { get; } = modality;

        public string Name { get; } = name;

        public string Description { get; } = description;

        public int ImageCount { get; } = imageCount;

        public string ImageCountDisplayText => $"{ImageCount} Bilder";
    }
}