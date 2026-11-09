using MarcusRunge.Mopr.Workbench.Services.Application.Models.Studies;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Models.Studies;
using Moq;

namespace MarcusRunge.Mopr.Workbench.Services.Application.Test.Studies
{
    public sealed class ImportedSeriesLoadServiceTests
    {
        [Fact]
        public async Task ResolveAsync_Should_Return_Validated_Files_In_Persisted_Order()
        {
            using var context = new ImportedSeriesLoadServiceTestContext();
            var secondPath = context.CreateFile(Path.Combine("study", "series", "2.dcm"));
            var firstPath = context.CreateFile(Path.Combine("study", "series", "1.dcm"));
            PersistedImportedSeries series = context.CreateSeries([context.CreateInstance(2, Path.Combine("study", "series", "2.dcm"), 2), context.CreateInstance(1, Path.Combine("study", "series", "1.dcm"), 1)]);
            context.SetupSeries(series);

            ImportedSeriesLoadResult result = await context.Service.ResolveAsync(new ImportedSeriesLoadRequest(series.SeriesId, series.SeriesInstanceUid), TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccessful);
            Assert.Equal(series.StudyInstanceUid, result.StudyInstanceUid);
            Assert.Equal([secondPath, firstPath], result.Files);
            Assert.Empty(result.TechnicalErrors);
        }

