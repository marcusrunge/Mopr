using MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts;
using System.Reactive.Subjects;

namespace MarcusRunge.Mopr.Workbench.Services.Persistence.Test
{
    public sealed class PersistenceFixture : IAsyncLifetime
    {
        public int InstanceId { get; set; }
        public int MeasurementId { get; set; }
        public int RepositoryLocationId { get; set; }
        public IPersistence? Persistence { get; private set; }
        public string RepositoryLocationRootPath { get; } = Path.Combine(Path.GetTempPath(), "MoprPersistenceTests", Guid.NewGuid().ToString("N"));
        public int SeriesId { get; set; }
        public string SeriesInstanceUid { get; } = Guid.NewGuid().ToString();
        public string SopInstanceUid { get; } = Guid.NewGuid().ToString();
        public int StudyId { get; set; }
        public string StudyInstanceUid { get; } = Guid.NewGuid().ToString();
        public int UserId { get; set; }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        public async ValueTask InitializeAsync()
        {
            BehaviorSubject<PersistenceConfiguration> configurationSubject = new(new PersistenceConfiguration
            {
                Mode = PersistenceMode.InMemory,
                ConnectionString = Guid.NewGuid().ToString("N")
            });
            Persistence = new PersistenceFactory(new TestApplicationLifetime(), configurationSubject).Create();
            await Task.CompletedTask;
        }
    }
}