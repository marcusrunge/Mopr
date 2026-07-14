using MarcusRunge.Mopr.Workbench.Modules.Imaging.Infrastructure.Viewports;
using MarcusRunge.Mopr.Workbench.Modules.Imaging.ViewModels;
using MarcusRunge.Mopr.Workbench.Modules.Imaging.Views.Viewports;
using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Input;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.Behaviors
{
    public sealed class ViewportInteractionBehavior : Behavior<ViewportTile>
    {
        private FrameworkElement? _interactionHost;
        private bool _isWindowLevelDragging;
        private Point _windowLevelDragStartPosition;

        protected override void OnAttached()
        {
            base.OnAttached();

            AssociatedObject.Loaded += OnAssociatedObjectLoaded;
            AssociatedObject.Unloaded += OnAssociatedObjectUnloaded;
        }

        protected override void OnDetaching()
        {
            DetachInteractionHost();

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

            _interactionHost.PreviewMouseLeftButtonDown += OnPreviewMouseLeftButtonDown;
            _interactionHost.PreviewMouseMove += OnPreviewMouseMove;
            _interactionHost.PreviewMouseLeftButtonUp += OnPreviewMouseLeftButtonUp;
            _interactionHost.MouseLeave += OnMouseLeave;
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
            if (_interactionHost == null)
            {
                return;
            }

            _interactionHost.PreviewMouseLeftButtonDown -= OnPreviewMouseLeftButtonDown;
            _interactionHost.PreviewMouseMove -= OnPreviewMouseMove;
            _interactionHost.PreviewMouseLeftButtonUp -= OnPreviewMouseLeftButtonUp;
            _interactionHost.MouseLeave -= OnMouseLeave;

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

        private void OnAssociatedObjectLoaded(object sender, RoutedEventArgs e) => AttachInteractionHost();

        private void OnAssociatedObjectUnloaded(object sender, RoutedEventArgs e) => DetachInteractionHost();

        private void OnMouseLeave(object sender, MouseEventArgs e) => ExecutePixelHover(null, null);

        private void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var viewportTile = AssociatedObject;

            if (!viewportTile.IsInteractive)
            {
                e.Handled = true;
                return;
            }

            if (!string.IsNullOrWhiteSpace(viewportTile.ViewportId) && viewportTile.SelectViewportCommand?.CanExecute(viewportTile.ViewportId) == true)
            {
                viewportTile.SelectViewportCommand.Execute(viewportTile.ViewportId);
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
            if (!_isWindowLevelDragging)
            {
                return;
            }

            CompleteWindowLevelDrag();

            e.Handled = true;
        }

        private void OnPreviewMouseMove(object sender, MouseEventArgs e)
        {
            ReportPixelHover(e);

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

            if (!ViewportImageGeometryCalculator.TryCreate(AssociatedObject.Tile?.CurrentDicomFrame, host, out var geometry))
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

            if (!ViewportImageGeometryCalculator.TryCreate(AssociatedObject.Tile?.CurrentDicomFrame, host, out var geometry))
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