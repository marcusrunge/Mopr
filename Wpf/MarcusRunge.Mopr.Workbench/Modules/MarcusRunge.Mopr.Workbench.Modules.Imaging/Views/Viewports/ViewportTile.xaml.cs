using MarcusRunge.Mopr.Workbench.Modules.Imaging.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

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
        public static readonly DependencyProperty TileProperty = DependencyProperty.Register(nameof(Tile), typeof(ViewportTileViewModel), typeof(ViewportTile), new PropertyMetadata(null));
        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(nameof(Title), typeof(string), typeof(ViewportTile), new PropertyMetadata(string.Empty));
        public static readonly DependencyProperty ViewportIdProperty = DependencyProperty.Register(nameof(ViewportId), typeof(string), typeof(ViewportTile), new PropertyMetadata(string.Empty, OnViewportStateChanged));
        public static readonly DependencyProperty WindowLevelDragCommandProperty = DependencyProperty.Register(nameof(WindowLevelDragCommand), typeof(ICommand), typeof(ViewportTile), new PropertyMetadata(null));
        public static readonly DependencyProperty WindowLevelDragCompletedCommandProperty = DependencyProperty.Register(nameof(WindowLevelDragCompletedCommand), typeof(ICommand), typeof(ViewportTile), new PropertyMetadata(null));
        public static readonly DependencyProperty WindowLevelDragStartedCommandProperty = DependencyProperty.Register(nameof(WindowLevelDragStartedCommand), typeof(ICommand), typeof(ViewportTile), new PropertyMetadata(null));

        private bool _isWindowLevelDragging;
        private Point _windowLevelDragStartPosition;

        public ViewportTile() => InitializeComponent();

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

        private ViewportPixelHoverInfo? TryCreatePixelHoverInfo(MouseEventArgs e)
        {
            if (Tile?.CurrentDicomFrame == null)
            {
                return null;
            }

            if (ViewportImage.Source == null)
            {
                return null;
            }

            var source = ViewportImage.Source;

            var sourceWidth = source is BitmapSource bitmapSource
                ? bitmapSource.PixelWidth
                : source.Width;

            var sourceHeight = source is BitmapSource bitmapSource2
                ? bitmapSource2.PixelHeight
                : source.Height;

            if (sourceWidth <= 0 ||
                sourceHeight <= 0 ||
                ViewportImage.ActualWidth <= 0 ||
                ViewportImage.ActualHeight <= 0)
            {
                return null;
            }

            var scale = Math.Min(
                ViewportImage.ActualWidth / sourceWidth,
                ViewportImage.ActualHeight / sourceHeight);

            if (scale <= 0)
            {
                return null;
            }

            var displayedWidth = sourceWidth * scale;
            var displayedHeight = sourceHeight * scale;

            var offsetX = (ViewportImage.ActualWidth - displayedWidth) / 2.0;
            var offsetY = (ViewportImage.ActualHeight - displayedHeight) / 2.0;

            var position = e.GetPosition(ViewportImage);

            if (position.X < offsetX ||
                position.Y < offsetY ||
                position.X >= offsetX + displayedWidth ||
                position.Y >= offsetY + displayedHeight)
            {
                return null;
            }

            var pixelX = (int)((position.X - offsetX) / scale);
            var pixelY = (int)((position.Y - offsetY) / scale);

            return new ViewportPixelHoverInfo(
                ViewportId,
                pixelX,
                pixelY);
        }

        private void ExecutePixelHover(int? pixelX, int? pixelY)
        {
            var hoverInfo = new ViewportPixelHoverInfo(ViewportId, pixelX, pixelY);

            if (PixelHoverCommand?.CanExecute(hoverInfo) == true)
            {
                PixelHoverCommand.Execute(hoverInfo);
            }
        }

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
                var hoverInfo = TryCreatePixelHoverInfo(e);

                if (hoverInfo != null &&
                    MeasurementPointCommand?.CanExecute(hoverInfo) == true)
                {
                    MeasurementPointCommand.Execute(hoverInfo);
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

        private void UpdateIsActive() => IsActive = !string.IsNullOrWhiteSpace(ViewportId) && string.Equals(ViewportId, ActiveViewportId, StringComparison.Ordinal);
    }
}