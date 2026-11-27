using MarcusRunge.Mopr.Workbench.Modules.Imaging.Models;
using System;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.ViewModels
{
    public sealed class ViewportSynchronizationGroupItemViewModel(ViewportSynchronizationGroup group)
    {
        public ViewportSynchronizationGroup Group { get; } = group;
        public string DisplayText => $"{group.Name} ({group.ViewportIds.Count})";
        public Guid Id => group.Id;
        public override string ToString() => DisplayText;
    }
}
