using MarcusRunge.Mopr.Workbench.Services.Persistence.Models.Studies;

namespace MarcusRunge.Mopr.Workbench.Services.Persistence.Test
{
    public sealed partial class PersistenceIntegrationTests
    {
        [Fact, Priority(37)]
        public async Task ImportedSeries_Should_Project_Exact_Series_And_Ordered_Instances()
        {
            PersistedImportedSeries? series = await _fixture.Persistence!.ImportedSeries!.GetAsync(_fixture.SeriesId, _fixture.SeriesInstanceUid, TestContext.Current.CancellationToken);

            Assert.NotNull(series);
            Assert.Equal(_fixture.SeriesId, series.SeriesId);
            Assert.Equal(_fixture.SeriesInstanceUid, series.SeriesInstanceUid);
            Assert.Equal(_fixture.StudyId, series.StudyId);
            Assert.Equal(_fixture.StudyInstanceUid, series.StudyInstanceUid);
            Assert.NotEmpty(series.Instances);
            Assert.Equal(series.Instances.OrderBy(instance => instance.InstanceNumber).ThenBy(instance => instance.InstanceId).Select(instance => instance.InstanceId), series.Instances.Select(instance => instance.InstanceId));
            Assert.All(series.Instances, instance =>
            {
                Assert.Equal(_fixture.RepositoryLocationId, instance.RepositoryLocationId);
                Assert.True(instance.RepositoryLocationIsEnabled);
                Assert.Equal(Path.TrimEndingDirectorySeparator(Path.GetFullPath(_fixture.RepositoryLocationRootPath)), Path.TrimEndingDirectorySeparator(Path.GetFullPath(instance.RepositoryRootPath!)));
            });
        }

        [Fact, Priority(38)]
        public async Task ImportedSeries_Should_Require_Matching_Id_And_Uid()
        {
            PersistedImportedSeries? wrongId = await _fixture.Persistence!.ImportedSeries!.GetAsync(int.MaxValue, _fixture.SeriesInstanceUid, TestContext.Current.CancellationToken);
            PersistedImportedSeries? wrongUid = await _fixture.Persistence.ImportedSeries.GetAsync(_fixture.SeriesId, $"Wrong_{Guid.NewGuid():N}", TestContext.Current.CancellationToken);

            Assert.Null(wrongId);
            Assert.Null(wrongUid);
        }

        [Fact, Priority(39)]
        public async Task ImportedSeries_Should_Propagate_Cancellation()
        {
            using var cancellation = new CancellationTokenSource();
            await cancellation.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => _fixture.Persistence!.ImportedSeries!.GetAsync(_fixture.SeriesId, _fixture.SeriesInstanceUid, cancellation.Token));
        }
    }
}