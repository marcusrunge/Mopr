using MarcusRunge.Mopr.Workbench.Contracts.Imaging;
using System;

namespace MarcusRunge.Mopr.Workbench.Services.Interfaces.Imaging
{
    public sealed class ImagingToolChangedEventArgs : EventArgs
    {
        public ImagingToolChangedEventArgs(ImagingTool oldTool, ImagingTool newTool)
        {
            OldTool = oldTool;
            NewTool = newTool;
        }

        public ImagingTool OldTool { get; }

        public ImagingTool NewTool { get; }
    }
}