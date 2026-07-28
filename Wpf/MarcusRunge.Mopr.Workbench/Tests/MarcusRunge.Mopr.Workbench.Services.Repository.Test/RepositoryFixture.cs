using MarcusRunge.Mopr.Workbench.Contracts.Application;
using MarcusRunge.Mopr.Workbench.Services.Persistence;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Entities;
using MarcusRunge.Mopr.Workbench.Services.Repository.Contracts;
using Microsoft.Extensions.Logging.Abstractions;
using System.Reactive.Subjects;

namespace MarcusRunge.Mopr.Workbench.Services.Repository.Test
{
    public sealed class RepositoryFixture : IAsyncLifetime
    {
        private BehaviorSubject<IApplicationConfiguration>? _applicationConfigurationSubject;
        private BehaviorSubject<PersistenceConfiguration>? _persistenceConfigurationSubject;
        public IPersistence? Persistence { get; private set; }
        public IRepository? Repository { get; private set; }
        public RepositoryLocation? RepositoryLocation { get; private set; }

        public string RepositoryRootPath { get; } = Path.Combine(Path.GetTempPath(), "MoprRepositoryTests", Guid.NewGuid().ToString("N"));
        public User? TestUser { get; private set; }

        public ValueTask DisposeAsync()
        {
            _applicationConfigurationSubject?.Dispose();
            _persistenceConfigurationSubject?.Dispose();

            if (Directory.Exists(RepositoryRootPath))
            {
                Directory.Delete(RepositoryRootPath, true);
            }

            return ValueTask.CompletedTask;
        }

        public async ValueTask InitializeAsync()
        {
            _persistenceConfigurationSubject = new BehaviorSubject<PersistenceConfiguration>(
                new PersistenceConfiguration
                {
                    Mode = PersistenceMode.InMemory,
                    ConnectionString = Guid.NewGuid().ToString("N")
                });

            Persistence = new PersistenceFactory(new TestApplicationLifetime(), _persistenceConfigurationSubject).Create();

            IUserRepository userRepository = Persistence.User ?? throw new InvalidOperationException("The user repository has not been initialized.");

            TestUser = new User
            {
                LoginName = $"RepositoryTest_{Guid.NewGuid():N}",
                ShortName = "Repository Test"
            };

            await userRepository.AddAsync(TestUser, TestContext.Current.CancellationToken);

            if (TestUser.Id <= 0)
            {
                throw new InvalidOperationException("The repository test user could not be persisted.");
            }

            IRepositoryLocationRepository repositoryLocationRepository = Persistence.RepositoryLocation ?? throw new InvalidOperationException("The repository-location repository has not been initialized.");

            RepositoryLocation = new RepositoryLocation
            {
                Name = "Repository Integration Test Location",
                RootPath = RepositoryRootPath,
                IsEnabled = true,
                IsDefault = true,
                CreatedByUserId = TestUser.Id
            };

            await repositoryLocationRepository.AddAsync(RepositoryLocation, TestContext.Current.CancellationToken);

            if (RepositoryLocation.Id <= 0)
            {
                throw new InvalidOperationException("The repository test location could not be persisted.");
            }

            Directory.CreateDirectory(RepositoryLocation.RootPath!);

            TestApplicationConfiguration applicationConfiguration = new();

            _applicationConfigurationSubject = new BehaviorSubject<IApplicationConfiguration>(applicationConfiguration);

            Repository = new RepositoryFactory(NullLogger.Instance, new TestApplicationLifetime(), _applicationConfigurationSubject, Persistence).Create();
        }
    }
}