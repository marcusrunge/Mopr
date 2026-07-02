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
        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(nameof(Title), typeof(string), typeof(ViewportTile), new PropertyMetadata(string.Empty));
        public static readonly DependencyProperty ViewportIdProperty = DependencyProperty.Register(nameof(ViewportId), typeof(string), typeof(ViewportTile), new PropertyMetadata(string.Empty, OnViewportStateChanged));
        public static readonly DependencyProperty WindowLevelDragCommandProperty = DependencyProperty.Register(nameof(WindowLevelDragCommand), typeof(ICommand), typeof(ViewportTile), new PropertyMetadata(null));
        public static readonly DependencyProperty WindowLevelDragCompletedCommandProperty = DependencyProperty.Register(nameof(WindowLevelDragCompletedCommand), typeof(ICommand), typeof(ViewportTile), new PropertyMetadata(null));
        public static readonly DependencyProperty WindowLevelDragStartedCommandProperty = DependencyProperty.Register(nameof(WindowLevelDragStartedCommand), typeof(ICommand), typeof(ViewportTile), new PropertyMetadata(null));

        private bool _isWindowLevelDragging;
        private Point _windowLevelDragStartPosition;

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

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
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

        private void CompleteWindowLevelDrag()
        {
            _isWindowLevelDragging = false;

            if (!string.IsNullOrWhiteSpace(ViewportId) &&
                WindowLevelDragCompletedCommand?.CanExecute(ViewportId) == true)
            {
                WindowLevelDragCompletedCommand.Execute(ViewportId);
            }
        }

        private void ExecutePixelHover(int? pixelX, int? pixelY)
        {
            var hoverInfo = new ViewportPixelHoverInfo(ViewportId, pixelX, pixelY);

            if (PixelHoverCommand?.CanExecute(hoverInfo) == true)
            {
                PixelHoverCommand.Execute(hoverInfo);
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

        private void OnViewportMouseLeave(object sender, MouseEventArgs e) => ExecutePixelHover(null, null);

        private void OnViewportPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!IsInteractive)
            {
                e.Handled = true;
                return;
            }

            if (!string.IsNullOrWhiteSpace(ViewportId) && SelectViewportCommand?.CanExecute(ViewportId) == true)
            {
                SelectViewportCommand.Execute(ViewportId);
            }

            if (IsMeasureActive)
            {
                var pointInfo = TryCreateMeasurementPointInfo(e);

                if (pointInfo != null && MeasurementPointCommand?.CanExecute(pointInfo) == true)
                {
                    MeasurementPointCommand.Execute(pointInfo);
                    UpdateMeasurementOverlay();
                }

                e.Handled = true;
                return;
            }

            if (IsWindowLevelActive)
            {
                _isWindowLevelDragging = true;
                _windowLevelDragStartPosition = e.GetPosition(this);

                if (WindowLevelDragStartedCommand?.CanExecute(ViewportId) == true)
                {
                    WindowLevelDragStartedCommand.Execute(ViewportId);
                }

                e.Handled = true;
                return;
            }

            e.Handled = true;
        }

        private void OnViewportPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!_isWindowLevelDragging)
            {
                return;
            }

            CompleteWindowLevelDrag();

            e.Handled = true;
        }

        private void OnViewportPreviewMouseMove(object sender, MouseEventArgs e)
        {
            ReportPixelHover(e);
            if (!_isWindowLevelDragging)
            {
                return;
            }

            if (e.LeftButton != MouseButtonState.Pressed)
            {
                CompleteWindowLevelDrag();
                e.Handled = true;
                return;
            }

            var currentPosition = e.GetPosition(this);

            var totalDeltaX = currentPosition.X - _windowLevelDragStartPosition.X;
            var totalDeltaY = currentPosition.Y - _windowLevelDragStartPosition.Y;

            var dragInfo = new ViewportWindowLevelDragInfo(ViewportId, totalDeltaX, totalDeltaY);

            if (WindowLevelDragCommand?.CanExecute(dragInfo) == true)
            {
                WindowLevelDragCommand.Execute(dragInfo);
            }

            e.Handled = true;
        }

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

        private bool TryConvertImagePointToHostPosition(double imageX, double imageY, out Point position)
        {
            position = default;

            if (!TryGetImageDisplayGeometry(out var scale, out var offsetX, out var offsetY, out var frameWidth, out var frameHeight))
            {
                return false;
            }

            if (imageX < 0 || imageY < 0 || imageX >= frameWidth || imageY >= frameHeight)
            {
                return false;
            }

            position = new Point(offsetX + imageX * scale, offsetY + imageY * scale);

            return true;
        }

        private ViewportMeasurementPointInfo? TryCreateMeasurementPointInfo(MouseEventArgs e)
        {
            if (!TryGetImageDisplayGeometry(out var scale, out var offsetX, out var offsetY, out var frameWidth, out var frameHeight))
            {
                return null;
            }

            var position = e.GetPosition(ViewportImageHost);

            var displayedWidth = frameWidth * scale;
            var displayedHeight = frameHeight * scale;

            if (position.X < offsetX || position.Y < offsetY || position.X >= offsetX + displayedWidth || position.Y >= offsetY + displayedHeight)
            {
                return null;
            }

            var imageX = (position.X - offsetX) / scale;
            var imageY = (position.Y - offsetY) / scale;

            if (imageX < 0 || imageY < 0 || imageX >= frameWidth || imageY >= frameHeight)
            {
                return null;
            }

            return new ViewportMeasurementPointInfo(ViewportId, imageX, imageY);
        }

        private ViewportPixelHoverInfo? TryCreatePixelHoverInfo(MouseEventArgs e)
        {
            if (!TryGetImageDisplayGeometry(out var scale, out var offsetX, out var offsetY, out var frameWidth, out var frameHeight))
            {
                return null;
            }

            var position = e.GetPosition(ViewportImageHost);

            var displayedWidth = frameWidth * scale;
            var displayedHeight = frameHeight * scale;

            if (position.X < offsetX || position.Y < offsetY || position.X >= offsetX + displayedWidth || position.Y >= offsetY + displayedHeight)
            {
                return null;
            }

            var pixelX = (int)((position.X - offsetX) / scale);
            var pixelY = (int)((position.Y - offsetY) / scale);

            if (pixelX < 0 || pixelY < 0 || pixelX >= frameWidth || pixelY >= frameHeight)
            {
                return null;
            }

            return new ViewportPixelHoverInfo(ViewportId, pixelX, pixelY);
        }

        private bool TryGetImageDisplayGeometry(out double scale, out double offsetX, out double offsetY, out int frameWidth, out int frameHeight)
        {
            scale = 0;
            offsetX = 0;
            offsetY = 0;
            frameWidth = 0;
            frameHeight = 0;

            var frame = Tile?.CurrentDicomFrame;

            if (frame == null)
            {
                return false;
            }

            frameWidth = frame.Width;
            frameHeight = frame.Height;

            if (frameWidth <= 0 || frameHeight <= 0 || ViewportImageHost.ActualWidth <= 0 || ViewportImageHost.ActualHeight <= 0)
            {
                return false;
            }

            scale = Math.Min(ViewportImageHost.ActualWidth / frameWidth, ViewportImageHost.ActualHeight / frameHeight);

            if (scale <= 0)
            {
                return false;
            }

            var displayedWidth = frameWidth * scale;
            var displayedHeight = frameHeight * scale;

            offsetX = (ViewportImageHost.ActualWidth - displayedWidth) / 2.0;
            offsetY = (ViewportImageHost.ActualHeight - displayedHeight) / 2.0;

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