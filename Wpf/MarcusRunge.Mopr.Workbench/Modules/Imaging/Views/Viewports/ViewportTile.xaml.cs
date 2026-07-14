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
        public static readonly DependencyProperty IsMeasureActiveProperty = DependencyProperty.Register(nameof(IsMeasureActive), typeof(bool), typeof(ViewportTile), new PropertyMetadata(false));
        public static readonly DependencyProperty IsWindowLevelActiveProperty = DependencyProperty.Register(nameof(IsWindowLevelActive), typeof(bool), typeof(ViewportTile), new PropertyMetadata(false));
        public static readonly DependencyProperty MeasurementPointCommandProperty = DependencyProperty.Register(nameof(MeasurementPointCommand), typeof(ICommand), typeof(ViewportTile), new PropertyMetadata(null));
        public static readonly DependencyProperty MeasurementPreviewCommandProperty = DependencyProperty.Register(nameof(MeasurementPreviewCommand), typeof(ICommand), typeof(ViewportTile), new PropertyMetadata(null));
        public static readonly DependencyProperty PixelHoverCommandProperty = DependencyProperty.Register(nameof(PixelHoverCommand), typeof(ICommand), typeof(ViewportTile), new PropertyMetadata(null));
        public static readonly DependencyProperty SelectViewportCommandProperty = DependencyProperty.Register(nameof(SelectViewportCommand), typeof(ICommand), typeof(ViewportTile), new PropertyMetadata(null));
        public static readonly DependencyProperty TileProperty = DependencyProperty.Register(nameof(Tile), typeof(ViewportTileViewModel), typeof(ViewportTile), new PropertyMetadata(null));
        public static readonly DependencyProperty ViewportIdProperty = DependencyProperty.Register(nameof(ViewportId), typeof(string), typeof(ViewportTile), new PropertyMetadata(string.Empty, OnViewportStateChanged));
        public static readonly DependencyProperty WindowLevelDragCommandProperty = DependencyProperty.Register(nameof(WindowLevelDragCommand), typeof(ICommand), typeof(ViewportTile), new PropertyMetadata(null));
        public static readonly DependencyProperty WindowLevelDragCompletedCommandProperty = DependencyProperty.Register(nameof(WindowLevelDragCompletedCommand), typeof(ICommand), typeof(ViewportTile), new PropertyMetadata(null));
        public static readonly DependencyProperty WindowLevelDragStartedCommandProperty = DependencyProperty.Register(nameof(WindowLevelDragStartedCommand), typeof(ICommand), typeof(ViewportTile), new PropertyMetadata(null));

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

        public ICommand? MeasurementPreviewCommand
        {
            get => (ICommand?)GetValue(MeasurementPreviewCommandProperty);
            set => SetValue(MeasurementPreviewCommandProperty, value);
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

        private static void OnViewportStateChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            if (dependencyObject is ViewportTile tile)
            {
                tile.UpdateIsActive();
            }
        }

        private void UpdateIsActive() => IsActive = !string.IsNullOrWhiteSpace(ViewportId) && string.Equals(ViewportId, ActiveViewportId, StringComparison.Ordinal);
    }
}