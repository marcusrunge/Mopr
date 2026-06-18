using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.Views.Viewports
{
    public partial class ViewportTile : UserControl
    {
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(
                nameof(Title),
                typeof(string),
                typeof(ViewportTile),
                new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty ViewportIdProperty =
            DependencyProperty.Register(
                nameof(ViewportId),
                typeof(string),
                typeof(ViewportTile),
                new PropertyMetadata(string.Empty, OnViewportStateChanged));

        public static readonly DependencyProperty ActiveViewportIdProperty =
            DependencyProperty.Register(
                nameof(ActiveViewportId),
                typeof(string),
                typeof(ViewportTile),
                new PropertyMetadata(string.Empty, OnViewportStateChanged));

        public static readonly DependencyProperty IsActiveProperty =
            DependencyProperty.Register(
                nameof(IsActive),
                typeof(bool),
                typeof(ViewportTile),
                new PropertyMetadata(false));

        public static readonly DependencyProperty SelectViewportCommandProperty =
            DependencyProperty.Register(
                nameof(SelectViewportCommand),
                typeof(ICommand),
                typeof(ViewportTile),
                new PropertyMetadata(null));

        public ViewportTile()
        {
            InitializeComponent();
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

        public string ActiveViewportId
        {
            get => (string)GetValue(ActiveViewportIdProperty);
            set => SetValue(ActiveViewportIdProperty, value);
        }

        public bool IsActive
        {
            get => (bool)GetValue(IsActiveProperty);
            private set => SetValue(IsActiveProperty, value);
        }

        public ICommand? SelectViewportCommand
        {
            get => (ICommand?)GetValue(SelectViewportCommandProperty);
            set => SetValue(SelectViewportCommandProperty, value);
        }

        private static void OnViewportStateChanged(
            DependencyObject dependencyObject,
            DependencyPropertyChangedEventArgs e)
        {
            if (dependencyObject is ViewportTile tile)
            {
                tile.UpdateIsActive();
            }
        }

        private void UpdateIsActive()
        {
            IsActive =
                !string.IsNullOrWhiteSpace(ViewportId) &&
                string.Equals(ViewportId, ActiveViewportId, StringComparison.Ordinal);
        }

        private void OnViewportPreviewMouseLeftButtonDown(
            object sender,
            MouseButtonEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(ViewportId) &&
                SelectViewportCommand?.CanExecute(ViewportId) == true)
            {
                SelectViewportCommand.Execute(ViewportId);
            }

            e.Handled = true;
        }
    }
}
