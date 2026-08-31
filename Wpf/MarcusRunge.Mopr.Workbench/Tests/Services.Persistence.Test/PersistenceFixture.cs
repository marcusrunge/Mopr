using MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts;
using System.Reactive.Subjects;

namespace MarcusRunge.Mopr.Workbench.Services.Persistence.Test
{
    public sealed class PersistenceFixture : IAsyncLifetime
    {
        private TestApplicationLifetime? _applicationLifetime;
        private BehaviorSubject<PersistenceConfiguration>? _configurationSubject;

        public int InstanceId { get; set; }

        public int MeasurementId { get; set; }

        public IPersistence? Persistence { get; private set; }

        public int RepositoryLocationId { get; set; }

        public string RepositoryLocationRootPath { get; } = Path.Combine(Path.GetTempPath(), "MoprPersistenceTests", Guid.NewGuid().ToString("N"));

        public int SeriesId { get; set; }

        public string SeriesInstanceUid { get; } = Guid.NewGuid().ToString();

        public string SopInstanceUid { get; } = Guid.NewGuid().ToString();

        public int StudyId { get; set; }

        public string StudyInstanceUid { get; } = Guid.NewGuid().ToString();

        public int UserId { get; set; }

        /// <inheritdoc/>
        public ValueTask DisposeAsync()
        {
            _configurationSubject?.Dispose();
            _applicationLifetime?.Dispose();

            if (Directory.Exists(RepositoryLocationRootPath))
            {
                Directory.Delete(RepositoryLocationRootPath, recursive: true);
            }

            return ValueTask.CompletedTask;
        }

        /// <inheritdoc/>
        public async ValueTask InitializeAsync()
        {
            _applicationLifetime = new TestApplicationLifetime();

            _configurationSubject = new BehaviorSubject<PersistenceConfiguration>(new PersistenceConfiguration
            {
                Mode = PersistenceMode.InMemory,
                ConnectionString = Guid.NewGuid().ToString("N")
            });

            Persistence = new PersistenceFactory(_applicationLifetime, _configurationSubject).Create();

            await Persistence.Initialization.WaitAsync(TestContext.Current.CancellationToken);
        }
    }
}