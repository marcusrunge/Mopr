using MarcusRunge.Mopr.Workbench.Contracts.Models;
using MarcusRunge.Mopr.Workbench.Core.Mvvm;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.ViewModels
{
    public sealed class ViewportTileViewModel(
        string viewportId,
        string title) : ViewModelBase
    {
        private IReadOnlyList<string> _seriesFiles = [];
        private ImageSource? _currentImage;
        private int _currentSlice = 1;
        private string? _currentFileName;
        private string? _currentFilePath;
        private SeriesInfo? _series;
        private int _sliceCount = 1;

        public string ViewportId { get; } = viewportId;

        public string Title { get; } = title;

        public SeriesInfo? Series
        {
            get => _series;
            private set
            {
                if (SetProperty(ref _series, value))
                {
                    RaisePropertyChanged(nameof(DisplayTitle));
                    RaisePropertyChanged(nameof(DisplaySubtitle));
                }
            }
        }

        public string DisplayTitle => Series == null ? Title : Series.Name;

        public string DisplaySubtitle => Series == null ? "Keine Serie" : $"{Series.Modality} · {Series.ImageCount} Bilder";

        public IReadOnlyList<string> SeriesFiles { get => _seriesFiles; private set => SetProperty(ref _seriesFiles, value); }

        public int CurrentSlice
        {
            get => _currentSlice;
            private set
            {
                if (SetProperty(ref _currentSlice, value))
                {
                    RaisePropertyChanged(nameof(SliceDisplayText));
                }
            }
        }

        public int SliceCount
        {
            get => _sliceCount;
            private set
            {
                if (SetProperty(ref _sliceCount, value))
                {
                    RaisePropertyChanged(nameof(SliceDisplayText));
                }
            }
        }

        public string SliceDisplayText => $"{CurrentSlice}/{SliceCount}";

        public ImageSource? CurrentImage
        {
            get => _currentImage;
            set
            {
                if (SetProperty(ref _currentImage, value))
                {
                    RaisePropertyChanged(nameof(IsEmptyViewerVisible));
                }
            }
        }

        public bool IsEmptyViewerVisible => CurrentImage == null;

        public string? CurrentFileName
        {
            get => _currentFileName;
            private set
            {
                if (SetProperty(ref _currentFileName, value))
                {
                    RaisePropertyChanged(nameof(CurrentFileDisplayText));
                    RaisePropertyChanged(nameof(CurrentFileToolTipText));
                }
            }
        }

        public string? CurrentFilePath
        {
            get => _currentFilePath;
            private set
            {
                if (SetProperty(ref _currentFilePath, value))
                {
                    RaisePropertyChanged(nameof(CurrentFileToolTipText));
                }
            }
        }

        public string CurrentFileDisplayText => string.IsNullOrWhiteSpace(CurrentFileName) ? "Datei: -" : $"Datei: {CurrentFileName}";

        public string CurrentFileToolTipText => string.IsNullOrWhiteSpace(CurrentFilePath) ? "Keine Datei" : $"Aktuelle Datei:{Environment.NewLine}{CurrentFilePath}";

        public void SetSeries(SeriesInfo? series, IReadOnlyList<string> files)
        {
            Series = series;
            SeriesFiles = files ?? [];

            SliceCount = SeriesFiles.Count > 0 ? SeriesFiles.Count : Math.Max(1, series?.ImageCount ?? 1);

            SetSlice(1);

            CurrentImage = null;
        }

        public void SetSlice(int slice)
        {
            if (SliceCount <= 0)
            {
                SliceCount = 1;
            }

            if (slice < 1)
            {
                slice = 1;
            }

            if (slice > SliceCount)
            {
                slice = SliceCount;
            }

            CurrentSlice = slice;

            UpdateCurrentFile();
        }

        public void MoveSlice(int delta) => SetSlice(CurrentSlice + delta);

        private void UpdateCurrentFile()
        {
            if (SeriesFiles.Count == 0)
            {
                CurrentFileName = null;
                CurrentFilePath = null;
                return;
            }

            var index = CurrentSlice - 1;

            if (index < 0 || index >= SeriesFiles.Count)
            {
                CurrentFileName = null;
                CurrentFilePath = null;
                return;
            }

            var filePath = SeriesFiles[index];

            CurrentFilePath = filePath;
            CurrentFileName = Path.GetFileName(filePath);
        }
    }
}