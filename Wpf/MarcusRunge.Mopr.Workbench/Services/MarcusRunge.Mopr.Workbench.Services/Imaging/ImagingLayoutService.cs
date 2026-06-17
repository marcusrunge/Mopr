using MarcusRunge.Mopr.Workbench.Contracts.Imaging;
using MarcusRunge.Mopr.Workbench.Services.Interfaces.Imaging;
using System;

namespace MarcusRunge.Mopr.Workbench.Services.Imaging
{
    public sealed class ImagingLayoutService : IImagingLayoutService
    {
        private ImagingLayout _currentLayout = ImagingLayout.Single;

        public event EventHandler<ImagingLayoutChangedEventArgs>? CurrentLayoutChanged;

        public ImagingLayout CurrentLayout => _currentLayout;

        public void CycleNextLayout()
        {
            var nextLayout = _currentLayout switch
            {
                ImagingLayout.Single => ImagingLayout.TwoByTwo,
                ImagingLayout.TwoByTwo => ImagingLayout.Mpr,
                ImagingLayout.Mpr => ImagingLayout.AxialSagittalCoronal,
                ImagingLayout.AxialSagittalCoronal => ImagingLayout.Single,
                _ => ImagingLayout.Single
            };

            SetLayout(nextLayout);
        }

        public void ResetLayout() => SetLayout(ImagingLayout.Single);

        public void SetLayout(ImagingLayout layout)
        {
            if (_currentLayout == layout)
            {
                return;
            }

            var oldLayout = _currentLayout;
            _currentLayout = layout;

            CurrentLayoutChanged?.Invoke(this, new ImagingLayoutChangedEventArgs(oldLayout, _currentLayout));
        }
    }
}