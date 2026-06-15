using MarcusRunge.Mopr.Workbench.Core.Mvvm;
using Prism.Commands;
using System.Windows.Media;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.ViewModels
{

    public sealed class ImageViewerViewModel : ViewModelBase
    {
        private ImageSource? _currentImage;
        private int _currentSlice = 1;
        private int _sliceCount = 1;
        private double _zoomFactor = 1.0;

        public ImageViewerViewModel()
        {
            ZoomCommand = new DelegateCommand(ActivateZoom);
            PanCommand = new DelegateCommand(ActivatePan);
            WindowLevelCommand = new DelegateCommand(ActivateWindowLevel);
            CrosshairCommand = new DelegateCommand(ToggleCrosshair);
            ResetViewCommand = new DelegateCommand(ResetView);
        }

        public DelegateCommand ZoomCommand { get; }

        public DelegateCommand PanCommand { get; }

        public DelegateCommand WindowLevelCommand { get; }

        public DelegateCommand CrosshairCommand { get; }

        public DelegateCommand ResetViewCommand { get; }

        public string ViewerTitle => "Image Viewer";

        public string ViewerSubtitle => "Keine Serie aktiv";

        public ImageSource? CurrentImage
        {
            get => _currentImage;
            private set
            {
                if (SetProperty(ref _currentImage, value))
                {
                    RaisePropertyChanged(nameof(IsEmptyViewerVisible));
                }
            }
        }

        public bool IsEmptyViewerVisible => CurrentImage is null;

        public int CurrentSlice
        {
            get => _currentSlice;
            set
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

        public double ZoomFactor
        {
            get => _zoomFactor;
            private set
            {
                if (SetProperty(ref _zoomFactor, value))
                {
                    RaisePropertyChanged(nameof(ZoomDisplayText));
                }
            }
        }

        public string ZoomDisplayText => $"{ZoomFactor:P0}";

        private void ActivateZoom()
        {
        }

        private void ActivatePan()
        {
        }

        private void ActivateWindowLevel()
        {
        }

        private void ToggleCrosshair()
        {
        }

        private void ResetView()
        {
            ZoomFactor = 1.0;
            CurrentSlice = 1;
        }
    }
}