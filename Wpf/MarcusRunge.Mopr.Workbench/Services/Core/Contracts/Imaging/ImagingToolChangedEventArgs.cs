using MarcusRunge.Mopr.Workbench.Contracts.Enums;
using System;

namespace MarcusRunge.Mopr.Workbench.Services.Core.Contracts.Imaging
{
    public sealed class ImagingToolChangedEventArgs(ImagingTool oldTool, ImagingTool newTool) : EventArgs
    {
        public ImagingTool OldTool { get; } = oldTool;

        public ImagingTool NewTool { get; } = newTool;
    }
}