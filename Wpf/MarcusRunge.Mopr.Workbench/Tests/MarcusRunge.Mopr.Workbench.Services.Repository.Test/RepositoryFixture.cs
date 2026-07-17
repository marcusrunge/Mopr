using MarcusRunge.Mopr.Workbench.Contracts.Application;
using MarcusRunge.Mopr.Workbench.Services.Repository.Contracts;
using Microsoft.Extensions.Logging.Abstractions;
using System.Reactive.Subjects;

namespace MarcusRunge.Mopr.Workbench.Services.Repository.Test
{
    public sealed class RepositoryFixture : IAsyncLifetime
    {
        public IRepository? Repository { get; private set; }

        public async ValueTask InitializeAsync()
        {
            var applicationConfiguration = new TestApplicationConfiguration();
            BehaviorSubject<IApplicationConfiguration> applicationConfigurationSubject = new(applicationConfiguration);
            Repository = new RepositoryFactory(NullLogger.Instance, new TestApplicationLifetime(), applicationConfigurationSubject, null!).Create();
            await Task.CompletedTask;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}