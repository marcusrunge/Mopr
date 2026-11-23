using MarcusRunge.Mopr.Workbench.Contracts;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Threading;

namespace MarcusRunge.Mopr.Workbench.Behaviors
{
    /// <summary>
    /// Defers a window close until its view model has resolved pending application work.
    /// </summary>
    public static class WindowClosingGuardBehavior
    {
        private static readonly Dictionary<Window, ClosingState> States = [];

        public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(WindowClosingGuardBehavior),
            new PropertyMetadata(false, OnIsEnabledChanged));

        public static bool GetIsEnabled(DependencyObject dependencyObject) => (bool)dependencyObject.GetValue(IsEnabledProperty);

        public static void SetIsEnabled(DependencyObject dependencyObject, bool value) => dependencyObject.SetValue(IsEnabledProperty, value);

        private static async void OnClosing(object? sender, CancelEventArgs e)
        {
            if (sender is not Window window || !States.TryGetValue(window, out ClosingState? state) || state.IsApproved) return;

            // The first close request must finish before WPF allows another Close call.
            // Cancel it immediately and resolve pending work asynchronously.
            e.Cancel = true;
            if (state.IsResolving || window.DataContext is not IWindowClosingGuard guard) return;

            state.IsResolving = true;
            try
            {
                if (!await guard.CanCloseWindowAsync().ConfigureAwait(true)) return;

                state.IsApproved = true;

                // Queue the approved close for the next Dispatcher turn. Calling
                // Window.Close directly from the active Closing event violates
                // WPF's Window.VerifyNotClosing invariant.
                _ = window.Dispatcher.BeginInvoke(
                    DispatcherPriority.Normal,
                    new Action(() =>
                    {
                        if (window.IsLoaded) window.Close();
                    }));
            }
            finally
            {
                state.IsResolving = false;
            }
        }

        private static void OnIsEnabledChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            if (dependencyObject is not Window window) return;

            if (e.NewValue is true)
            {
                if (States.ContainsKey(window)) return;
                States.Add(window, new ClosingState());
                window.Closing += OnClosing;
                window.Closed += OnClosed;
                return;
            }

            window.Closing -= OnClosing;
            window.Closed -= OnClosed;
            States.Remove(window);
        }

        private static void OnClosed(object? sender, EventArgs e)
        {
            if (sender is not Window window) return;
            window.Closing -= OnClosing;
            window.Closed -= OnClosed;
            States.Remove(window);
        }

        private sealed class ClosingState
        {
            public bool IsApproved { get; set; }
            public bool IsResolving { get; set; }
        }
    }
}