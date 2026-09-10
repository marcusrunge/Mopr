using MarcusRunge.Mopr.Workbench.Contracts.Application.Lifetime.Services;
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
            MirasStaticState.Reset();

            ApplicationLifetime = new TestApplicationLifetime();
            Operations = new Mock<IOperations>(MockBehavior.Strict);

            var mirasBase = new TestMirasBase(ApplicationLifetime, Operations.Object);
            Flow = MirasFlow.Create(mirasBase);
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

            // Remove static references before disposing the lifetime owned by this context.
            MirasStaticState.Reset();
            ApplicationLifetime.Dispose();

            GC.SuppressFinalize(this);
        }

        private sealed class TestMirasBase : IMirasBase
        {
            public TestMirasBase(ILifetimeService applicationLifetime, IOperations operations)
            {
                ApplicationLifetime = applicationLifetime ?? throw new ArgumentNullException(nameof(applicationLifetime));
                Operations = operations ?? throw new ArgumentNullException(nameof(operations));
            }

            public ILifetimeService? ApplicationLifetime { get; }

            public ILogger? Logger => null;

            public IOperations? Operations { get; }

            public IPersistence? Persistence => null;

            public IRepository? Repository => null;

            public void OnExceptionThrown(Exception exception) => ArgumentNullException.ThrowIfNull(exception);
        }
    }
}