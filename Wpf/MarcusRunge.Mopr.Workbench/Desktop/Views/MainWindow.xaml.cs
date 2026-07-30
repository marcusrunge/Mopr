using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace MarcusRunge.Mopr.Workbench.Views
{
    public partial class MainWindow : Window
    {
        private const int SwRestore = 9;

        public MainWindow() => InitializeComponent();

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
        private static partial bool ShowWindowAsync(nint windowHandle, int command);

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool SetForegroundWindow(nint windowHandle);
    }
}