using MarcusRunge.Mopr.Workbench.Modules.Imaging.Infrastructure.Viewports;
using MarcusRunge.Mopr.Workbench.Modules.Imaging.ViewModels;
using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.Views.Viewports
{
    public sealed class MeasurementOverlayControl : Canvas
    {
        private ViewportMeasurementViewModel? _attachedActiveDraft;
        public static readonly DependencyProperty TileProperty = DependencyProperty.Register(nameof(Tile), typeof(ViewportTileViewModel), typeof(MeasurementOverlayControl), new PropertyMetadata(null, OnTileChanged));

        public MeasurementOverlayControl()
        {
            IsHitTestVisible = false;
            Visibility = Visibility.Visible;

            SizeChanged += OnSizeChanged;
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }
        private void AttachActiveDraft(ViewportMeasurementViewModel? draft)
        {
            if (ReferenceEquals(_attachedActiveDraft, draft))
            {
                return;
            }

            DetachActiveDraft();

            _attachedActiveDraft = draft;

            if (_attachedActiveDraft != null)
            {
                _attachedActiveDraft.PropertyChanged -= OnMeasurementPropertyChanged;
                _attachedActiveDraft.PropertyChanged += OnMeasurementPropertyChanged;
            }
        }

        private void DetachActiveDraft()
        {
            _attachedActiveDraft?.PropertyChanged -= OnMeasurementPropertyChanged;
            _attachedActiveDraft = null;
        }
        public ViewportTileViewModel? Tile
        {
            get => (ViewportTileViewModel?)GetValue(TileProperty);
            set => SetValue(TileProperty, value);
        }

        private static void OnTileChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            if (dependencyObject is not MeasurementOverlayControl overlay)
            {
                return;
            }

            overlay.DetachTile(e.OldValue as ViewportTileViewModel);
            overlay.AttachTile(e.NewValue as ViewportTileViewModel);
            overlay.UpdateOverlay();
        }

        private void AttachTile(ViewportTileViewModel? tile)
        {
            if (tile == null)
            {
                return;
            }

            tile.PropertyChanged -= OnTilePropertyChanged;
            tile.PropertyChanged += OnTilePropertyChanged;

            tile.Measurements.CollectionChanged -= OnMeasurementsCollectionChanged;
            tile.Measurements.CollectionChanged += OnMeasurementsCollectionChanged;

            foreach (var measurement in tile.Measurements)
            {
                measurement.PropertyChanged -= OnMeasurementPropertyChanged;
                measurement.PropertyChanged += OnMeasurementPropertyChanged;
            }
            AttachActiveDraft(tile.ActiveMeasurementDraft);
        }

        private void DetachTile(ViewportTileViewModel? tile)
        {
            if (tile == null)
            {
                return;
            }

            tile.PropertyChanged -= OnTilePropertyChanged;
            tile.Measurements.CollectionChanged -= OnMeasurementsCollectionChanged;

            foreach (var measurement in tile.Measurements)
            {
                measurement.PropertyChanged -= OnMeasurementPropertyChanged;
            }

            DetachActiveDraft();
        }

        private void DrawLabel(string text, Point startPoint, Point endPoint)
        {
            var textBlock = new TextBlock
            {
                Text = text,
                Foreground = new SolidColorBrush(Color.FromRgb(0xDD, 0xE5, 0xEC)),
                FontSize = 11,
                FontWeight = FontWeights.SemiBold
            };

            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(0xCC, 0x10, 0x16, 0x1C)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x60, 0xE1, 0xEB)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(5, 2, 5, 2),
                Child = textBlock
            };

            var labelX = (startPoint.X + endPoint.X) / 2.0 + 8.0;
            var labelY = (startPoint.Y + endPoint.Y) / 2.0 - 22.0;

            SetLeft(border, Math.Max(0, labelX));
            SetTop(border, Math.Max(0, labelY));

            Children.Add(border);
        }
        private void DrawLine(Point startPoint, Point endPoint, bool isDraft, bool isSelected)
        {
            var line = new Line
            {
                X1 = startPoint.X,
                Y1 = startPoint.Y,
                X2 = endPoint.X,
                Y2 = endPoint.Y,
                Stroke = new SolidColorBrush(isSelected ? Color.FromRgb(0x9A, 0xF7, 0xFF) : Color.FromRgb(0x60, 0xE1, 0xEB)),
                StrokeThickness = isSelected ? 2.5 : 1.5,
                SnapsToDevicePixels = true
            };

            if (isDraft)
            {
                line.StrokeDashArray = [4, 3];
            }

            Children.Add(line);
        }
        private void DrawMarker(Point point, bool isSelected)
        {
            var size = isSelected ? 10.0 : 8.0;
            var halfSize = size / 2.0;

            var marker = new Ellipse
            {
                Width = size,
                Height = size,
                Stroke = new SolidColorBrush(isSelected ? Color.FromRgb(0x9A, 0xF7, 0xFF) : Color.FromRgb(0x60, 0xE1, 0xEB)),
                StrokeThickness = isSelected ? 2.0 : 1.5,
                Fill = new SolidColorBrush(Color.FromArgb(0x26, 0x00, 0xE5, 0xFF)),
                RenderTransformOrigin = new Point(0.5, 0.5),
                RenderTransform = new TranslateTransform(-halfSize, -halfSize)
            };

            SetLeft(marker, point.X);
            SetTop(marker, point.Y);

            Children.Add(marker);
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            AttachTile(Tile);
            UpdateOverlay();
        }

        private void OnMeasurementPropertyChanged(object? sender, PropertyChangedEventArgs e) => UpdateOverlay();

        private void OnMeasurementsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (ViewportMeasurementViewModel measurement in e.OldItems)
                {
                    measurement.PropertyChanged -= OnMeasurementPropertyChanged;
                }
            }

            if (e.NewItems != null)
            {
                foreach (ViewportMeasurementViewModel measurement in e.NewItems)
                {
                    measurement.PropertyChanged -= OnMeasurementPropertyChanged;
                    measurement.PropertyChanged += OnMeasurementPropertyChanged;
                }
            }

            UpdateOverlay();
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e) => UpdateOverlay();

        private void OnTilePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (string.Equals(e.PropertyName, nameof(ViewportTileViewModel.ActiveMeasurementDraft), StringComparison.Ordinal))
            {
                AttachActiveDraft(Tile?.ActiveMeasurementDraft);
                UpdateOverlay();
                return;
            }

            if (string.Equals(e.PropertyName, nameof(ViewportTileViewModel.SelectedMeasurement), StringComparison.Ordinal) || string.Equals(e.PropertyName, nameof(ViewportTileViewModel.CurrentImage), StringComparison.Ordinal) || string.Equals(e.PropertyName, nameof(ViewportTileViewModel.CurrentDicomFrame), StringComparison.Ordinal) || string.Equals(e.PropertyName, nameof(ViewportTileViewModel.CurrentFilePath), StringComparison.Ordinal) || string.Equals(e.PropertyName, nameof(ViewportTileViewModel.Measurements), StringComparison.Ordinal))
            {
                UpdateOverlay();
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e) => DetachTile(Tile);

        private bool TryConvertImagePointToHostPosition(double imageX, double imageY, out Point position)
        {
            position = default;

            if (!ViewportImageGeometryCalculator.TryCreate(Tile?.CurrentDicomFrame, this, out var geometry))
            {
                return false;
            }

            if (!geometry.ContainsImagePoint(imageX, imageY))
            {
                return false;
            }

            position = geometry.ImagePointToHostPoint(imageX, imageY);

            return true;
        }

        private bool TryDrawCompletedMeasurement(
            ViewportMeasurementViewModel measurement)
        {
            if (!measurement.EndImageX.HasValue || !measurement.EndImageY.HasValue)
            {
                return false;
            }

            if (!TryConvertImagePointToHostPosition(measurement.StartImageX, measurement.StartImageY, out var startPoint))
            {
                return false;
            }

            if (!TryConvertImagePointToHostPosition(measurement.EndImageX.Value, measurement.EndImageY.Value, out var endPoint))
            {
                return false;
            }

            DrawLine(startPoint, endPoint, isDraft: false, isSelected: measurement.IsSelected);

            DrawMarker(startPoint, measurement.IsSelected);

            DrawMarker(endPoint, measurement.IsSelected);

            if (!string.IsNullOrWhiteSpace(measurement.LabelText))
            {
                DrawLabel(measurement.LabelText, startPoint, endPoint);
            }

            return true;
        }

        private bool TryDrawDraftMeasurement(ViewportMeasurementViewModel measurement)
        {
            if (!TryConvertImagePointToHostPosition(measurement.StartImageX, measurement.StartImageY, out var startPoint))
            {
                return false;
            }

            DrawMarker(startPoint, isSelected: false);

            if (!measurement.PreviewEndImageX.HasValue || !measurement.PreviewEndImageY.HasValue)
            {
                return true;
            }

            if (!TryConvertImagePointToHostPosition(measurement.PreviewEndImageX.Value, measurement.PreviewEndImageY.Value, out var previewEndPoint))
            {
                return true;
            }

            DrawLine(startPoint, previewEndPoint, isDraft: true, isSelected: false);

            DrawMarker(previewEndPoint, isSelected: false);

            if (!string.IsNullOrWhiteSpace(measurement.PreviewLabelText))
            {
                DrawLabel(measurement.PreviewLabelText, startPoint, previewEndPoint);
            }

            return true;
        }

        private void UpdateOverlay()
        {
            Children.Clear();

            Visibility = Visibility.Visible;

            if (Tile?.CurrentDicomFrame == null)
            {
                return;
            }

            foreach (var measurement in Tile.Measurements)
            {
                if (measurement.IsComplete)
                {
                    TryDrawCompletedMeasurement(measurement);
                }
            }

            if (Tile.ActiveMeasurementDraft != null)
            {
                TryDrawDraftMeasurement(Tile.ActiveMeasurementDraft);
            }
        }
    }
}