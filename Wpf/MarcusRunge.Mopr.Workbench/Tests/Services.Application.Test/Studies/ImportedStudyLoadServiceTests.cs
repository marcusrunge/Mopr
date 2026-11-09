using MarcusRunge.Mopr.Workbench.Services.Application.Models.Studies;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Models.Studies;
using Moq;

namespace MarcusRunge.Mopr.Workbench.Services.Application.Test.Studies
{
    public sealed class ImportedStudyLoadServiceTests
    {
        [Fact]
        public async Task ResolveAsync_Should_Resolve_All_Selected_Series_Atomically()
        {
            using var context = new ImportedSeriesLoadServiceTestContext();
            var firstFile = context.CreateFile(Path.Combine("study", "series-1", "1.dcm"));
            var secondFile = context.CreateFile(Path.Combine("study", "series-2", "1.dcm"));
            PersistedImportedSeries firstSeries = context.CreateSeries([context.CreateInstance(1, Path.Combine("study", "series-1", "1.dcm"), 1)], seriesId: 17, seriesInstanceUid: "1.2.3.1");
            PersistedImportedSeries secondSeries = context.CreateSeries([context.CreateInstance(2, Path.Combine("study", "series-2", "1.dcm"), 1)], seriesId: 18, seriesInstanceUid: "1.2.3.2");
            context.Repository.Setup(value => value.GetAsync(firstSeries.SeriesId, firstSeries.SeriesInstanceUid, It.IsAny<CancellationToken>())).ReturnsAsync(firstSeries);
            context.Repository.Setup(value => value.GetAsync(secondSeries.SeriesId, secondSeries.SeriesInstanceUid, It.IsAny<CancellationToken>())).ReturnsAsync(secondSeries);
            ImportedSeriesLoadRequest[] requests = [new(firstSeries.SeriesId, firstSeries.SeriesInstanceUid), new(secondSeries.SeriesId, secondSeries.SeriesInstanceUid)];

            ImportedStudyLoadResult result = await context.Service.ResolveAsync(requests, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccessful);
            Assert.Equal("1.2.3.4", result.StudyInstanceUid);
            Assert.Equal(2, result.Series.Count);
            Assert.Equal(firstFile, Assert.Single(result.Series[0].Files));
            Assert.Equal(secondFile, Assert.Single(result.Series[1].Files));
            Assert.Empty(result.TechnicalErrors);
        }

