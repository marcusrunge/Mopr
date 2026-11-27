using MarcusRunge.Mopr.Workbench.Modules.Imaging.ViewModels;
using System.Windows.Input;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.Behaviors
{
    internal static class ViewportSliceNavigation
    {
        internal static bool CanNavigate(ViewportTileViewModel? tile) => tile?.Series != null && tile.ImagePositions.Count > 0 && tile.SliceCount > 0;
        internal static int GetWheelSliceDelta(int wheelDelta) => wheelDelta > 0 ? 1 : wheelDelta < 0 ? -1 : 0;

        internal static void MoveByWheelDelta(ViewportTileViewModel tile, int wheelDelta)
        {
            if (!CanNavigate(tile)) return;
            var delta = GetWheelSliceDelta(wheelDelta);
            if (delta != 0) tile.RequestSliceNavigation(delta);
        }

        internal static bool TryNavigate(ViewportTileViewModel? tile, Key key)
        {
            if (!CanNavigate(tile)) return false;
            var delta = key switch
            {
                Key.Up => 1,
                Key.Down => -1,
                Key.PageUp => 10,
                Key.PageDown => -10,
                Key.Home => int.MinValue,
                Key.End => int.MaxValue,
                _ => 0
            };
            if (delta == 0) return false;
            tile!.RequestSliceNavigation(delta);
            return true;
        }
    }
}

