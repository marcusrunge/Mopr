using MarcusRunge.Base;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Administration.Services;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Configuration.Services;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Lifetime.Services;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Security.Services;
using MarcusRunge.Mopr.Workbench.Services.Miras.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Repository.Contracts;
using Microsoft.Extensions.Logging;
using Moq;
using MirasFlow = MarcusRunge.Mopr.Workbench.Services.Miras.Implementations.Flow;

namespace MarcusRunge.Mopr.Workbench.Services.Miras.Test
{
    internal sealed class MirasFlowServiceTestContext : IDisposable
    {
        private bool _disposed;

        public MirasFlowServiceTestContext()
        {
            ApplicationLifetime = new TestApplicationLifetime();
            Operations = new Mock<IOperations>(MockBehavior.Strict);

            var mirasBase = new TestMirasBase(ApplicationLifetime, Operations.Object);
            Flow = MirasFlow.Create(mirasBase, CreationLifetime.Scoped);
        }

        public TestApplicationLifetime ApplicationLifetime { get; }

        public IFlow Flow { get; }

        public Mock<IOperations> Operations { get; }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            ApplicationLifetime.Dispose();
            GC.SuppressFinalize(this);
        }

        private sealed class TestMirasBase(ILifetimeService applicationLifetime, IOperations operations) : IMirasBase
        {
            public IAdministrativeAuthorizationService AdministrativeAuthorizationService => throw CreateUnavailableDependencyException(nameof(AdministrativeAuthorizationService));

            public ILifetimeService? LifetimeService { get; } = applicationLifetime ?? throw new ArgumentNullException(nameof(applicationLifetime));

            public ILogger? Logger => null;

            public IOperations? Operations { get; } = operations ?? throw new ArgumentNullException(nameof(operations));

            public IPersistence Persistence => throw CreateUnavailableDependencyException(nameof(Persistence));

            public IRepository Repository => throw CreateUnavailableDependencyException(nameof(Repository));

            public IRepositoryInfrastructureRecoveryService? RepositoryInfrastructureRecovery => null;

            public IRepositoryLocationValidationService RepositoryLocationValidationService => throw CreateUnavailableDependencyException(nameof(RepositoryLocationValidationService));

            public ISystemAuditIdentityProvider SystemAuditIdentityProvider => throw CreateUnavailableDependencyException(nameof(SystemAuditIdentityProvider));

            public void OnExceptionThrown(Exception exception) => ArgumentNullException.ThrowIfNull(exception);

            private static InvalidOperationException CreateUnavailableDependencyException(string dependencyName) => new($"'{dependencyName}' is not available in the isolated MIRAS flow test context.");
        }
    }
}