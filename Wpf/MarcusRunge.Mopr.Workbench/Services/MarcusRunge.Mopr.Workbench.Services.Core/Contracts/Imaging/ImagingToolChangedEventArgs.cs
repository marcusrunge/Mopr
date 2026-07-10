using MarcusRunge.Mopr.Workbench.Contracts.Enums;
using System;

namespace MarcusRunge.Mopr.Workbench.Services.Core.Contracts.Imaging
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