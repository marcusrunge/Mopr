using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace MarcusRunge.Mopr.Workbench.Views;

/// <summary>
/// Hosts the application shell.
/// </summary>
public partial class MainWindow : Window
{
    private const int SwRestore = 9;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class.
    /// </summary>
    public MainWindow() => InitializeComponent();

    /// <summary>
    /// Restores and activates this window after another process attempted to start MOPR.
    /// </summary>
    public void ActivateFromSecondInstance()
    {
        if (WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
        }

        if (!IsVisible)
        {
            Show();
        }

        var windowHandle = new WindowInteropHelper(this).Handle;
        if (windowHandle != nint.Zero)
        {
            _ = ShowWindowAsync(windowHandle, SwRestore);
            _ = SetForegroundWindow(windowHandle);
        }

        _ = Activate();
        Topmost = true;
        Topmost = false;
        Focus();
    }

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetForegroundWindow(nint windowHandle);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool ShowWindowAsync(nint windowHandle, int command);
}