using System.Runtime.InteropServices;

namespace MarcusRunge.Mopr.Workbench.Application.SingleInstance
{
    internal interface IForegroundPermission
    {
        void AllowPrimaryInstance(int processId);
    }

    internal sealed partial class ForegroundPermission : IForegroundPermission
    {
        public void AllowPrimaryInstance(int processId) => _ = AllowSetForegroundWindow(processId);

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool AllowSetForegroundWindow(int processId);
    }
}