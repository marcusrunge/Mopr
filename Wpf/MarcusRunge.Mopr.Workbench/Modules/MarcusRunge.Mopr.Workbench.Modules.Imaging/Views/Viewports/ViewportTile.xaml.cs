using MarcusRunge.Mopr.Workbench.Modules.Imaging.ViewModels;
using System;
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
        public static readonly DependencyProperty IsWindowLevelActiveProperty = DependencyProperty.Register(nameof(IsWindowLevelActive), typeof(bool), typeof(ViewportTile), new PropertyMetadata(false));
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

        public bool IsWindowLevelActive
        {
            get => (bool)GetValue(IsWindowLevelActiveProperty);
            set => SetValue(IsWindowLevelActiveProperty, value);
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

        private void OnViewportPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!IsInteractive)
            {
                e.Handled = true;
                return;
            }

            if (!string.IsNullOrWhiteSpace(ViewportId) &&
                SelectViewportCommand?.CanExecute(ViewportId) == true)
            {
                SelectViewportCommand.Execute(ViewportId);
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

        private void UpdateIsActive() => IsActive = !string.IsNullOrWhiteSpace(ViewportId) && string.Equals(ViewportId, ActiveViewportId, StringComparison.Ordinal);
    }
}