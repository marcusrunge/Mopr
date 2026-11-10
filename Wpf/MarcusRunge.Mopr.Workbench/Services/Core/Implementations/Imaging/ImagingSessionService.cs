using MarcusRunge.Base;
using MarcusRunge.Mopr.Workbench.Contracts.Imaging;
using MarcusRunge.Mopr.Workbench.Services.Core.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Core.Contracts.Imaging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MarcusRunge.Mopr.Workbench.Services.Core.Implementations.Imaging
{
    /// <summary>
    /// Coordinates clearing the current study and every transient imaging state.
    /// </summary>
    internal sealed class ImagingSessionService : CreateableBindableBase<IImagingSessionService, ImagingSessionService, IImagingServiceBase>, IImagingSessionService
    {
        private IImagingServiceBase? _base;
        private bool _isClearing;
        private bool _isInitialized;
        private IImagingServiceBase Base => _base ?? throw new InvalidOperationException("Service has not been initialized.");
        private IImagingService ImagingService => Base as IImagingService ?? throw new InvalidOperationException("The imaging service contract is not available.");

        public event EventHandler? StateChanged;
        public bool HasOpenStudy => ImagingService.ImagingStudyService?.CurrentStudy is not null;
        public bool HasUnsavedChanges => ImagingService.ImagingSessionChangeTracker?.HasUnsavedChanges == true;

        public void Clear()
        {
            if (HasUnsavedChanges) throw new InvalidOperationException("Unsaved imaging changes must be saved or explicitly discarded before the current study can be cleared.");

            /*
             * Suppress forwarded state notifications while the coordinated clear is in
             * progress. ImagingStudyService.Clear() raises StudyLoaded itself, but the
             * session must publish exactly one final, fully consistent state change.
             */
            _isClearing = true;

            try
            {
                ImagingService.ImagingStudyService?.Clear();
                ImagingService.ImagingSelectionService?.ClearSelection();
                ImagingService.ImagingViewportService?.Reset();
                ImagingService.ImagingWindowLevelService?.ResetWindowLevelToDefault();
                ImagingService.ImagingToolService?.ClearActiveTool();
                ImagingService.ImagingSessionChangeTracker?.Clear();
            }
            finally
            {
                _isClearing = false;
            }

            StateChanged?.Invoke(this, EventArgs.Empty);
        }

        protected override void OnCreate(IImagingServiceBase @base) => Initialize(@base);

        protected override Task OnCreateAsync(IImagingServiceBase @base, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Initialize(@base);
            return Task.CompletedTask;
        }

        private void Initialize(IImagingServiceBase @base)
        {
            if (@base is null) throw new ArgumentNullException(nameof(@base));
            if (_isInitialized)
            {
                if (!ReferenceEquals(_base, @base)) throw new InvalidOperationException("Service has already been initialized with a different imaging-service base.");
                return;
            }

            _base = @base;
            ImagingService.ImagingStudyService?.StudyLoaded += OnStudyLoaded;
            ImagingService.ImagingSessionChangeTracker?.UnsavedChangesChanged += OnUnsavedChangesChanged;
            _isInitialized = true;
        }

        private void OnStudyLoaded(object? sender, ImagingStudyLoadedEventArgs e)
        {
            if (!_isClearing) StateChanged?.Invoke(this, EventArgs.Empty);
        }

        private void OnUnsavedChangesChanged(object? sender, EventArgs e)
        {
            if (!_isClearing) StateChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}