using MarcusRunge.Mopr.Workbench.Contracts.Models;
using MarcusRunge.Mopr.Workbench.Modules.Imaging.Behaviors;
using MarcusRunge.Mopr.Workbench.Modules.Imaging.Services;
using MarcusRunge.Mopr.Workbench.Modules.Imaging.ViewModels;
using Moq;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Xunit;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.Test.Behaviors
{
    public sealed class ViewportSliceNavigationTests
    {
        public static TheoryData<Key, int> KeyboardCases => new()
        {
            { Key.Up, 6 },
            { Key.Down, 4 },
            { Key.PageUp, 15 },
            { Key.PageDown, 1 },
            { Key.Home, 1 },
            { Key.End, 20 }
        };

        [Theory]
        [MemberData(nameof(KeyboardCases))]
        public void TryNavigate_Should_Apply_Keyboard_Semantics(Key key, int expectedSlice)
        {
            var tile = CreateTile(20);
            tile.SetSlice(5);

            var handled = ViewportSliceNavigation.TryNavigate(tile, key);

            Assert.True(handled);
            Assert.Equal(expectedSlice, tile.CurrentSlice);
        }

        [Fact]
        public void MoveByWheelDelta_Should_Move_Exactly_One_Slice_In_Each_Direction()
        {
            var tile = CreateTile(3);
            tile.SetSlice(2);

            ViewportSliceNavigation.MoveByWheelDelta(tile, 120);
            Assert.Equal(3, tile.CurrentSlice);

            ViewportSliceNavigation.MoveByWheelDelta(tile, -120);
            Assert.Equal(2, tile.CurrentSlice);
        }

        [Fact]
        public void Navigation_Should_Clamp_To_First_And_Last_Slice()
        {
            var tile = CreateTile(3);

            tile.SetSlice(1);
            Assert.True(ViewportSliceNavigation.TryNavigate(tile, Key.Down));
            Assert.Equal(1, tile.CurrentSlice);

            tile.SetSlice(3);
            Assert.True(ViewportSliceNavigation.TryNavigate(tile, Key.PageUp));
            Assert.Equal(3, tile.CurrentSlice);
        }

        [Fact]
        public void Navigation_Should_Ignore_Empty_Viewport()
        {
            var tile = CreateEmptyTile();

            Assert.False(ViewportSliceNavigation.TryNavigate(tile, Key.Up));
            ViewportSliceNavigation.MoveByWheelDelta(tile, 120);

            Assert.Equal(1, tile.CurrentSlice);
        }

        [Fact]
        public void Navigation_Should_Change_Only_The_Target_Viewport()
        {
            var firstTile = CreateTile(3);
            var secondTile = CreateTile(3);
            firstTile.SetSlice(1);
            secondTile.SetSlice(2);

            ViewportSliceNavigation.MoveByWheelDelta(firstTile, 120);

            Assert.Equal(2, firstTile.CurrentSlice);
            Assert.Equal(2, secondTile.CurrentSlice);
        }

        [Fact]
        public void Slice_Change_Should_Rebind_Existing_Measurements_Without_Creating_Changes()
        {
            var firstKey = new ImagingMeasurementInstanceKey("1.2.3", "1.2.3.4", "1.2.3.4.1");
            var secondKey = new ImagingMeasurementInstanceKey("1.2.3", "1.2.3.4", "1.2.3.4.2");
            var firstMeasurements = new ObservableCollection<ViewportMeasurementViewModel>();
            var secondMeasurements = new ObservableCollection<ViewportMeasurementViewModel>();
            var store = new Mock<IImagingMeasurementSessionStore>();
            store.Setup(service => service.GetMeasurements(firstKey)).Returns(firstMeasurements);
            store.Setup(service => service.GetMeasurements(secondKey)).Returns(secondMeasurements);
            var resolver = new Mock<IImagingMeasurementInstanceResolver>();
            var series = new SeriesInfo("1.2.3.4", "CT", "Series", string.Empty, 2, "1.2.3");
            resolver.Setup(service => service.Resolve(series, "1.dcm", 0)).Returns(firstKey);
            resolver.Setup(service => service.Resolve(series, "2.dcm", 0)).Returns(secondKey);
            var tile = new ViewportTileViewModel("Viewport", "Viewport", store.Object, resolver.Object);
            tile.SetSeries(series, ["1.dcm", "2.dcm"]);
            ConnectCanonicalNavigation(tile);

            Assert.True(ViewportSliceNavigation.TryNavigate(tile, Key.Up));

            Assert.Same(secondMeasurements, tile.Measurements);
            Assert.Empty(secondMeasurements);
            store.Verify(service => service.Create(It.IsAny<ImagingMeasurementInstanceKey>(), It.IsAny<ViewportMeasurementViewModel>()), Times.Never);
            store.Verify(service => service.Delete(It.IsAny<ImagingMeasurementInstanceKey>(), It.IsAny<ViewportMeasurementViewModel>()), Times.Never);
        }

        private static void ConnectCanonicalNavigation(ViewportTileViewModel tile) => tile.SliceNavigationRequested += (_, args) => tile.SetLocalSlice(args.Delta == int.MinValue ? 1 : args.Delta == int.MaxValue ? tile.SliceCount : tile.CurrentSlice + args.Delta);

        private static ViewportTileViewModel CreateEmptyTile() => new("Viewport", "Viewport", Mock.Of<IImagingMeasurementSessionStore>(), Mock.Of<IImagingMeasurementInstanceResolver>());

        private static ViewportTileViewModel CreateTile(int sliceCount)
        {
            var store = new Mock<IImagingMeasurementSessionStore>();
            store.Setup(service => service.GetMeasurements(It.IsAny<ImagingMeasurementInstanceKey>())).Returns(() => new ObservableCollection<ViewportMeasurementViewModel>());
            var resolver = new Mock<IImagingMeasurementInstanceResolver>();
            resolver.Setup(service => service.Resolve(It.IsAny<SeriesInfo>(), It.IsAny<string>(), It.IsAny<int>())).Returns((SeriesInfo series, string filePath, int frameIndex) => new ImagingMeasurementInstanceKey(series.StudyId!, series.Id, filePath, frameIndex));
            var tile = new ViewportTileViewModel("Viewport", "Viewport", store.Object, resolver.Object);
            var series = new SeriesInfo("1.2.3.4", "CT", "Series", string.Empty, sliceCount, "1.2.3");
            tile.SetSeries(series, Enumerable.Range(1, sliceCount).Select(index => $"{index}.dcm").ToArray());
            ConnectCanonicalNavigation(tile);
            return tile;
        }
    }
}
