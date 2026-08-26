using MarcusRunge.Mopr.Workbench.Contracts.Application.Configuration;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Lifetime;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Repository.Contracts;
using Microsoft.Extensions.Logging;

namespace MarcusRunge.Mopr.Workbench.Services.Repository
{
    /// <summary>
    /// Defines a factory contract for creating a repository module instance.
    /// </summary>
    public interface IRepositoryFactory
    {
        /// <summary>
        /// Creates or returns the module instance owned by this factory.
        /// </summary>
        IRepository Create();
    }

    /// <summary>
    /// Creates and retains one repository module instance per factory.
    /// </summary>
    public sealed class RepositoryFactory : IRepositoryFactory
    {
        private readonly IObservable<IApplicationConfiguration>? _applicationConfigurationObservable;
        private readonly IApplicationLifetime? _applicationLifetime;
        private readonly ILogger? _logger;
        private readonly IPersistence _persistence;
        private IRepository? _moduleInstance;

        public RepositoryFactory(IApplicationLifetime? applicationLifetime, IObservable<IApplicationConfiguration>? applicationConfigurationObservable, IPersistence persistence)
        {
            _applicationLifetime = applicationLifetime;
            _applicationConfigurationObservable = applicationConfigurationObservable;
            _persistence = persistence ?? throw new ArgumentNullException(nameof(persistence));
        }

        public RepositoryFactory(ILogger? logger, IApplicationLifetime? applicationLifetime, IObservable<IApplicationConfiguration>? applicationConfigurationObservable, IPersistence persistence)
        {
            _logger = logger;
            _applicationLifetime = applicationLifetime;
            _applicationConfigurationObservable = applicationConfigurationObservable;
            _persistence = persistence ?? throw new ArgumentNullException(nameof(persistence));
        }

        /// <inheritdoc/>
        public IRepository Create()
        {
            // The factory retains one repository instance. The composition root controls
            // the lifetime of the factory and therefore the lifetime of the module.
            return _moduleInstance ??= new Implementations.Repository(_logger, _applicationLifetime, _applicationConfigurationObservable, _persistence);
        }
    }
}