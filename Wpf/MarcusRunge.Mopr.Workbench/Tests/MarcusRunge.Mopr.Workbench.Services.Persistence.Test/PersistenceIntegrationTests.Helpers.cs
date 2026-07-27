using MarcusRunge.Mopr.Workbench.Services.Persistence.Entities;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Models;

namespace MarcusRunge.Mopr.Workbench.Services.Persistence.Test
{
    public sealed partial class PersistenceIntegrationTests
    {
        private async Task<Instance> GetInstanceAsync()
        {
            Instance? instance = await _fixture.Persistence!.Instance!.GetBySopInstanceUidAsync(_fixture.SopInstanceUid, TestContext.Current.CancellationToken);
            Assert.NotNull(instance);
            return instance;
        }

        private async Task<Series> GetSeriesAsync()
        {
            Series? series = await _fixture.Persistence!.Series!.GetBySeriesInstanceUidAsync(_fixture.SeriesInstanceUid, TestContext.Current.CancellationToken);
            Assert.NotNull(series);
            return series;
        }

        private async Task<Study> GetStudyAsync()
        {
            Study? study = await _fixture.Persistence!.Study!.GetByStudyInstanceUidAsync(_fixture.StudyInstanceUid, TestContext.Current.CancellationToken);
            Assert.NotNull(study);
            return study;
        }

        private async Task<User> GetUserAsync()
        {
            User? user = await _fixture.Persistence!.User!.GetByIdAsync(_fixture.UserId, TestContext.Current.CancellationToken);
            Assert.NotNull(user);
            return user;
        }

        private async Task<PersistenceIntegrityResult> VerifyIntegrityAsync(PersistenceIntegrityRequest? request = null) => await _fixture.Persistence!.Integrity!.VerifyAsync(request ?? new PersistenceIntegrityRequest(), TestContext.Current.CancellationToken);
    }
}