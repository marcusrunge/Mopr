using MarcusRunge.Mopr.Workbench.Modules.Imaging.Infrastructure.Viewports;
using MarcusRunge.Mopr.Workbench.Modules.Imaging.ViewModels;
using Xunit;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.Test.ViewModels
{
    public sealed class ViewportTransformStateTests
    {
        [Fact]
        public void Constructor_Should_Create_FitToView_State()
        {
            var state = new ViewportTransformState();

            Assert.Equal(ViewportImageGeometryCalculator.DefaultZoomFactor, state.ZoomFactor);
            Assert.Equal(0.0, state.PanOffsetX);
            Assert.Equal(0.0, state.PanOffsetY);
            Assert.True(state.CanZoomIn);
            Assert.True(state.CanZoomOut);
        }

        [Fact]
        public void ZoomIn_Should_Increase_ZoomFactor()
        {
            var state = new ViewportTransformState();

            state.ZoomIn();

            Assert.Equal(1.25, state.ZoomFactor, precision: 10);
        }

        [Fact]
        public void ZoomOut_Should_Decrease_ZoomFactor()
        {
            var state = new ViewportTransformState();

            state.ZoomOut();

            Assert.Equal(0.8, state.ZoomFactor, precision: 10);
        }

        [Fact]
        public void Repeated_ZoomIn_Should_Not_Exceed_Maximum()
        {
            var state = new ViewportTransformState();

            for (var index = 0; index < 100; index++) state.ZoomIn();

            Assert.Equal(ViewportImageGeometryCalculator.MaximumZoomFactor, state.ZoomFactor);
            Assert.False(state.CanZoomIn);
            Assert.True(state.CanZoomOut);
        }

        [Fact]
        public void Repeated_ZoomOut_Should_Not_Fall_Below_Minimum()
        {
            var state = new ViewportTransformState();

            for (var index = 0; index < 100; index++) state.ZoomOut();

            Assert.Equal(ViewportImageGeometryCalculator.MinimumZoomFactor, state.ZoomFactor);
            Assert.True(state.CanZoomIn);
            Assert.False(state.CanZoomOut);
        }

        [Theory]
        [InlineData(double.NaN)]
        [InlineData(double.NegativeInfinity)]
        [InlineData(double.PositiveInfinity)]
        public void SetZoom_Should_Replace_Invalid_Value_With_Default(double zoomFactor)
        {
            var state = new ViewportTransformState();
            state.SetZoom(2.0);

            state.SetZoom(zoomFactor);

            Assert.Equal(ViewportImageGeometryCalculator.DefaultZoomFactor, state.ZoomFactor);
        }

        [Theory]
        [InlineData(1.0)]
        [InlineData(0.0)]
        [InlineData(-1.0)]
        [InlineData(double.NaN)]
        [InlineData(double.NegativeInfinity)]
        [InlineData(double.PositiveInfinity)]
        public void ZoomIn_Should_Ignore_Invalid_Step(double zoomStep)
        {
            var state = new ViewportTransformState();

            state.ZoomIn(zoomStep);

            Assert.Equal(ViewportImageGeometryCalculator.DefaultZoomFactor, state.ZoomFactor);
        }

        [Theory]
        [InlineData(1.0)]
        [InlineData(0.0)]
        [InlineData(-1.0)]
        [InlineData(double.NaN)]
        [InlineData(double.NegativeInfinity)]
        [InlineData(double.PositiveInfinity)]
        public void ZoomOut_Should_Ignore_Invalid_Step(double zoomStep)
        {
            var state = new ViewportTransformState();

            state.ZoomOut(zoomStep);

            Assert.Equal(ViewportImageGeometryCalculator.DefaultZoomFactor, state.ZoomFactor);
        }

        [Fact]
        public void AddPan_Should_Add_Horizontal_And_Vertical_Deltas()
        {
            var state = new ViewportTransformState();
            state.SetPan(25.0, -30.0);

            state.AddPan(15.0, 12.0);

            Assert.Equal(40.0, state.PanOffsetX);
            Assert.Equal(-18.0, state.PanOffsetY);
        }

        [Fact]
        public void AddPan_Should_Ignore_Movement_When_One_Delta_Is_Invalid()
        {
            var state = new ViewportTransformState();
            state.SetPan(25.0, -30.0);

            state.AddPan(double.NaN, 12.0);

            Assert.Equal(25.0, state.PanOffsetX);
            Assert.Equal(-30.0, state.PanOffsetY);
        }

        [Theory]
        [InlineData(double.NaN, 20.0, 0.0, 20.0)]
        [InlineData(20.0, double.NaN, 20.0, 0.0)]
        [InlineData(double.PositiveInfinity, double.NegativeInfinity, 0.0, 0.0)]
        public void SetPan_Should_Normalize_Invalid_Offsets(double panOffsetX, double panOffsetY, double expectedPanOffsetX, double expectedPanOffsetY)
        {
            var state = new ViewportTransformState();

            state.SetPan(panOffsetX, panOffsetY);

            Assert.Equal(expectedPanOffsetX, state.PanOffsetX);
            Assert.Equal(expectedPanOffsetY, state.PanOffsetY);
        }

        [Fact]
        public void ResetPan_Should_Preserve_ZoomFactor()
        {
            var state = new ViewportTransformState();
            state.SetZoomAndPan(3.0, 120.0, -80.0);

            state.ResetPan();

            Assert.Equal(3.0, state.ZoomFactor);
            Assert.Equal(0.0, state.PanOffsetX);
            Assert.Equal(0.0, state.PanOffsetY);
        }

        [Fact]
        public void Reset_Should_Restore_FitToView_And_ZeroPan()
        {
            var state = new ViewportTransformState();
            state.SetZoomAndPan(4.0, 250.0, -130.0);

            state.Reset();

            Assert.Equal(ViewportImageGeometryCalculator.DefaultZoomFactor, state.ZoomFactor);
            Assert.Equal(0.0, state.PanOffsetX);
            Assert.Equal(0.0, state.PanOffsetY);
        }

        [Fact]
        public void Independent_States_Should_Not_Affect_Each_Other()
        {
            var firstState = new ViewportTransformState();
            var secondState = new ViewportTransformState();

            firstState.SetZoomAndPan(3.0, 120.0, -80.0);

            Assert.Equal(3.0, firstState.ZoomFactor);
            Assert.Equal(120.0, firstState.PanOffsetX);
            Assert.Equal(-80.0, firstState.PanOffsetY);

            Assert.Equal(ViewportImageGeometryCalculator.DefaultZoomFactor, secondState.ZoomFactor);
            Assert.Equal(0.0, secondState.PanOffsetX);
            Assert.Equal(0.0, secondState.PanOffsetY);
        }

        [Fact]
        public void PropertyChanged_Should_Report_Changed_Transform_Properties()
        {
            var state = new ViewportTransformState();
            var changedProperties = new List<string?>();
            state.PropertyChanged += (_, eventArgs) => changedProperties.Add(eventArgs.PropertyName);

            state.SetZoomAndPan(2.0, 10.0, 20.0);

            Assert.Contains(nameof(ViewportTransformState.ZoomFactor), changedProperties);
            Assert.Contains(nameof(ViewportTransformState.PanOffsetX), changedProperties);
            Assert.Contains(nameof(ViewportTransformState.PanOffsetY), changedProperties);
            Assert.Contains(nameof(ViewportTransformState.CanZoomIn), changedProperties);
            Assert.Contains(nameof(ViewportTransformState.CanZoomOut), changedProperties);
        }
    }
}