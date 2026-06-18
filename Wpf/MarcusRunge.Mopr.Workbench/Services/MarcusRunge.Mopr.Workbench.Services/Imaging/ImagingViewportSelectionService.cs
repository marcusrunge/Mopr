using MarcusRunge.Mopr.Workbench.Services.Interfaces.Imaging;
using System;

namespace MarcusRunge.Mopr.Workbench.Services.Imaging
{
    public sealed class ImagingViewportSelectionService : IImagingViewportSelectionService
    {
        private string _activeViewportId = "Single.Main";

        public event EventHandler<ImagingViewportSelectionChangedEventArgs>? ActiveViewportChanged;

        public string ActiveViewportId => _activeViewportId;

        public void SelectViewport(string viewportId)
        {
            if (string.IsNullOrWhiteSpace(viewportId))
            {
                return;
            }

            SetActiveViewport(viewportId);
        }

        public void SetDefaultViewportForLayout(string viewportId)
        {
            if (string.IsNullOrWhiteSpace(viewportId))
            {
                return;
            }

            SetActiveViewport(viewportId);
        }

        private void SetActiveViewport(string viewportId)
        {
            if (string.Equals(_activeViewportId, viewportId, StringComparison.Ordinal))
            {
                return;
            }

            var oldViewportId = _activeViewportId;
            _activeViewportId = viewportId;

            ActiveViewportChanged?.Invoke(this, new ImagingViewportSelectionChangedEventArgs(oldViewportId, _activeViewportId));
        }
    }
}