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
            _operations = Implementations.Operations.Create(this);
            _flow = Implementations.Flow.Create(this);            
        }
    }
}