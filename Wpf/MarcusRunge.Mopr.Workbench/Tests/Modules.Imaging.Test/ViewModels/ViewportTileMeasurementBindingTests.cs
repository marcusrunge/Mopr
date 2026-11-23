using MarcusRunge.Mopr.Workbench.Contracts.Models;
using MarcusRunge.Mopr.Workbench.Modules.Imaging.Services;
using MarcusRunge.Mopr.Workbench.Modules.Imaging.ViewModels;
using Moq;
using System.Collections.ObjectModel;
using Xunit;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.Test.ViewModels
{
    public sealed class ViewportTileMeasurementBindingTests
    {
        [Fact]
        public void SetSeries_Should_Bind_First_Slice_Before_Measurement_Creation()
        {
            var key = new ImagingMeasurementInstanceKey("1.2.3", "1.2.3.4", "1.2.3.4.5");
            var measurements = new ObservableCollection<ViewportMeasurementViewModel>();
            var store = new Mock<IImagingMeasurementSessionStore>();
            store.Setup(service => service.GetMeasurements(key)).Returns(measurements);
            store.Setup(service => service.Create(key, It.IsAny<ViewportMeasurementViewModel>())).Callback<ImagingMeasurementInstanceKey, ViewportMeasurementViewModel>((_, measurement) => measurements.Add(measurement));
            var resolver = new Mock<IImagingMeasurementInstanceResolver>();
            var series = new SeriesInfo("1.2.3.4", "CT", "Series", string.Empty, 1, "1.2.3");
            const string filePath = @"C:\study\instance.dcm";
            resolver.Setup(service => service.Resolve(series, filePath)).Returns(key);
            var viewModel = new ViewportTileViewModel("Viewport", "Viewport", store.Object, resolver.Object);

            viewModel.SetSeries(series, [filePath]);
            viewModel.BeginOrCompleteMeasurement(10, 20);
            viewModel.BeginOrCompleteMeasurement(30, 40);

            store.Verify(service => service.GetMeasurements(key), Times.Once);
            store.Verify(service => service.Create(key, It.IsAny<ViewportMeasurementViewModel>()), Times.Once);
            Assert.Single(viewModel.Measurements);
        }

        [Fact]
        public void SetSlice_Should_Rebind_To_Selected_Dicom_Instance()
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
            resolver.Setup(service => service.Resolve(series, "first.dcm")).Returns(firstKey);
            resolver.Setup(service => service.Resolve(series, "second.dcm")).Returns(secondKey);
            var viewModel = new ViewportTileViewModel("Viewport", "Viewport", store.Object, resolver.Object);

            viewModel.SetSeries(series, ["first.dcm", "second.dcm"]);
            Assert.Same(firstMeasurements, viewModel.Measurements);

            viewModel.SetSlice(2);
            Assert.Same(secondMeasurements, viewModel.Measurements);
        }
    }
}
