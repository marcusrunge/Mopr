using MarcusRunge.Base;
using MarcusRunge.Mopr.Workbench.Contracts.Models;
using MarcusRunge.Mopr.Workbench.Services.Core.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Core.Contracts.Imaging;

namespace MarcusRunge.Mopr.Workbench.Services.Core.Implementations.Imaging
{
    internal sealed class ImagingViewportService : CreateableBindableBase<IImagingViewportService, ImagingViewportService, IImagingServiceBase>, IImagingViewportService
    {
        private ImagingViewportState _state = new ImagingViewportState(currentSlice: 1, sliceCount: 1, zoomFactor: 1.0, windowValue: 400, levelValue: 40);

        public event EventHandler<ImagingViewportStateChangedEventArgs>? StateChanged;

        public ImagingViewportState State => _state;

        public void Reset() => UpdateState(new ImagingViewportState(currentSlice: 1, sliceCount: _state.SliceCount, zoomFactor: 1.0, windowValue: 400, levelValue: 40));

        public void SetSlice(int currentSlice, int sliceCount)
        {
            if (sliceCount < 1)
            {
                sliceCount = 1;
            }

            if (currentSlice < 1)
            {
                currentSlice = 1;
            }

            if (currentSlice > sliceCount)
            {
                currentSlice = sliceCount;
            }

            UpdateState(new ImagingViewportState(currentSlice, sliceCount, _state.ZoomFactor, _state.WindowValue, _state.LevelValue));
        }

        public void SetWindowLevel(double windowValue, double levelValue)
        {
            if (windowValue < 1)
            {
                windowValue = 1;
            }

            UpdateState(new ImagingViewportState(_state.CurrentSlice, _state.SliceCount, _state.ZoomFactor, windowValue, levelValue));
        }

        public void SetZoom(double zoomFactor)
        {
            if (zoomFactor < 0.1)
            {
                zoomFactor = 0.1;
            }

            if (zoomFactor > 20.0)
            {
                zoomFactor = 20.0;
            }

            UpdateState(new ImagingViewportState(_state.CurrentSlice, _state.SliceCount, zoomFactor, _state.WindowValue, _state.LevelValue));
        }

        protected override void OnCreate(IImagingServiceBase @base)
        {
        }

        protected override Task OnCreateAsync(IImagingServiceBase @base, CancellationToken cancellationToken) => Task.CompletedTask;

        private void UpdateState(ImagingViewportState newState)
        {
            if (_state.CurrentSlice == newState.CurrentSlice &&
                _state.SliceCount == newState.SliceCount &&
                Math.Abs(_state.ZoomFactor - newState.ZoomFactor) < 0.0001 &&
                Math.Abs(_state.WindowValue - newState.WindowValue) < 0.0001 &&
                Math.Abs(_state.LevelValue - newState.LevelValue) < 0.0001)
            {
                return;
            }

            _state = newState;

            StateChanged?.Invoke(this, new ImagingViewportStateChangedEventArgs(_state));
        }
    }
}