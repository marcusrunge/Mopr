using MarcusRunge.Mopr.Workbench.Contracts.Application.Administration.Services;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Configuration.Services;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Identity.Services;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts;
using Microsoft.Extensions.Logging;

namespace MarcusRunge.Mopr.Workbench.Services.Application;

/// <summary>
/// Creates an application-service graph.
/// </summary>
public interface IApplicationFactory
{
    /// <summary>
    /// Creates or returns the application-service instance owned by this factory.
    /// </summary>
    /// <returns>The application-service instance.</returns>
    IApplication Create();
}

/// <summary>
/// Creates and owns one application-service graph.
/// </summary>
public sealed class ApplicationFactory : IApplicationFactory
{
    private readonly IAdministrativeAuthorizationService? _administrativeAuthorizationService;
    private readonly ILogger? _logger;
    private readonly IMachineConfigurationService _machineConfigurationService;
    private readonly IOperatingSystemIdentityProvider? _operatingSystemIdentityProvider;
    private readonly IPersistence? _persistence;
    private readonly Repository.Contracts.IRepository? _repository;
    private IApplication? _application;

    /// <summary>
    /// Initializes a factory without external application dependencies.
    /// </summary>
    /// <summary>
    /// Initializes a factory with all supported application dependencies.
    /// </summary>
    /// <param name="logger">The optional application logger.</param>
    /// <param name="administrativeAuthorizationService">The optional administrative authorization service.</param>
    /// <param name="persistence">The optional Persistence service.</param>
    /// <param name="repository">The optional Repository service.</param>
    /// <param name="operatingSystemIdentityProvider">The optional operating-system identity provider.</param>
    /// <param name="machineConfigurationService">The required machine-configuration service.</param>
    public ApplicationFactory(ILogger? logger, IAdministrativeAuthorizationService? administrativeAuthorizationService, IPersistence? persistence, Repository.Contracts.IRepository? repository, IOperatingSystemIdentityProvider? operatingSystemIdentityProvider, IMachineConfigurationService machineConfigurationService)
    {
        _logger = logger;
        _administrativeAuthorizationService = administrativeAuthorizationService;
        _persistence = persistence;
        _repository = repository;
        _operatingSystemIdentityProvider = operatingSystemIdentityProvider;
        _machineConfigurationService = machineConfigurationService ?? throw new ArgumentNullException(nameof(machineConfigurationService));
    }

    /// <summary>
    /// Initializes a factory with the runtime dependencies required by application services.
    /// </summary>
    /// <param name="administrativeAuthorizationService">The optional administrative authorization service.</param>
    /// <param name="persistence">The optional Persistence service.</param>
    /// <param name="repository">The optional Repository service.</param>
    /// <param name="operatingSystemIdentityProvider">The optional operating-system identity provider.</param>
    /// <param name="machineConfigurationService">The required machine-configuration service.</param>
    public ApplicationFactory(IAdministrativeAuthorizationService? administrativeAuthorizationService, IPersistence? persistence, Repository.Contracts.IRepository? repository, IOperatingSystemIdentityProvider? operatingSystemIdentityProvider, IMachineConfigurationService machineConfigurationService) : this(logger: null, administrativeAuthorizationService, persistence, repository, operatingSystemIdentityProvider, machineConfigurationService)
    {
    }

    /// <summary>
    /// Initializes a factory with logging and the existing application dependencies.
    /// </summary>
    /// <param name="logger">The optional application logger.</param>
    /// <param name="persistence">The optional Persistence service.</param>
    /// <param name="repository">The optional Repository service.</param>
    /// <param name="operatingSystemIdentityProvider">The optional operating-system identity provider.</param>
    /// <param name="machineConfigurationService">The required machine-configuration service.</param>
    public ApplicationFactory(ILogger? logger, IPersistence? persistence, Repository.Contracts.IRepository? repository, IOperatingSystemIdentityProvider? operatingSystemIdentityProvider, IMachineConfigurationService machineConfigurationService) : this(logger, administrativeAuthorizationService: null, persistence, repository, operatingSystemIdentityProvider, machineConfigurationService)
    {
    }

    /// <summary>
    /// Initializes a factory with the existing runtime dependencies.
    /// </summary>
    /// <param name="persistence">The optional Persistence service.</param>
    /// <param name="repository">The optional Repository service.</param>
    /// <param name="operatingSystemIdentityProvider">The optional operating-system identity provider.</param>
    /// <param name="machineConfigurationService">The required machine-configuration service.</param>
    public ApplicationFactory(IPersistence? persistence, Repository.Contracts.IRepository? repository, IOperatingSystemIdentityProvider? operatingSystemIdentityProvider, IMachineConfigurationService machineConfigurationService) : this(logger: null, administrativeAuthorizationService: null, persistence, repository, operatingSystemIdentityProvider, machineConfigurationService)
    {
    }

    /// <inheritdoc/>
    public IApplication Create() => _application ??= new Implementations.Application(_logger, _administrativeAuthorizationService, _operatingSystemIdentityProvider, _persistence, _repository, _machineConfigurationService);
}