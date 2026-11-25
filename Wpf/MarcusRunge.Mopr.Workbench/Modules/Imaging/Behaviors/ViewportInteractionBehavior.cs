using MarcusRunge.Mopr.Workbench.Modules.Imaging.Infrastructure.Viewports;
using MarcusRunge.Mopr.Workbench.Modules.Imaging.ViewModels;
using MarcusRunge.Mopr.Workbench.Modules.Imaging.Views.Viewports;
using Microsoft.Xaml.Behaviors;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.Behaviors
{
    public sealed class ViewportInteractionBehavior : Behavior<ViewportTile>
    {
        private FrameworkElement? _interactionHost;
        private bool _isPanDragging;
        private bool _isWindowLevelDragging;
        private Point _panDragLastPosition;
        private Point _windowLevelDragStartPosition;

        protected override void OnAttached()
        {
            base.OnAttached();

            InputManager.Current.PreProcessInput += OnPreProcessInput;
            AssociatedObject.Loaded += OnAssociatedObjectLoaded;
            AssociatedObject.Unloaded += OnAssociatedObjectUnloaded;
        }

        protected override void OnDetaching()
        {
            DetachInteractionHost();

            InputManager.Current.PreProcessInput -= OnPreProcessInput;
            AssociatedObject.Loaded -= OnAssociatedObjectLoaded;
            AssociatedObject.Unloaded -= OnAssociatedObjectUnloaded;

            base.OnDetaching();
        }

        private void AttachInteractionHost()
        {
            DetachInteractionHost();

            _interactionHost = AssociatedObject.FindName("ViewportImageHost") as FrameworkElement;

            if (_interactionHost == null)
            {
                return;
            }

            _interactionHost.PreviewKeyDown += OnPreviewKeyDown;
            _interactionHost.PreviewMouseLeftButtonDown += OnPreviewMouseLeftButtonDown;
            _interactionHost.PreviewMouseWheel += OnPreviewMouseWheel;
            _interactionHost.PreviewMouseMove += OnPreviewMouseMove;
            _interactionHost.PreviewMouseLeftButtonUp += OnPreviewMouseLeftButtonUp;
            _interactionHost.MouseEnter += OnMouseEnter;
            _interactionHost.MouseLeave += OnMouseLeave;

            AssociatedObject.Tile?.PropertyChanged += OnTilePropertyChanged;
            UpdateInteractionCursor();
        }

        private void CompleteWindowLevelDrag()
        {
            _isWindowLevelDragging = false;

            if (!string.IsNullOrWhiteSpace(AssociatedObject.ViewportId) && AssociatedObject.WindowLevelDragCompletedCommand?.CanExecute(AssociatedObject.ViewportId) == true)
            {
                AssociatedObject.WindowLevelDragCompletedCommand.Execute(AssociatedObject.ViewportId);
            }
        }

        private void DetachInteractionHost()
        {
            CompletePanDrag();

            if (_interactionHost == null)
            {
                return;
            }

            _interactionHost.PreviewKeyDown -= OnPreviewKeyDown;
            _interactionHost.PreviewMouseLeftButtonDown -= OnPreviewMouseLeftButtonDown;
            _interactionHost.PreviewMouseWheel -= OnPreviewMouseWheel;
            _interactionHost.PreviewMouseMove -= OnPreviewMouseMove;
            _interactionHost.PreviewMouseLeftButtonUp -= OnPreviewMouseLeftButtonUp;
            _interactionHost.MouseEnter -= OnMouseEnter;
            _interactionHost.MouseLeave -= OnMouseLeave;

            AssociatedObject.Tile?.PropertyChanged -= OnTilePropertyChanged;
            _interactionHost.ClearValue(FrameworkElement.CursorProperty);
            _interactionHost = null;
        }

        private void ExecutePixelHover(int? pixelX, int? pixelY)
        {
            var hoverInfo = new ViewportPixelHoverInfo(AssociatedObject.ViewportId, pixelX, pixelY);

            if (AssociatedObject.PixelHoverCommand?.CanExecute(hoverInfo) == true)
            {
                AssociatedObject.PixelHoverCommand.Execute(hoverInfo);
            }
        }

        private FrameworkElement? GetInteractionHost() => _interactionHost;

        private static bool IsControlPressed() => (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control || Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);

        private void OnAssociatedObjectLoaded(object sender, RoutedEventArgs e) => AttachInteractionHost();

        private void OnAssociatedObjectUnloaded(object sender, RoutedEventArgs e) => DetachInteractionHost();

        private void OnMouseEnter(object sender, MouseEventArgs e) => UpdateInteractionCursor();

        private void OnMouseLeave(object sender, MouseEventArgs e)
        {
            ExecutePixelHover(null, null);
            UpdateInteractionCursor();
        }

        private void OnPreProcessInput(object sender, PreProcessInputEventArgs e)
        {
            if (e.StagingItem.Input is KeyEventArgs keyEventArgs && (keyEventArgs.Key == Key.LeftCtrl || keyEventArgs.Key == Key.RightCtrl)) UpdateInteractionCursor();
        }

        private void OnTilePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (string.Equals(e.PropertyName, nameof(ViewportTileViewModel.IsZoomActive), StringComparison.Ordinal)) UpdateInteractionCursor();
        }

        private void UpdateInteractionCursor()
        {
            if (_interactionHost == null) return;

            if (_interactionHost.IsMouseOver && AssociatedObject.Tile?.IsZoomActive == true && IsControlPressed())
            {
                _interactionHost.Cursor = Cursors.ScrollNS;
                return;
            }

            _interactionHost.ClearValue(FrameworkElement.CursorProperty);
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!AssociatedObject.IsInteractive || !AssociatedObject.IsActive || !ViewportSliceNavigation.TryNavigate(AssociatedObject.Tile, e.Key))
            {
                return;
            }

            e.Handled = true;
        }

        private void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var viewportTile = AssociatedObject;
            var tile = viewportTile.Tile;

            if (!viewportTile.IsInteractive || tile?.CurrentDicomFrame == null) return;

            SelectAndFocusViewport();

            var shouldZoom = IsControlPressed() && tile.IsZoomActive;

            if (shouldZoom)
            {
                ApplyZoomAtPointer(tile, e);
                e.Handled = true;
                return;
            }

            var sliceDelta = ViewportSliceNavigation.GetWheelSliceDelta(e.Delta);

            if (sliceDelta == 0) return;

            tile.RequestSliceNavigation(sliceDelta);
            e.Handled = true;
        }

        private void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var viewportTile = AssociatedObject;

            if (!viewportTile.IsInteractive)
            {
                e.Handled = true;
                return;
            }

            _interactionHost?.Focus();

            if (!string.IsNullOrWhiteSpace(viewportTile.ViewportId) && viewportTile.SelectViewportCommand?.CanExecute(viewportTile.ViewportId) == true)
            {
                viewportTile.SelectViewportCommand.Execute(viewportTile.ViewportId);
            }

            if (viewportTile.Tile?.IsPanActive == true && viewportTile.Tile.CurrentDicomFrame != null)
            {
                var host = GetInteractionHost();
                if (host == null) return;
                _panDragLastPosition = e.GetPosition(host);
                _isPanDragging = true;
                host.CaptureMouse();
                e.Handled = true;
                return;
            }

            if (viewportTile.IsMeasureActive)
            {
                var pointInfo = TryCreateMeasurementPointInfo(e);

                if (pointInfo != null &&
                    viewportTile.MeasurementPointCommand?.CanExecute(pointInfo) == true)
                {
                    viewportTile.MeasurementPointCommand.Execute(pointInfo);
                }

                e.Handled = true;
                return;
            }

            if (viewportTile.IsWindowLevelActive)
            {
                var host = GetInteractionHost();

                if (host == null)
                {
                    return;
                }

                _isWindowLevelDragging = true;
                _windowLevelDragStartPosition = e.GetPosition(host);

                if (viewportTile.WindowLevelDragStartedCommand?.CanExecute(viewportTile.ViewportId) == true)
                {
                    viewportTile.WindowLevelDragStartedCommand.Execute(viewportTile.ViewportId);
                }

                e.Handled = true;
                return;
            }

            e.Handled = true;
        }

        private void OnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isPanDragging)
            {
                CompletePanDrag();
                e.Handled = true;
                return;
            }

            if (!_isWindowLevelDragging) return;

            CompleteWindowLevelDrag();
            e.Handled = true;
        }

        private void OnPreviewMouseMove(object sender, MouseEventArgs e)
        {
            ReportPixelHover(e);

            if (_isPanDragging)
            {
                var panHost = GetInteractionHost();
                var tile = AssociatedObject.Tile;

                if (panHost == null || tile == null || e.LeftButton != MouseButtonState.Pressed)
                {
                    CompletePanDrag();
                    e.Handled = true;
                    return;
                }

                var panCurrentPosition = e.GetPosition(panHost);
                tile.AddPan(panCurrentPosition.X - _panDragLastPosition.X, panCurrentPosition.Y - _panDragLastPosition.Y);
                _panDragLastPosition = panCurrentPosition;
                e.Handled = true;
                return;
            }

            if (AssociatedObject.IsMeasureActive)
            {
                var previewPointInfo = TryCreateMeasurementPointInfo(e);

                if (previewPointInfo != null && AssociatedObject.MeasurementPreviewCommand?.CanExecute(previewPointInfo) == true)
                {
                    AssociatedObject.MeasurementPreviewCommand.Execute(previewPointInfo);
                }
            }

            if (!_isWindowLevelDragging)
            {
                return;
            }

            var host = GetInteractionHost();

            if (host == null)
            {
                CompleteWindowLevelDrag();
                return;
            }

            if (e.LeftButton != MouseButtonState.Pressed)
            {
                CompleteWindowLevelDrag();
                e.Handled = true;
                return;
            }

            var currentPosition = e.GetPosition(host);

            var totalDeltaX = currentPosition.X - _windowLevelDragStartPosition.X;
            var totalDeltaY = currentPosition.Y - _windowLevelDragStartPosition.Y;

            var dragInfo = new ViewportWindowLevelDragInfo(AssociatedObject.ViewportId, totalDeltaX, totalDeltaY);

            if (AssociatedObject.WindowLevelDragCommand?.CanExecute(dragInfo) == true)
            {
                AssociatedObject.WindowLevelDragCommand.Execute(dragInfo);
            }

            e.Handled = true;
        }

        private void ApplyZoomAtPointer(ViewportTileViewModel tile, MouseWheelEventArgs e)
        {
            var targetZoomFactor = e.Delta > 0 ? tile.ZoomFactor * ViewportTransformState.DefaultZoomStep : tile.ZoomFactor / ViewportTransformState.DefaultZoomStep;
            targetZoomFactor = ViewportImageGeometryCalculator.ClampZoomFactor(targetZoomFactor);

            if (Math.Abs(targetZoomFactor - tile.ZoomFactor) < 0.0001) return;

            var host = GetInteractionHost();

            if (host == null || !ViewportImageGeometryCalculator.TryCreate(tile.CurrentDicomFrame, host, tile.ZoomFactor, tile.PanOffsetX, tile.PanOffsetY, out var geometry))
            {
                // A temporary missing layout geometry must not suppress the user's zoom input.
                tile.SetZoomAndPan(targetZoomFactor, tile.PanOffsetX, tile.PanOffsetY);
                return;
            }

            var anchorHostPoint = e.GetPosition(host);
            var targetPanOffset = ViewportImageGeometryCalculator.CalculateZoomAnchoredPanOffset(geometry, targetZoomFactor, anchorHostPoint, host.ActualWidth, host.ActualHeight);
            tile.SetZoomAndPan(targetZoomFactor, targetPanOffset.X, targetPanOffset.Y);
        }

        private void CompletePanDrag()
        {
            if (!_isPanDragging) return;
            _isPanDragging = false;
            if (_interactionHost?.IsMouseCaptured == true) _interactionHost.ReleaseMouseCapture();
        }

        private void SelectAndFocusViewport()
        {
            if (!string.IsNullOrWhiteSpace(AssociatedObject.ViewportId) && AssociatedObject.SelectViewportCommand?.CanExecute(AssociatedObject.ViewportId) == true) AssociatedObject.SelectViewportCommand.Execute(AssociatedObject.ViewportId);
            _interactionHost?.Focus();
        }

        private void ReportPixelHover(MouseEventArgs e)
        {
            var hoverInfo = TryCreatePixelHoverInfo(e);

            if (hoverInfo == null)
            {
                ExecutePixelHover(null, null);

                return;
            }

            ExecutePixelHover(hoverInfo.PixelX, hoverInfo.PixelY);
        }

        private ViewportMeasurementPointInfo? TryCreateMeasurementPointInfo(MouseEventArgs e)
        {
            var host = GetInteractionHost();

            if (host == null)
            {
                return null;
            }

            if (!ViewportImageGeometryCalculator.TryCreate(AssociatedObject.Tile?.CurrentDicomFrame, host, AssociatedObject.Tile?.ZoomFactor ?? ViewportImageGeometryCalculator.DefaultZoomFactor, AssociatedObject.Tile?.PanOffsetX ?? 0.0, AssociatedObject.Tile?.PanOffsetY ?? 0.0, out var geometry))
            {
                return null;
            }

            var hostPoint = e.GetPosition(host);

            if (!geometry.ContainsHostPoint(hostPoint))
            {
                return null;
            }

            var imagePoint = geometry.HostPointToImagePoint(hostPoint);

            if (!geometry.ContainsImagePoint(imagePoint.X, imagePoint.Y))
            {
                return null;
            }

            return new ViewportMeasurementPointInfo(AssociatedObject.ViewportId, imagePoint.X, imagePoint.Y);
        }

        private ViewportPixelHoverInfo? TryCreatePixelHoverInfo(MouseEventArgs e)
        {
            var host = GetInteractionHost();

            if (host == null)
            {
                return null;
            }

            if (!ViewportImageGeometryCalculator.TryCreate(AssociatedObject.Tile?.CurrentDicomFrame, host, AssociatedObject.Tile?.ZoomFactor ?? ViewportImageGeometryCalculator.DefaultZoomFactor, AssociatedObject.Tile?.PanOffsetX ?? 0.0, AssociatedObject.Tile?.PanOffsetY ?? 0.0, out var geometry))
            {
                return null;
            }

            var hostPoint = e.GetPosition(host);

            if (!geometry.ContainsHostPoint(hostPoint))
            {
                return null;
            }

            var imagePoint = geometry.HostPointToImagePoint(hostPoint);

            if (!geometry.ContainsImagePoint(imagePoint.X, imagePoint.Y))
            {
                return null;
            }

            var pixelX = geometry.ToPixelX(imagePoint.X);
            var pixelY = geometry.ToPixelY(imagePoint.Y);

            if (pixelX < 0 || pixelY < 0 || pixelX >= geometry.FrameWidth || pixelY >= geometry.FrameHeight)
            {
                return null;
            }

            return new ViewportPixelHoverInfo(AssociatedObject.ViewportId, pixelX, pixelY);
        }
    }
}
