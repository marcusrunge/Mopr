using System.Windows;
using System.Windows.Input;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.Behaviors
{
    public static class FocusOnMouseDownBehavior
    {
        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached("IsEnabled", typeof(bool), typeof(FocusOnMouseDownBehavior), new PropertyMetadata(false, OnIsEnabledChanged));

        public static bool GetIsEnabled(DependencyObject obj) => (bool)obj.GetValue(IsEnabledProperty);

        public static void SetIsEnabled(DependencyObject obj, bool value) => obj.SetValue(IsEnabledProperty, value);

        private static void OnIsEnabledChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            if (dependencyObject is not UIElement element)
            {
                return;
            }

            if ((bool)e.NewValue)
            {
                element.PreviewMouseDown += OnPreviewMouseDown;
            }
            else
            {
                element.PreviewMouseDown -= OnPreviewMouseDown;
            }
        }

        private static void OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not UIElement element)
            {
                return;
            }

            if (element.Focusable == false)
            {
                element.Focusable = true;
            }

            element.Focus();
            Keyboard.Focus(element);
        }
    }
}