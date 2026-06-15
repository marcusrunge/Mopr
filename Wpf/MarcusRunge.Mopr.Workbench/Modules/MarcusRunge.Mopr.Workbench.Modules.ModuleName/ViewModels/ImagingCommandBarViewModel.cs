using MarcusRunge.Mopr.Workbench.Core.Mvvm;
using Prism.Commands;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.ViewModels
{

    public sealed class ImagingCommandBarViewModel : ViewModelBase
    {
        public ImagingCommandBarViewModel()
        {
            OpenCommand = new DelegateCommand(Open);
            LayoutCommand = new DelegateCommand(ChangeLayout);
            WindowLevelCommand = new DelegateCommand(ChangeWindowLevel);
            ZoomCommand = new DelegateCommand(ActivateZoom);
            PanCommand = new DelegateCommand(ActivatePan);
            CrosshairCommand = new DelegateCommand(ToggleCrosshair);
            ResetViewCommand = new DelegateCommand(ResetView);
            MoreCommand = new DelegateCommand(OpenMoreMenu);
        }

        public DelegateCommand OpenCommand { get; }

        public DelegateCommand LayoutCommand { get; }

        public DelegateCommand WindowLevelCommand { get; }

        public DelegateCommand ZoomCommand { get; }

        public DelegateCommand PanCommand { get; }

        public DelegateCommand CrosshairCommand { get; }

        public DelegateCommand ResetViewCommand { get; }

        public DelegateCommand MoreCommand { get; }

        private void Open()
        {
        }

        private void ChangeLayout()
        {
        }

        private void ChangeWindowLevel()
        {
        }

        private void ActivateZoom()
        {
        }

        private void ActivatePan()
        {
        }

        private void ToggleCrosshair()
        {
        }

        private void ResetView()
        {
        }

        private void OpenMoreMenu()
        {
        }
    }
}