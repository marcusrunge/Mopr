using MarcusRunge.Mopr.Workbench.Contracts.Application.Security.Services;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts;
using Microsoft.Extensions.Logging;

namespace MarcusRunge.Mopr.Workbench.Services.Application
{
    /// <summary>
    /// Defines a factory contract for creating a clean architecture module instance.
    /// </summary>
    public interface IApplicationFactory
    {
        /// <summary>
        /// Creates (or returns) a module instance.
        /// </summary>
        IApplication Create();
    }

    /// <summary>
    /// Default factory implementation that provides a factory and module instance.
    /// </summary>
    public class ApplicationFactory : IApplicationFactory
    {
        // Audit identity provider reference for potential auditing; can be null if not provided.
        private readonly IAuditIdentityProvider? _auditIdentityProvider;

        // Logger reference for potential logging; can be null if not provided.
        private readonly ILogger? _logger;

        // Persistence reference for potential data persistence; can be null if not provided.
        private readonly IPersistence? _persistence;

        // Repository reference for potential data repository access; can be null if not provided.
        private readonly Repository.Contracts.IRepository? _repository;

        // Stores the module instance created by this factory (lazy-created).
        private IApplication? _moduleInstance;

        public ApplicationFactory()
        {
        }

        public ApplicationFactory(ILogger? logger, IAuditIdentityProvider? auditIdentityProvider, IPersistence? persistence, Repository.Contracts.IRepository? repository)
        {
            _logger = logger;
            _auditIdentityProvider = auditIdentityProvider;
            _persistence = persistence;
            _repository = repository;
        }

        public ApplicationFactory(IAuditIdentityProvider? auditIdentityProvider, IPersistence? persistence, Repository.Contracts.IRepository? repository)
        {
            _auditIdentityProvider = auditIdentityProvider;
            _persistence = persistence;
            _repository = repository;
        }

        /// <inheritdoc/>
        public IApplication Create() =>
            /* What happens here:
               - Lazy initialization of the instance.
               - If _moduleInstance is null, a new Implementations.MarcusRunge.Mopr.Workbench.Services.Wpf is created and cached.
               - If it is already set, the cached module instance is returned.

               Purpose/intent:
               - Ensures consumers get a single shared module instance per process/app-domain-like context,
                 created on first demand. */
            _moduleInstance ??= new Implementations.Application(_logger, _auditIdentityProvider, _persistence, _repository);
    }
}