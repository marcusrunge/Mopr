using MarcusRunge.Base;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Lifetime.Services;
using MarcusRunge.Mopr.Workbench.Services.Miras.Bases;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Repository.Contracts;
using Microsoft.Extensions.Logging;

namespace MarcusRunge.Mopr.Workbench.Services.Miras.Implementations
{
    /// <summary>
    /// Composes the services owned by one MIRAS module instance.
    /// </summary>
    internal sealed class Miras : MirasBase
    {
        internal Miras(ILogger? logger, ILifetimeService? applicationLifetime, IPersistence persistence, IRepository repository) : base(logger, applicationLifetime, persistence, repository)
        {
            // Each MIRAS graph must retain its own lifetime, persistence and repository
            // dependencies. The MIRAS root is therefore the stable scope identity for
            // both operations and flow services.
            _operations = Implementations.Operations.Create(this, CreationLifetime.Scoped);
            _flow = Implementations.Flow.Create(this, CreationLifetime.Scoped);
        }
    }
}