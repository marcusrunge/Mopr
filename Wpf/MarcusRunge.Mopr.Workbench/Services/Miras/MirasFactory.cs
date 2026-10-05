using MarcusRunge.Mopr.Workbench.Contracts.Application.Administration.Services;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Configuration.Services;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Lifetime.Services;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Security.Services;
using MarcusRunge.Mopr.Workbench.Services.Miras.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Repository.Contracts;
using Microsoft.Extensions.Logging;

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
    /// <remarks>
    /// Initializes a new instance of the <see cref="MirasFactory"/> class.
    /// </remarks>
    public sealed class MirasFactory(ILogger? logger, ILifetimeService? applicationLifetime, IPersistence persistence, IRepository repository, IAdministrativeAuthorizationService administrativeAuthorizationService, IRepositoryLocationValidationService repositoryLocationValidationService, ISystemAuditIdentityProvider systemAuditIdentityProvider) : IMirasFactory
    {
        private readonly IAdministrativeAuthorizationService _administrativeAuthorizationService = administrativeAuthorizationService ?? throw new ArgumentNullException(nameof(administrativeAuthorizationService));
        private readonly ILifetimeService? _applicationLifetime = applicationLifetime;
        private readonly ILogger? _logger = logger;
        private readonly IPersistence _persistence = persistence ?? throw new ArgumentNullException(nameof(persistence));
        private readonly IRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        private readonly IRepositoryLocationValidationService _repositoryLocationValidationService = repositoryLocationValidationService ?? throw new ArgumentNullException(nameof(repositoryLocationValidationService));
        private readonly ISystemAuditIdentityProvider _systemAuditIdentityProvider = systemAuditIdentityProvider ?? throw new ArgumentNullException(nameof(systemAuditIdentityProvider));
        private IMiras? _moduleInstance;

        /// <summary>
        /// Initializes a new instance of the <see cref="MirasFactory"/> class.
        /// </summary>
        public MirasFactory(ILifetimeService? applicationLifetime, IPersistence persistence, IRepository repository, IAdministrativeAuthorizationService administrativeAuthorizationService, IRepositoryLocationValidationService repositoryLocationValidationService, ISystemAuditIdentityProvider systemAuditIdentityProvider) : this(logger: null, applicationLifetime, persistence, repository, administrativeAuthorizationService, repositoryLocationValidationService, systemAuditIdentityProvider)
        {
        }

        /// <inheritdoc/>
        public IMiras Create() => _moduleInstance ??= new Implementations.Miras(_logger, _applicationLifetime, _persistence, _repository, _administrativeAuthorizationService, _repositoryLocationValidationService, _systemAuditIdentityProvider);
    }
}