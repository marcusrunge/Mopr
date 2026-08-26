using MarcusRunge.Mopr.Workbench.Contracts.Application.Lifetime;
using MarcusRunge.Mopr.Workbench.Services.Miras.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Repository.Contracts;
using Microsoft.Extensions.Logging;
using System;

namespace MarcusRunge.Mopr.Workbench.Services.Miras
{
    /// <summary>
    /// Defines a factory contract for creating a MIRAS module instance.
    /// </summary>
    public interface IMirasFactory
    {
        /// <summary>
        /// Creates or returns the module instance owned by this factory.
        /// </summary>
        IMiras Create();
    }

    /// <summary>
    /// Creates and retains one MIRAS module instance per factory.
    /// </summary>
    public sealed class MirasFactory : IMirasFactory
    {
        private readonly IApplicationLifetime? _applicationLifetime;
        private readonly ILogger? _logger;
        private readonly IPersistence _persistence;
        private readonly IRepository _repository;
        private IMiras? _moduleInstance;

        public MirasFactory(IApplicationLifetime? applicationLifetime, IPersistence persistence, IRepository repository)
        {
            _applicationLifetime = applicationLifetime;
            _persistence = persistence ?? throw new ArgumentNullException(nameof(persistence));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public MirasFactory(ILogger? logger, IApplicationLifetime? applicationLifetime, IPersistence persistence, IRepository repository)
        {
            _logger = logger;
            _applicationLifetime = applicationLifetime;
            _persistence = persistence ?? throw new ArgumentNullException(nameof(persistence));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        /// <inheritdoc/>
        public IMiras Create()
        {
            // MIRAS remains stable within one factory. Separate composition roots receive
            // independent module instances with their corresponding dependencies.
            return _moduleInstance ??= new Implementations.Miras(_logger, _applicationLifetime, _persistence, _repository);
        }
    }
}