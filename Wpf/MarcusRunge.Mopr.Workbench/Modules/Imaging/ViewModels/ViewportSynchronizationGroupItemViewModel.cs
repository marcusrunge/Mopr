using MarcusRunge.Mopr.Workbench.Modules.Imaging.Models;
using System;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.ViewModels
{
    public sealed class ViewportSynchronizationGroupItemViewModel(ViewportSynchronizationGroup group)
    {
        public ViewportSynchronizationGroup Group { get; } = group ?? throw new ArgumentNullException(nameof(group));
        public string DisplayText => $"{Group.Name} ({Group.ViewportIds.Count})";
        public Guid Id => Group.Id;

        public override string ToString() => DisplayText;
    }
}