        [Fact]
        public async Task ResolveAsync_Should_Reject_Empty_Selection()
        {
            using var context = new ImportedSeriesLoadServiceTestContext();

            ImportedStudyLoadResult result = await context.Service.ResolveAsync([], TestContext.Current.CancellationToken);

            Assert.False(result.IsSuccessful);
            Assert.Empty(result.Series);
            Assert.Contains(result.TechnicalErrors, error => error.Contains("At least one imported series", StringComparison.Ordinal));
            context.Repository.Verify(value => value.GetAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ResolveAsync_Should_Reject_Duplicate_Series_Identity()
        {
            using var context = new ImportedSeriesLoadServiceTestContext();
            ImportedSeriesLoadRequest request = new(17, "1.2.3.1");

            ImportedStudyLoadResult result = await context.Service.ResolveAsync([request, request], TestContext.Current.CancellationToken);

            Assert.False(result.IsSuccessful);
            Assert.Contains(result.TechnicalErrors, error => error.Contains("duplicate identities", StringComparison.Ordinal));
            context.Repository.Verify(value => value.GetAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ResolveAsync_Should_Reject_Series_From_Different_Studies()
        {
            using var context = new ImportedSeriesLoadServiceTestContext();
            context.CreateFile(Path.Combine("study-1", "1.dcm"));
            context.CreateFile(Path.Combine("study-2", "1.dcm"));
            PersistedImportedSeries firstSeries = context.CreateSeries([context.CreateInstance(1, Path.Combine("study-1", "1.dcm"), 1)], studyInstanceUid: "study-1", seriesId: 17, seriesInstanceUid: "series-1");
            PersistedImportedSeries secondSeries = context.CreateSeries([context.CreateInstance(2, Path.Combine("study-2", "1.dcm"), 1)], studyInstanceUid: "study-2", seriesId: 18, seriesInstanceUid: "series-2");
            context.Repository.Setup(value => value.GetAsync(firstSeries.SeriesId, firstSeries.SeriesInstanceUid, It.IsAny<CancellationToken>())).ReturnsAsync(firstSeries);
            context.Repository.Setup(value => value.GetAsync(secondSeries.SeriesId, secondSeries.SeriesInstanceUid, It.IsAny<CancellationToken>())).ReturnsAsync(secondSeries);

            ImportedStudyLoadResult result = await context.Service.ResolveAsync([new ImportedSeriesLoadRequest(firstSeries.SeriesId, firstSeries.SeriesInstanceUid), new ImportedSeriesLoadRequest(secondSeries.SeriesId, secondSeries.SeriesInstanceUid)], TestContext.Current.CancellationToken);

            Assert.False(result.IsSuccessful);
            Assert.Empty(result.Series);
            Assert.Contains(result.TechnicalErrors, error => error.Contains("same persisted study", StringComparison.Ordinal));
        }

        [Fact]
        public async Task ResolveAsync_Should_Fail_Atomically_When_One_Series_Is_Invalid()
        {
            using var context = new ImportedSeriesLoadServiceTestContext();
            context.CreateFile(Path.Combine("study", "valid.dcm"));
            PersistedImportedSeries validSeries = context.CreateSeries([context.CreateInstance(1, Path.Combine("study", "valid.dcm"), 1)], seriesId: 17, seriesInstanceUid: "series-1");
            PersistedImportedSeries invalidSeries = context.CreateSeries([context.CreateInstance(2, Path.Combine("study", "missing.dcm"), 1)], seriesId: 18, seriesInstanceUid: "series-2");
            context.Repository.Setup(value => value.GetAsync(validSeries.SeriesId, validSeries.SeriesInstanceUid, It.IsAny<CancellationToken>())).ReturnsAsync(validSeries);
            context.Repository.Setup(value => value.GetAsync(invalidSeries.SeriesId, invalidSeries.SeriesInstanceUid, It.IsAny<CancellationToken>())).ReturnsAsync(invalidSeries);

            ImportedStudyLoadResult result = await context.Service.ResolveAsync([new ImportedSeriesLoadRequest(validSeries.SeriesId, validSeries.SeriesInstanceUid), new ImportedSeriesLoadRequest(invalidSeries.SeriesId, invalidSeries.SeriesInstanceUid)], TestContext.Current.CancellationToken);

            Assert.False(result.IsSuccessful);
            Assert.Empty(result.Series);
            Assert.Contains(result.TechnicalErrors, error => error.Contains("does not exist", StringComparison.Ordinal));
        }

        [Fact]
        public async Task ResolveAsync_Should_Reject_File_Shared_By_Selected_Series()
        {
            using var context = new ImportedSeriesLoadServiceTestContext();
            context.CreateFile(Path.Combine("study", "shared.dcm"));
            PersistedImportedSeries firstSeries = context.CreateSeries([context.CreateInstance(1, Path.Combine("study", "shared.dcm"), 1)], seriesId: 17, seriesInstanceUid: "series-1");
            PersistedImportedSeries secondSeries = context.CreateSeries([context.CreateInstance(2, Path.Combine("study", "shared.dcm"), 1)], seriesId: 18, seriesInstanceUid: "series-2");
            context.Repository.Setup(value => value.GetAsync(firstSeries.SeriesId, firstSeries.SeriesInstanceUid, It.IsAny<CancellationToken>())).ReturnsAsync(firstSeries);
            context.Repository.Setup(value => value.GetAsync(secondSeries.SeriesId, secondSeries.SeriesInstanceUid, It.IsAny<CancellationToken>())).ReturnsAsync(secondSeries);

            ImportedStudyLoadResult result = await context.Service.ResolveAsync([new ImportedSeriesLoadRequest(firstSeries.SeriesId, firstSeries.SeriesInstanceUid), new ImportedSeriesLoadRequest(secondSeries.SeriesId, secondSeries.SeriesInstanceUid)], TestContext.Current.CancellationToken);

            Assert.False(result.IsSuccessful);
            Assert.Contains(result.TechnicalErrors, error => error.Contains("same repository file", StringComparison.Ordinal));
        }

        [Fact]
        public async Task ResolveAsync_Should_Propagate_Cancellation()
        {
            using var context = new ImportedSeriesLoadServiceTestContext();
            using var cancellation = new CancellationTokenSource();
            await cancellation.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => context.Service.ResolveAsync([new ImportedSeriesLoadRequest(17, "series-1")], cancellation.Token));
        }
    }
}