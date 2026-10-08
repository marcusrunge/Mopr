using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.Views
{
    public partial class ImagingWorkbenchView : UserControl
    {
        public ImagingWorkbenchView() => InitializeComponent();

        private void OpenApplicationMenu(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { ContextMenu: { } contextMenu } button)
            {
                return;
            }

            contextMenu.PlacementTarget = button;
            contextMenu.Placement = PlacementMode.Bottom;
            contextMenu.HorizontalOffset = -220;
            contextMenu.IsOpen = true;
        }
    }
}