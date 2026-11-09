using MarcusRunge.Mopr.Workbench.Modules.Studies.ViewModels;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Studies;
using MarcusRunge.Mopr.Workbench.Services.Application.Models.Studies;
using Moq;
using System.Reflection;

namespace MarcusRunge.Mopr.Workbench.Modules.Studies.Test.ViewModels
{
    internal sealed class StudySelectionViewModelTestContext
    {
        public StudySelectionViewModelTestContext()
        {
            Application.SetupGet(value => value.StudiesService).Returns(StudiesService.Object);
            StudiesService.SetupGet(value => value.StudyCatalog).Returns(StudyCatalogService.Object);
            ViewModel = new StudySelectionViewModel(Application.Object);
        }

        public Mock<IApplication> Application { get; } = new();
        public Mock<IStudiesService> StudiesService { get; } = new();
        public Mock<IStudyCatalogService> StudyCatalogService { get; } = new();
        public StudySelectionViewModel ViewModel { get; }

        public static StudyCatalogItem CreateStudy(int studyId = 1, IReadOnlyList<SeriesCatalogItem>? series = null) => new(studyId, $"study-{studyId}", "MUSTERMANN, MAX", new DateTime(1968, 3, 14), $"PAT-{studyId}", new DateTime(2026, 11, 9, 8, 30, 0), "CT Thorax", $"ACC-{studyId}", series ?? new[] { CreateSeries(11), CreateSeries(12), CreateSeries(13) });

        public static SeriesCatalogItem CreateSeries(int seriesId, bool isAvailable = true) => new(seriesId, $"series-{seriesId}", seriesId, $"Series {seriesId}", "CT", 10, isAvailable ? 10 : 9);

        public static Task InvokeLoadAsync(StudySelectionViewModel viewModel)
        {
            MethodInfo method = typeof(StudySelectionViewModel).GetMethod("LoadAsync", BindingFlags.Instance | BindingFlags.NonPublic) ?? throw new MissingMethodException(typeof(StudySelectionViewModel).FullName, "LoadAsync");
            return method.Invoke(viewModel, Array.Empty<object>()) as Task ?? throw new InvalidOperationException("LoadAsync did not return a Task.");
        }

        public static void InvokeExecuteLoad(StudySelectionViewModel viewModel)
        {
            MethodInfo method = typeof(StudySelectionViewModel).GetMethod("ExecuteLoad", BindingFlags.Instance | BindingFlags.NonPublic) ?? throw new MissingMethodException(typeof(StudySelectionViewModel).FullName, "ExecuteLoad");
            method.Invoke(viewModel, Array.Empty<object>());
        }

        public static async Task WaitUntilAsync(Func<bool> condition, CancellationToken cancellationToken)
        {
            while (!condition()) await Task.Delay(10, cancellationToken);
        }
    }
}