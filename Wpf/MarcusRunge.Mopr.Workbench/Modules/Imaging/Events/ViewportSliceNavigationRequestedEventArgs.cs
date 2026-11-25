using System;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.Events
{
    public sealed class ViewportSliceNavigationRequestedEventArgs(int delta) : EventArgs
    {
        public int Delta { get; } = delta;
    }
}