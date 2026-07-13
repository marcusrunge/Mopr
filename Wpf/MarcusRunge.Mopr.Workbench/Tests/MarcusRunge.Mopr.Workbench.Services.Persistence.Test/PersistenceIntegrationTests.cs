using MarcusRunge.Mopr.Workbench.Contracts.Enums;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Entities;

namespace MarcusRunge.Mopr.Workbench.Services.Persistence.Test
{
    [TestCaseOrderer(typeof(PriorityOrderer))]
    public sealed class PersistenceIntegrationTests(PersistenceFixture fixture) : IClassFixture<PersistenceFixture>
    {
        private readonly PersistenceFixture _fixture = fixture;

        [Fact, Priority(1)]
        public void Repositories_Should_Be_Available()
        {
            Assert.NotNull(_fixture.Persistence);

            Assert.NotNull(_fixture.Persistence!.Study);
            Assert.NotNull(_fixture.Persistence.Series);
            Assert.NotNull(_fixture.Persistence.Instance);
            Assert.NotNull(_fixture.Persistence.Measurement);
            Assert.NotNull(_fixture.Persistence.User);
        }

        [Fact, Priority(2)]
        public async Task User_Should_Be_Saved_And_Loaded()
        {
            User user = new()
            {
                LoginName = "TestUser"
            };
            await _fixture.Persistence!.User!.AddAsync(user, TestContext.Current.CancellationToken);
            User? loaded = await _fixture.Persistence.User.GetByLoginNameAsync("TestUser", TestContext.Current.CancellationToken);
            Assert.NotNull(loaded);
            Assert.Equal("TestUser", loaded!.LoginName);
            _fixture.UserId = loaded.Id;
        }

        [Fact, Priority(3)]
        public async Task Study_Should_Be_Saved_And_Loaded()
        {
            Study study = new()
            {
                StudyInstanceUid = _fixture.StudyInstanceUid,
                CreatedByUserId = _fixture.UserId
            };
            await _fixture.Persistence!.Study!.AddAsync(study, TestContext.Current.CancellationToken);
            Study? loaded = await _fixture.Persistence.Study.GetByStudyInstanceUidAsync(_fixture.StudyInstanceUid, TestContext.Current.CancellationToken);
            Assert.NotNull(loaded);
            _fixture.StudyId = loaded!.Id;
        }

        [Fact, Priority(4)]
        public async Task Study_Should_Set_CreatedAtUtc()
        {
            Study? study = await _fixture.Persistence!.Study!.GetByStudyInstanceUidAsync(_fixture.StudyInstanceUid, TestContext.Current.CancellationToken);
            Assert.NotNull(study);
            Assert.NotEqual(default, study!.CreatedAtUtc);
        }

        [Fact, Priority(5)]
        public async Task Series_Should_Be_Saved_And_Loaded()
        {
            Series series = new()
            {
                StudyId = _fixture.StudyId,
                SeriesInstanceUid = _fixture.SeriesInstanceUid,
                CreatedByUserId = _fixture.UserId
            };

            await _fixture.Persistence!.Series!.AddAsync(series, TestContext.Current.CancellationToken);
            Series? loaded = await _fixture.Persistence.Series.GetBySeriesInstanceUidAsync(_fixture.SeriesInstanceUid, TestContext.Current.CancellationToken);
            Assert.NotNull(loaded);
            _fixture.SeriesId = loaded!.Id;
        }

        [Fact, Priority(6)]
        public async Task Instance_Should_Be_Saved_And_Loaded()
        {
            Instance instance = new()
            {
                SeriesId = _fixture.SeriesId,
                SopInstanceUid = _fixture.SopInstanceUid,
                CreatedByUserId = _fixture.UserId
            };
            await _fixture.Persistence!.Instance!.AddAsync(instance, TestContext.Current.CancellationToken);
            Instance? loaded = await _fixture.Persistence.Instance.GetBySopInstanceUidAsync(_fixture.SopInstanceUid, TestContext.Current.CancellationToken);
            Assert.NotNull(loaded);
            _fixture.InstanceId = loaded!.Id;
        }

        [Fact, Priority(7)]
        public async Task Instance_Should_Be_Found_By_SopInstanceUid()
        {
            Instance? loaded = await _fixture.Persistence!.Instance!.GetBySopInstanceUidAsync(_fixture.SopInstanceUid, TestContext.Current.CancellationToken);
            Assert.NotNull(loaded);
            Assert.Equal(_fixture.SopInstanceUid, loaded!.SopInstanceUid);
        }

        [Fact, Priority(8)]
        public async Task Measurement_Should_Be_Saved_And_Loaded()
        {
            Measurement measurement = new()
            {
                InstanceId = _fixture.InstanceId,
                CreatedByUserId = _fixture.UserId,
                MeasurementType = MeasurementType.Length,
                Title = "Test Measurement",
                DataJson = "{}"
            };
            await _fixture.Persistence!.Measurement!.AddAsync(measurement, TestContext.Current.CancellationToken);
            Measurement? loaded = await _fixture.Persistence.Measurement.GetByIdAsync(measurement.Id, TestContext.Current.CancellationToken);
            Assert.NotNull(loaded);
            _fixture.MeasurementId = loaded!.Id;
        }

        [Fact, Priority(9)]
        public async Task Measurement_Should_Be_Found_By_InstanceId()
        {
            IList<Measurement> measurements = await _fixture.Persistence!.Measurement!.GetByInstanceIdAsync(_fixture.InstanceId, TestContext.Current.CancellationToken);
            Assert.Single(measurements);
            Assert.Equal(_fixture.MeasurementId, measurements[0].Id);
        }

        [Fact, Priority(10)]
        public async Task Study_Should_Be_Updated()
        {
            Study? study = await _fixture.Persistence!.Study!.GetByStudyInstanceUidAsync(_fixture.StudyInstanceUid, TestContext.Current.CancellationToken);
            Assert.NotNull(study);
            study!.Description = "Updated";
            await _fixture.Persistence.Study.UpdateAsync(study, TestContext.Current.CancellationToken);
            Study? loaded = await _fixture.Persistence.Study.GetByStudyInstanceUidAsync(_fixture.StudyInstanceUid, TestContext.Current.CancellationToken);
            Assert.Equal("Updated", loaded!.Description);
        }

        [Fact, Priority(11)]
        public async Task Study_Should_Set_ModifiedAtUtc()
        {
            Study? study = await _fixture.Persistence!.Study!.GetByStudyInstanceUidAsync(_fixture.StudyInstanceUid, TestContext.Current.CancellationToken);
            Assert.NotNull(study);
            Assert.NotNull(study!.ModifiedAtUtc);
        }

        [Fact, Priority(12)]
        public async Task Study_Should_Load_Assigned_Series()
        {
            Study? study = await _fixture.Persistence!.Study!.GetByStudyInstanceUidAsync(_fixture.StudyInstanceUid, TestContext.Current.CancellationToken);
            Assert.NotNull(study);
            Assert.Single(study!.Series);
            Assert.Equal(_fixture.SeriesId, study.Series.First().Id);
        }
    }
}