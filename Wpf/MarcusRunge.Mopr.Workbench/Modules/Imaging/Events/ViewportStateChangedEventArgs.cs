using MarcusRunge.Mopr.Workbench.Modules.Imaging.Models;
using System;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.Events
{
    /// <summary>Provides one completed, atomic viewport state change.</summary>
    public sealed class ViewportStateChangedEventArgs(ViewportStateChange change) : EventArgs
    {
        public ViewportStateChange Change { get; } = change ?? throw new ArgumentNullException(nameof(change));
    }
}