        [Theory]
        [InlineData(0, "1.2.3")]
        [InlineData(-1, "1.2.3")]
        [InlineData(1, "")]
        [InlineData(1, "   ")]
        public async Task ResolveAsync_Should_Reject_Incomplete_Identity(int seriesId, string seriesInstanceUid)
        {
            using var context = new ImportedSeriesLoadServiceTestContext();

            ImportedSeriesLoadResult result = await context.Service.ResolveAsync(new ImportedSeriesLoadRequest(seriesId, seriesInstanceUid), TestContext.Current.CancellationToken);

            Assert.False(result.IsSuccessful);
            Assert.Empty(result.Files);
            context.Repository.Verify(value => value.GetAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ResolveAsync_Should_Reject_Missing_Series()
        {
            using var context = new ImportedSeriesLoadServiceTestContext();
            context.SetupSeries(null);

            ImportedSeriesLoadResult result = await context.Service.ResolveAsync(new ImportedSeriesLoadRequest(17, "1.2.3"), TestContext.Current.CancellationToken);

            Assert.False(result.IsSuccessful);
            Assert.Contains(result.TechnicalErrors, error => error.Contains("No persisted series", StringComparison.Ordinal));
        }

        [Fact]
        public async Task ResolveAsync_Should_Reject_Missing_Study_Identity()
        {
            using var context = new ImportedSeriesLoadServiceTestContext();
            PersistedImportedSeries series = context.CreateSeries(studyInstanceUid: string.Empty);
            context.SetupSeries(series);

            ImportedSeriesLoadResult result = await context.Service.ResolveAsync(new ImportedSeriesLoadRequest(series.SeriesId, series.SeriesInstanceUid), TestContext.Current.CancellationToken);

            Assert.False(result.IsSuccessful);
            Assert.Contains(result.TechnicalErrors, error => error.Contains("parent study identity", StringComparison.Ordinal));
        }

        [Fact]
        public async Task ResolveAsync_Should_Reject_Empty_Series()
        {
            using var context = new ImportedSeriesLoadServiceTestContext();
            PersistedImportedSeries series = context.CreateSeries();
            context.SetupSeries(series);

            ImportedSeriesLoadResult result = await context.Service.ResolveAsync(new ImportedSeriesLoadRequest(series.SeriesId, series.SeriesInstanceUid), TestContext.Current.CancellationToken);

            Assert.False(result.IsSuccessful);
            Assert.Contains(result.TechnicalErrors, error => error.Contains("contains no DICOM instances", StringComparison.Ordinal));
        }

        [Fact]
        public async Task ResolveAsync_Should_Reject_Disabled_Repository()
        {
            using var context = new ImportedSeriesLoadServiceTestContext();
            PersistedImportedSeriesInstance instance = context.CreateInstance(1, "1.dcm", repositoryLocationIsEnabled: false);
            PersistedImportedSeries series = context.CreateSeries([instance]);
            context.SetupSeries(series);

            ImportedSeriesLoadResult result = await context.Service.ResolveAsync(new ImportedSeriesLoadRequest(series.SeriesId, series.SeriesInstanceUid), TestContext.Current.CancellationToken);

            Assert.False(result.IsSuccessful);
            Assert.Contains(result.TechnicalErrors, error => error.Contains("disabled or unavailable", StringComparison.Ordinal));
        }

        [Fact]
        public async Task ResolveAsync_Should_Reject_Missing_Root_Path()
        {
            using var context = new ImportedSeriesLoadServiceTestContext();
            PersistedImportedSeriesInstance instance = new(1, "1.2.3", 1, 5, true, null, "1.dcm");
            PersistedImportedSeries series = context.CreateSeries([instance]);
            context.SetupSeries(series);

            ImportedSeriesLoadResult result = await context.Service.ResolveAsync(new ImportedSeriesLoadRequest(series.SeriesId, series.SeriesInstanceUid), TestContext.Current.CancellationToken);

            Assert.False(result.IsSuccessful);
            Assert.Contains(result.TechnicalErrors, error => error.Contains("has no root path", StringComparison.Ordinal));
        }

        [Fact]
        public async Task ResolveAsync_Should_Reject_Missing_Relative_Path()
        {
            using var context = new ImportedSeriesLoadServiceTestContext();
            PersistedImportedSeriesInstance instance = new(1, "1.2.3", 1, 5, true, context.RepositoryRootPath, null);
            PersistedImportedSeries series = context.CreateSeries([instance]);
            context.SetupSeries(series);

            ImportedSeriesLoadResult result = await context.Service.ResolveAsync(new ImportedSeriesLoadRequest(series.SeriesId, series.SeriesInstanceUid), TestContext.Current.CancellationToken);

            Assert.False(result.IsSuccessful);
            Assert.Contains(result.TechnicalErrors, error => error.Contains("has no relative repository path", StringComparison.Ordinal));
        }

        [Fact]
        public async Task ResolveAsync_Should_Reject_Path_Traversal()
        {
            using var context = new ImportedSeriesLoadServiceTestContext();
            PersistedImportedSeries series = context.CreateSeries([context.CreateInstance(1, Path.Combine("..", "outside.dcm"))]);
            context.SetupSeries(series);

            ImportedSeriesLoadResult result = await context.Service.ResolveAsync(new ImportedSeriesLoadRequest(series.SeriesId, series.SeriesInstanceUid), TestContext.Current.CancellationToken);

            Assert.False(result.IsSuccessful);
            Assert.Contains(result.TechnicalErrors, error => error.Contains("outside repository location", StringComparison.Ordinal));
        }

        [Fact]
        public async Task ResolveAsync_Should_Reject_Missing_File()
        {
            using var context = new ImportedSeriesLoadServiceTestContext();
            PersistedImportedSeries series = context.CreateSeries([context.CreateInstance(1, "missing.dcm")]);
            context.SetupSeries(series);

            ImportedSeriesLoadResult result = await context.Service.ResolveAsync(new ImportedSeriesLoadRequest(series.SeriesId, series.SeriesInstanceUid), TestContext.Current.CancellationToken);

            Assert.False(result.IsSuccessful);
            Assert.Contains(result.TechnicalErrors, error => error.Contains("does not exist", StringComparison.Ordinal));
        }

        [Fact]
        public async Task ResolveAsync_Should_Reject_Duplicate_Physical_File()
        {
            using var context = new ImportedSeriesLoadServiceTestContext();
            context.CreateFile("same.dcm");
            PersistedImportedSeries series = context.CreateSeries([context.CreateInstance(1, "same.dcm", 1), context.CreateInstance(2, "same.dcm", 2)]);
            context.SetupSeries(series);

            ImportedSeriesLoadResult result = await context.Service.ResolveAsync(new ImportedSeriesLoadRequest(series.SeriesId, series.SeriesInstanceUid), TestContext.Current.CancellationToken);

            Assert.False(result.IsSuccessful);
            Assert.Contains(result.TechnicalErrors, error => error.Contains("same repository file", StringComparison.Ordinal));
        }

        [Fact]
        public async Task ResolveAsync_Should_Propagate_Cancellation()
        {
            using var context = new ImportedSeriesLoadServiceTestContext();
            using var cancellation = new CancellationTokenSource();
            await cancellation.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => context.Service.ResolveAsync(new ImportedSeriesLoadRequest(17, "1.2.3"), cancellation.Token));
        }
    }
}