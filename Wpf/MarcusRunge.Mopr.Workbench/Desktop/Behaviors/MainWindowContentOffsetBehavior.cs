using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;

namespace MarcusRunge.Mopr.Workbench.Behaviors;

/// <summary>
/// Compensates for the invisible upper window-frame inset that Windows applies
/// to a maximized native window.
/// </summary>
public static partial class MainWindowContentOffsetBehavior
{
    /// <summary>
    /// Identifies the attached property that enables maximized content compensation.
    /// </summary>
    public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached("IsEnabled", typeof(bool), typeof(MainWindowContentOffsetBehavior), new PropertyMetadata(false, OnIsEnabledChanged));

    private const uint DefaultDpi = 96;
    private const int DwmwaExtendedFrameBounds = 9;
    private const int WmDpiChanged = 0x02E0;
    private const int WmWindowPositionChanged = 0x0047;
    private static readonly Dictionary<FrameworkElement, ContentOffsetState> States = [];

    /// <summary>
    /// Gets whether maximized content compensation is enabled.
    /// </summary>
    /// <param name="dependencyObject">The object from which to read the value.</param>
    /// <returns><see langword="true"/> when the behavior is enabled; otherwise, <see langword="false"/>.</returns>
    public static bool GetIsEnabled(DependencyObject dependencyObject) => (bool)dependencyObject.GetValue(IsEnabledProperty);

    /// <summary>
    /// Enables or disables maximized content compensation.
    /// </summary>
    /// <param name="dependencyObject">The object on which to set the value.</param>
    /// <param name="value"><see langword="true"/> to enable the behavior; otherwise, <see langword="false"/>.</param>
    public static void SetIsEnabled(DependencyObject dependencyObject, bool value) => dependencyObject.SetValue(IsEnabledProperty, value);

    private static void ApplyCurrentMargin(FrameworkElement element, ContentOffsetState state)
    {
        if (state.Window?.WindowState != WindowState.Maximized)
        {
            element.Margin = state.OriginalMargin;
            return;
        }

        if (!TryGetMaximizedTopMargin(state.WindowHandle, out var topMargin))
        {
            // A failed native measurement must never leave a stale compensation value.
            element.Margin = state.OriginalMargin;
            return;
        }

        var originalMargin = state.OriginalMargin;
        element.Margin = new Thickness(originalMargin.Left, originalMargin.Top + topMargin, originalMargin.Right, originalMargin.Bottom);
    }

    private static void Attach(FrameworkElement element)
    {
        if (States.ContainsKey(element))
        {
            return;
        }

        var state = new ContentOffsetState(element.Margin);
        States.Add(element, state);

        element.Loaded += OnElementLoaded;
        element.Unloaded += OnElementUnloaded;

        if (element.IsLoaded)
        {
            ConnectToWindow(element, state);
        }
    }

    private static void ConnectToWindow(FrameworkElement element, ContentOffsetState state)
    {
        var window = Window.GetWindow(element);
        if (window is null)
        {
            return;
        }

        if (ReferenceEquals(state.Window, window))
        {
            QueueMarginUpdate(element, state);
            return;
        }

        DisconnectFromWindow(state);

        var windowHandle = new WindowInteropHelper(window).Handle;
        if (windowHandle == nint.Zero || HwndSource.FromHwnd(windowHandle) is not HwndSource hwndSource)
        {
            return;
        }

        state.Window = window;
        state.WindowHandle = windowHandle;
        state.HwndSource = hwndSource;

        window.StateChanged += OnWindowStateChanged;
        hwndSource.AddHook(WindowProcedure);

        QueueMarginUpdate(element, state);
    }

    private static void Detach(FrameworkElement element)
    {
        element.Loaded -= OnElementLoaded;
        element.Unloaded -= OnElementUnloaded;

        if (!States.Remove(element, out var state))
        {
            return;
        }

        DisconnectFromWindow(state);
        element.Margin = state.OriginalMargin;
    }

    private static void DisconnectFromWindow(ContentOffsetState state)
    {
        state.Window?.StateChanged -= OnWindowStateChanged;

        state.HwndSource?.RemoveHook(WindowProcedure);

        state.Window = null;
        state.WindowHandle = nint.Zero;
        state.HwndSource = null;
    }

