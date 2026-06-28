using MarcusRunge.Mopr.Workbench.Contracts.Models;
using MarcusRunge.Mopr.Workbench.Core.Mvvm;
using MarcusRunge.Mopr.Workbench.Modules.Imaging.Properties;
using MarcusRunge.Mopr.Workbench.Services.Dicom.Contracts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.ViewModels
{
    public sealed class ViewportTileViewModel(string viewportId, string title) : ViewModelBase
    {
        private DicomImageFrame? _currentDicomFrame;
        private string? _currentFileName, _currentFilePath;
        private ImageSource? _currentImage;
        private int _currentSlice = 1, _sliceCount = 1;
        private SeriesInfo? _series;
        private IReadOnlyList<string> _seriesFiles = [];
        private double? _windowCenter, _windowWidth, _currentPixelValue;
        private int? _currentPixelX, _currentPixelY;

        public DicomImageFrame? CurrentDicomFrame
        {
            get => _currentDicomFrame;
            set => SetProperty(ref _currentDicomFrame, value);
        }
        public int? CurrentPixelX
        {
            get => _currentPixelX;
            private set
            {
                if (SetProperty(ref _currentPixelX, value))
                {
                    RaisePropertyChanged(nameof(CurrentPixelDisplayText));
                }
            }
        }

        public int? CurrentPixelY
        {
            get => _currentPixelY;
            private set
            {
                if (SetProperty(ref _currentPixelY, value))
                {
                    RaisePropertyChanged(nameof(CurrentPixelDisplayText));
                }
            }
        }

        public double? CurrentPixelValue
        {
            get => _currentPixelValue;
            private set
            {
                if (SetProperty(ref _currentPixelValue, value))
                {
                    RaisePropertyChanged(nameof(CurrentPixelDisplayText));
                }
            }
        }

        public string CurrentPixelDisplayText
        {
            get
            {
                if (!CurrentPixelX.HasValue ||
                    !CurrentPixelY.HasValue ||
                    !CurrentPixelValue.HasValue)
                {
                    return $"{Resources.ViewportTile_Pixel}: -";
                }

                if (string.Equals(CurrentDicomFrame?.Modality,
                        $"{Resources.ViewportTile_Ct}", StringComparison.OrdinalIgnoreCase))
                {
                    return $"{Resources.ViewportTile_Pixel}: X={CurrentPixelX.Value} Y={CurrentPixelY.Value} · HU={CurrentPixelValue.Value:0}";
                }

                return $"{Resources.ViewportTile_Pixel}: X={CurrentPixelX.Value} Y={CurrentPixelY.Value} · Wert={CurrentPixelValue.Value:0}";
            }
        }
        public string CurrentFileDisplayText => string.IsNullOrWhiteSpace(CurrentFileName) ? $"{Resources.ViewportTile_File}: -" : $"{Resources.ViewportTile_File}: {CurrentFileName}";

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

        public string CurrentFileToolTipText => string.IsNullOrWhiteSpace(CurrentFilePath) ? Resources.ViewportTile_NoFile : $"{Resources.ViewportTile_ActualFile}:{Environment.NewLine}{CurrentFilePath}";

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
        public void SetCurrentPixel(int pixelX, int pixelY, double value)
        {
            CurrentPixelX = pixelX;
            CurrentPixelY = pixelY;
            CurrentPixelValue = value;
        }

        public void ClearCurrentPixel()
        {
            CurrentPixelX = null;
            CurrentPixelY = null;
            CurrentPixelValue = null;
        }
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

        public string DisplaySubtitle => Series == null ? Resources.ViewportTile_Series_None : $"{Series.Modality} · {Series.ImageCount} {Resources.ViewportTile_Images}";

        public string DisplayTitle => Series == null ? Title : Series.Name;

        public bool IsEmptyViewerVisible => CurrentImage == null;

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

        public IReadOnlyList<string> SeriesFiles { get => _seriesFiles; private set => SetProperty(ref _seriesFiles, value); }

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

        public string Title { get; } = title;

        public string ViewportId { get; } = viewportId;

        public double? WindowCenter
        {
            get => _windowCenter;
            private set
            {
                if (SetProperty(ref _windowCenter, value))
                {
                    RaisePropertyChanged(nameof(WindowDisplayText));
                }
            }
        }

        public string WindowDisplayText
        {
            get
            {
                if (!WindowCenter.HasValue || !WindowWidth.HasValue)
                {
                    return $"{Resources.ViewportTile_WindowLevel}: {Resources.ViewportTile_Default}";
                }

                return $"{Resources.ViewportTile_WindowLevel}: {WindowWidth.Value:0}/{WindowCenter.Value:0}";
            }
        }

        public double? WindowWidth
        {
            get => _windowWidth;
            private set
            {
                if (SetProperty(ref _windowWidth, value))
                {
                    RaisePropertyChanged(nameof(WindowDisplayText));
                }
            }
        }

        public void MoveSlice(int delta) => SetSlice(CurrentSlice + delta);

        public void ResetWindowLevel()
        {
            WindowCenter = null;
            WindowWidth = null;
        }

        public void SetSeries(SeriesInfo? series, IReadOnlyList<string> files)
        {
            Series = series;
            SeriesFiles = files ?? [];

            SliceCount = SeriesFiles.Count > 0 ? SeriesFiles.Count : Math.Max(1, series?.ImageCount ?? 1);

            CurrentDicomFrame = null;
            CurrentImage = null;

            SetSlice(1);

            if (series == null)
            {
                CurrentFileName = null;
                CurrentFilePath = null;
            }
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

            if (CurrentSlice != slice)
            {
                CurrentDicomFrame = null;
                CurrentImage = null;
                ClearCurrentPixel();
            }

            CurrentSlice = slice;

            UpdateCurrentFile();
        }

        public void SetWindowLevel(double? windowCenter, double? windowWidth)
        {
            WindowCenter = windowCenter;
            WindowWidth = windowWidth;
        }

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