using MarcusRunge.Mopr.Workbench.Modules.Imaging.ViewModels;
using System.Windows.Input;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.Behaviors
{
    internal static class ViewportSliceNavigation
    {
        internal static bool CanNavigate(ViewportTileViewModel? tile) => tile?.Series != null && tile.SeriesFiles.Count > 0 && tile.SliceCount > 0;

        internal static void MoveByWheelDelta(ViewportTileViewModel tile, int wheelDelta)
        {
            if (!CanNavigate(tile) || wheelDelta == 0) return;
            tile.MoveSlice(wheelDelta > 0 ? 1 : -1);
        }

        internal static bool TryNavigate(ViewportTileViewModel? tile, Key key)
        {
            if (!CanNavigate(tile)) return false;

            switch (key)
            {
                case Key.Up:
                    tile!.MoveSlice(1);
                    return true;
                case Key.Down:
                    tile!.MoveSlice(-1);
                    return true;
                case Key.PageUp:
                    tile!.MoveSlice(10);
                    return true;
                case Key.PageDown:
                    tile!.MoveSlice(-10);
                    return true;
                case Key.Home:
                    tile!.SetSlice(1);
                    return true;
                case Key.End:
                    tile!.SetSlice(tile.SliceCount);
                    return true;
                default:
                    return false;
            }
        }
    }
}
