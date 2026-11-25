using MarcusRunge.Mopr.Workbench.Modules.Imaging.Infrastructure.Viewports;
using MarcusRunge.Mopr.Workbench.Modules.Imaging.ViewModels;
using MarcusRunge.Mopr.Workbench.Modules.Imaging.Views.Viewports;
using Microsoft.Xaml.Behaviors;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.Behaviors
{
    public sealed class ViewportImagePlacementBehavior : Behavior<Image>
    {
        private FrameworkElement? _host;
        private ViewportTileViewModel? _tile;

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.Loaded += OnLoaded;
            AssociatedObject.Unloaded += OnUnloaded;
        }

        protected override void OnDetaching()
        {
            DetachHost();
            DetachTile();
            AssociatedObject.Loaded -= OnLoaded;
            AssociatedObject.Unloaded -= OnUnloaded;
            base.OnDetaching();
        }

        private static T? FindVisualAncestor<T>(DependencyObject? dependencyObject) where T : DependencyObject
        {
            var current = dependencyObject;

            while (current != null)
            {
                if (current is T ancestor) return ancestor;
                current = VisualTreeHelper.GetParent(current);
            }

            return null;
        }

        private void AttachHost()
        {
            DetachHost();
            _host = AssociatedObject.Parent as FrameworkElement;
            if (_host == null) return;
            _host.SizeChanged += OnHostSizeChanged;
        }

        private void AttachTile()
        {
            DetachTile();
            _tile = FindVisualAncestor<ViewportTile>(AssociatedObject)?.Tile;
            if (_tile == null) return;
            _tile.PropertyChanged += OnTilePropertyChanged;
        }

        private void DetachHost()
        {
            _host?.SizeChanged -= OnHostSizeChanged;
            _host = null;
        }

        private void DetachTile()
        {
            _tile?.PropertyChanged -= OnTilePropertyChanged;
            _tile = null;
        }

        private void OnHostSizeChanged(object sender, SizeChangedEventArgs eventArgs) => UpdatePlacement();

        private void OnLoaded(object sender, RoutedEventArgs eventArgs)
        {
            AttachHost();
            AttachTile();
            UpdatePlacement();
        }

        private void OnTilePropertyChanged(object? sender, PropertyChangedEventArgs eventArgs)
        {
            if (string.Equals(eventArgs.PropertyName, nameof(ViewportTileViewModel.CurrentDicomFrame), StringComparison.Ordinal) || string.Equals(eventArgs.PropertyName, nameof(ViewportTileViewModel.ZoomFactor), StringComparison.Ordinal) || string.Equals(eventArgs.PropertyName, nameof(ViewportTileViewModel.PanOffsetX), StringComparison.Ordinal) || string.Equals(eventArgs.PropertyName, nameof(ViewportTileViewModel.PanOffsetY), StringComparison.Ordinal) || string.Equals(eventArgs.PropertyName, nameof(ViewportTileViewModel.Rotation), StringComparison.Ordinal) || string.Equals(eventArgs.PropertyName, nameof(ViewportTileViewModel.IsFlippedHorizontally), StringComparison.Ordinal) || string.Equals(eventArgs.PropertyName, nameof(ViewportTileViewModel.IsFlippedVertically), StringComparison.Ordinal)) UpdatePlacement();
        }

        private void OnUnloaded(object sender, RoutedEventArgs eventArgs)
        {
            DetachHost();
            DetachTile();
        }

        private void UpdatePlacement()
        {
            if (!ViewportImageGeometryCalculator.TryCreate(_tile?.CurrentDicomFrame, _host, _tile?.ZoomFactor ?? ViewportImageGeometryCalculator.DefaultZoomFactor, _tile?.PanOffsetX ?? 0.0, _tile?.PanOffsetY ?? 0.0, _tile?.Rotation ?? ViewportRotation.None, _tile?.IsFlippedHorizontally ?? false, _tile?.IsFlippedVertically ?? false, out var geometry))
            {
                AssociatedObject.Width = 0.0;
                AssociatedObject.Height = 0.0;
                AssociatedObject.Margin = new Thickness();
                AssociatedObject.RenderTransform = Transform.Identity;
                return;
            }

            var placement = geometry.Placement;
            AssociatedObject.Width = placement.ImageWidth;
            AssociatedObject.Height = placement.ImageHeight;
            AssociatedObject.Margin = new Thickness(placement.OffsetX, placement.OffsetY, 0.0, 0.0);
            AssociatedObject.RenderTransformOrigin = new Point(0.0, 0.0);
            AssociatedObject.RenderTransform = new MatrixTransform(placement.ImageTransform);
        }
    }
}