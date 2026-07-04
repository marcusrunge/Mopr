using MarcusRunge.Mopr.Workbench.Modules.Imaging.Infrastructure.Viewports;
using MarcusRunge.Mopr.Workbench.Modules.Imaging.ViewModels;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.Views.Viewports
{
    public partial class ViewportTile : UserControl
    {
        public static readonly DependencyProperty ActiveViewportIdProperty = DependencyProperty.Register(nameof(ActiveViewportId), typeof(string), typeof(ViewportTile), new PropertyMetadata(string.Empty, OnViewportStateChanged));
        public static readonly DependencyProperty ClearViewportCommandProperty = DependencyProperty.Register(nameof(ClearViewportCommand), typeof(ICommand), typeof(ViewportTile), new PropertyMetadata(null));
        public static readonly DependencyProperty IsActiveProperty = DependencyProperty.Register(nameof(IsActive), typeof(bool), typeof(ViewportTile), new PropertyMetadata(false));
        public static readonly DependencyProperty IsInteractiveProperty = DependencyProperty.Register(nameof(IsInteractive), typeof(bool), typeof(ViewportTile), new PropertyMetadata(true));
        public static readonly DependencyProperty IsMeasureActiveProperty = DependencyProperty.Register(nameof(IsMeasureActive), typeof(bool), typeof(ViewportTile), new PropertyMetadata(false));
        public static readonly DependencyProperty IsWindowLevelActiveProperty = DependencyProperty.Register(nameof(IsWindowLevelActive), typeof(bool), typeof(ViewportTile), new PropertyMetadata(false));
        public static readonly DependencyProperty MeasurementPointCommandProperty = DependencyProperty.Register(nameof(MeasurementPointCommand), typeof(ICommand), typeof(ViewportTile), new PropertyMetadata(null));
        public static readonly DependencyProperty PixelHoverCommandProperty = DependencyProperty.Register(nameof(PixelHoverCommand), typeof(ICommand), typeof(ViewportTile), new PropertyMetadata(null));
        public static readonly DependencyProperty SelectViewportCommandProperty = DependencyProperty.Register(nameof(SelectViewportCommand), typeof(ICommand), typeof(ViewportTile), new PropertyMetadata(null));
        public static readonly DependencyProperty TileProperty = DependencyProperty.Register(nameof(Tile), typeof(ViewportTileViewModel), typeof(ViewportTile), new PropertyMetadata(null, OnTileChanged));
        public static readonly DependencyProperty ViewportIdProperty = DependencyProperty.Register(nameof(ViewportId), typeof(string), typeof(ViewportTile), new PropertyMetadata(string.Empty, OnViewportStateChanged));
        public static readonly DependencyProperty WindowLevelDragCommandProperty = DependencyProperty.Register(nameof(WindowLevelDragCommand), typeof(ICommand), typeof(ViewportTile), new PropertyMetadata(null));
        public static readonly DependencyProperty WindowLevelDragCompletedCommandProperty = DependencyProperty.Register(nameof(WindowLevelDragCompletedCommand), typeof(ICommand), typeof(ViewportTile), new PropertyMetadata(null));
        public static readonly DependencyProperty WindowLevelDragStartedCommandProperty = DependencyProperty.Register(nameof(WindowLevelDragStartedCommand), typeof(ICommand), typeof(ViewportTile), new PropertyMetadata(null));

        public ViewportTile()
        {
            InitializeComponent();

            Loaded += OnViewportTileLoaded;
            Unloaded += OnViewportTileUnloaded;
        }

        public string ActiveViewportId
        {
            get => (string)GetValue(ActiveViewportIdProperty);
            set => SetValue(ActiveViewportIdProperty, value);
        }

        public ICommand? ClearViewportCommand
        {
            get => (ICommand?)GetValue(ClearViewportCommandProperty);
            set => SetValue(ClearViewportCommandProperty, value);
        }

        public bool IsActive
        {
            get => (bool)GetValue(IsActiveProperty);
            private set => SetValue(IsActiveProperty, value);
        }

        public bool IsInteractive
        {
            get => (bool)GetValue(IsInteractiveProperty);
            set => SetValue(IsInteractiveProperty, value);
        }

        public bool IsMeasureActive
        {
            get => (bool)GetValue(IsMeasureActiveProperty);
            set => SetValue(IsMeasureActiveProperty, value);
        }

        public bool IsWindowLevelActive
        {
            get => (bool)GetValue(IsWindowLevelActiveProperty);
            set => SetValue(IsWindowLevelActiveProperty, value);
        }

        public ICommand? MeasurementPointCommand
        {
            get => (ICommand?)GetValue(MeasurementPointCommandProperty);
            set => SetValue(MeasurementPointCommandProperty, value);
        }

        public ICommand? PixelHoverCommand
        {
            get => (ICommand?)GetValue(PixelHoverCommandProperty);
            set => SetValue(PixelHoverCommandProperty, value);
        }

        public ICommand? SelectViewportCommand
        {
            get => (ICommand?)GetValue(SelectViewportCommandProperty);
            set => SetValue(SelectViewportCommandProperty, value);
        }

        public ViewportTileViewModel? Tile
        {
            get => (ViewportTileViewModel?)GetValue(TileProperty);
            set => SetValue(TileProperty, value);
        }

        public string ViewportId
        {
            get => (string)GetValue(ViewportIdProperty);
            set => SetValue(ViewportIdProperty, value);
        }

        public ICommand? WindowLevelDragCommand
        {
            get => (ICommand?)GetValue(WindowLevelDragCommandProperty);
            set => SetValue(WindowLevelDragCommandProperty, value);
        }

        public ICommand? WindowLevelDragCompletedCommand
        {
            get => (ICommand?)GetValue(WindowLevelDragCompletedCommandProperty);
            set => SetValue(WindowLevelDragCompletedCommandProperty, value);
        }

        public ICommand? WindowLevelDragStartedCommand
        {
            get => (ICommand?)GetValue(WindowLevelDragStartedCommandProperty);
            set => SetValue(WindowLevelDragStartedCommandProperty, value);
        }

        private static void OnTileChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            if (dependencyObject is not ViewportTile viewportTile)
            {
                return;
            }

            if (e.OldValue is ViewportTileViewModel oldTile)
            {
                oldTile.PropertyChanged -= viewportTile.OnTilePropertyChanged;
            }

            if (e.NewValue is ViewportTileViewModel newTile)
            {
                newTile.PropertyChanged += viewportTile.OnTilePropertyChanged;
            }

            viewportTile.UpdateMeasurementOverlay();
        }

        private static void OnViewportStateChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            if (dependencyObject is ViewportTile tile)
            {
                tile.UpdateIsActive();
            }
        }

        private void OnTilePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (string.Equals(e.PropertyName, nameof(ViewportTileViewModel.MeasurementDisplayText), StringComparison.Ordinal) || string.Equals(e.PropertyName, nameof(ViewportTileViewModel.MeasurementOverlayLabelText), StringComparison.Ordinal) || string.Equals(e.PropertyName, nameof(ViewportTileViewModel.CurrentImage), StringComparison.Ordinal) || string.Equals(e.PropertyName, nameof(ViewportTileViewModel.CurrentDicomFrame), StringComparison.Ordinal) || string.Equals(e.PropertyName, nameof(ViewportTileViewModel.CurrentFilePath), StringComparison.Ordinal))
            {
                UpdateMeasurementOverlay();
            }
        }

        private void OnViewportImageSizeChanged(object sender, SizeChangedEventArgs e) => UpdateMeasurementOverlay();

        private void OnViewportTileLoaded(object sender, RoutedEventArgs e)
        {
            if (Tile != null)
            {
                Tile.PropertyChanged -= OnTilePropertyChanged;
                Tile.PropertyChanged += OnTilePropertyChanged;
            }

            UpdateMeasurementOverlay();
        }

        private void OnViewportTileUnloaded(object sender, RoutedEventArgs e) => Tile?.PropertyChanged -= OnTilePropertyChanged;

        private bool TryConvertImagePointToHostPosition(double imageX, double imageY, out Point position)
        {
            position = default;

            if (!ViewportImageGeometryCalculator.TryCreate(Tile?.CurrentDicomFrame, ViewportImageHost, out var geometry))
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

        private void UpdateIsActive() => IsActive = !string.IsNullOrWhiteSpace(ViewportId) && string.Equals(ViewportId, ActiveViewportId, StringComparison.Ordinal);

        private void UpdateMeasurementOverlay()
        {
            if (Tile?.MeasurementState == null || !Tile.MeasurementState.HasFirstPoint)
            {
                MeasurementOverlayCanvas.Visibility = Visibility.Collapsed;
                return;
            }

            var firstX = Tile.MeasurementState.FirstPointX!.Value;
            var firstY = Tile.MeasurementState.FirstPointY!.Value;

            if (!TryConvertImagePointToHostPosition(firstX, firstY, out var firstPoint))
            {
                MeasurementOverlayCanvas.Visibility = Visibility.Collapsed;
                return;
            }

            MeasurementOverlayCanvas.Visibility = Visibility.Visible;

            Canvas.SetLeft(MeasurementFirstPoint, firstPoint.X);
            Canvas.SetTop(MeasurementFirstPoint, firstPoint.Y);

            MeasurementFirstPoint.Visibility = Visibility.Visible;

            if (!Tile.MeasurementState.HasSecondPoint)
            {
                MeasurementLine.Visibility = Visibility.Collapsed;
                MeasurementSecondPoint.Visibility = Visibility.Collapsed;
                MeasurementLabelBorder.Visibility = Visibility.Collapsed;
                return;
            }

            var secondX = Tile.MeasurementState.SecondPointX!.Value;
            var secondY = Tile.MeasurementState.SecondPointY!.Value;

            if (!TryConvertImagePointToHostPosition(secondX, secondY, out var secondPoint))
            {
                MeasurementLine.Visibility = Visibility.Collapsed;
                MeasurementSecondPoint.Visibility = Visibility.Collapsed;
                MeasurementLabelBorder.Visibility = Visibility.Collapsed;
                return;
            }

            MeasurementSecondPoint.Visibility = Visibility.Visible;

            Canvas.SetLeft(MeasurementSecondPoint, secondPoint.X);
            Canvas.SetTop(MeasurementSecondPoint, secondPoint.Y);

            MeasurementLine.Visibility = Visibility.Visible;
            MeasurementLine.X1 = firstPoint.X;
            MeasurementLine.Y1 = firstPoint.Y;
            MeasurementLine.X2 = secondPoint.X;
            MeasurementLine.Y2 = secondPoint.Y;

            MeasurementLabelTextBlock.Text = Tile.MeasurementOverlayLabelText;

            MeasurementLabelBorder.Visibility = string.IsNullOrWhiteSpace(Tile.MeasurementOverlayLabelText) ? Visibility.Collapsed : Visibility.Visible;

            var labelX = (firstPoint.X + secondPoint.X) / 2.0 + 8.0;
            var labelY = (firstPoint.Y + secondPoint.Y) / 2.0 - 22.0;

            Canvas.SetLeft(MeasurementLabelBorder, Math.Max(0, labelX));

            Canvas.SetTop(MeasurementLabelBorder, Math.Max(0, labelY));
        }
    }
}