    [LibraryImport("dwmapi.dll")]
    private static partial int DwmGetWindowAttribute(nint windowHandle, int attribute, out NativeRectangle attributeValue, int attributeSize);

    [LibraryImport("user32.dll")]
    private static partial uint GetDpiForWindow(nint windowHandle);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetWindowRect(nint windowHandle, out NativeRectangle rectangle);

    private static void OnElementLoaded(object sender, RoutedEventArgs eventArgs)
    {
        if (sender is FrameworkElement element && States.TryGetValue(element, out var state))
        {
            ConnectToWindow(element, state);
        }
    }

    private static void OnElementUnloaded(object sender, RoutedEventArgs eventArgs)
    {
        if (sender is FrameworkElement element && States.TryGetValue(element, out var state))
        {
            DisconnectFromWindow(state);
            element.Margin = state.OriginalMargin;
        }
    }

    private static void OnIsEnabledChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs eventArgs)
    {
        if (dependencyObject is not FrameworkElement element)
        {
            throw new InvalidOperationException($"{nameof(MainWindowContentOffsetBehavior)} can only be attached to a {nameof(FrameworkElement)}.");
        }

        if ((bool)eventArgs.NewValue)
        {
            Attach(element);
            return;
        }

        Detach(element);
    }

    private static void OnWindowStateChanged(object? sender, EventArgs eventArgs)
    {
        if (sender is not Window window)
        {
            return;
        }

        foreach (var entry in States)
        {
            if (ReferenceEquals(entry.Value.Window, window))
            {
                QueueMarginUpdate(entry.Key, entry.Value);
            }
        }
    }

    private static void QueueMarginUpdate(FrameworkElement element, ContentOffsetState state)
    {
        if (state.UpdatePending)
        {
            return;
        }

        state.UpdatePending = true;

        // The native frame bounds are queried only after Windows and WPF have completed
        // the current state, position, or per-monitor DPI transition.
        _ = element.Dispatcher.BeginInvoke(
            DispatcherPriority.Loaded,
            () =>
            {
                state.UpdatePending = false;
                ApplyCurrentMargin(element, state);
            });
    }

    private static bool TryGetMaximizedTopMargin(nint windowHandle, out double topMargin)
    {
        topMargin = 0.0;

        if (windowHandle == nint.Zero || !GetWindowRect(windowHandle, out var windowRectangle))
        {
            return false;
        }

        var result = DwmGetWindowAttribute(windowHandle, DwmwaExtendedFrameBounds, out NativeRectangle extendedFrameRectangle, Marshal.SizeOf<NativeRectangle>());

        if (result < 0)
        {
            return false;
        }

        var dpi = GetDpiForWindow(windowHandle);
        if (dpi == 0)
        {
            return false;
        }

        // GetWindowRect includes the invisible resize border while the DWM extended
        // frame begins at the visible upper window edge. Their top-coordinate
        // difference is therefore the required physical-pixel compensation.
        var hiddenTopPixels = Math.Max(0, extendedFrameRectangle.Top - windowRectangle.Top);
        topMargin = hiddenTopPixels * DefaultDpi / (double)dpi;
        return double.IsFinite(topMargin);
    }

    private static nint WindowProcedure(nint windowHandle, int message, nint wordParameter, nint longParameter, ref bool handled)
    {
        if (message is not WmDpiChanged and not WmWindowPositionChanged)
        {
            return nint.Zero;
        }

        foreach (var entry in States)
        {
            if (entry.Value.WindowHandle == windowHandle)
            {
                QueueMarginUpdate(entry.Key, entry.Value);
            }
        }

        return nint.Zero;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRectangle
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    private sealed class ContentOffsetState(Thickness originalMargin)
    {
        public HwndSource? HwndSource { get; set; }
        public Thickness OriginalMargin { get; } = originalMargin;

        public bool UpdatePending { get; set; }
        public Window? Window { get; set; }

        public nint WindowHandle { get; set; }
    }
}