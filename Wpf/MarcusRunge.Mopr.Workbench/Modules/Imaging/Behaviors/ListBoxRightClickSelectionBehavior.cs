using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.Behaviors
{
    public static class ListBoxRightClickSelectionBehavior
    {
        public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached("IsEnabled", typeof(bool), typeof(ListBoxRightClickSelectionBehavior), new PropertyMetadata(false, OnIsEnabledChanged));

        public static bool GetIsEnabled(DependencyObject dependencyObject) => (bool)dependencyObject.GetValue(IsEnabledProperty);

        public static void SetIsEnabled(DependencyObject dependencyObject, bool value) => dependencyObject.SetValue(IsEnabledProperty, value);

        private static T? FindParent<T>(DependencyObject? dependencyObject) where T : DependencyObject
        {
            while (dependencyObject != null)
            {
                if (dependencyObject is T typedParent)
                {
                    return typedParent;
                }

                dependencyObject = VisualTreeHelper.GetParent(dependencyObject);
            }

            return null;
        }

        private static void OnIsEnabledChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            if (dependencyObject is not ListBox listBox)
            {
                return;
            }

            if ((bool)e.NewValue)
            {
                listBox.PreviewMouseRightButtonDown += OnPreviewMouseRightButtonDown;
                return;
            }

            listBox.PreviewMouseRightButtonDown -= OnPreviewMouseRightButtonDown;
        }

        private static void OnPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not ListBox listBox)
            {
                return;
            }

            var item = FindParent<ListBoxItem>(e.OriginalSource as DependencyObject);

            if (item == null)
            {
                return;
            }

            if (!item.IsSelected)
            {
                listBox.SelectedItems.Clear();
                item.IsSelected = true;
            }

            item.Focus();
        }
    }
}