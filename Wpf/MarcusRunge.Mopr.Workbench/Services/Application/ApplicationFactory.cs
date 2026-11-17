using MarcusRunge.Mopr.Workbench.Contracts.Application.Administration.Services;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Configuration.Services;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Identity.Services;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Core.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Miras.Contracts;
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
    IApplication Create();
}

/// <summary>
/// Creates and owns one application-service graph.
/// </summary>
public sealed class ApplicationFactory : IApplicationFactory
{
    private readonly IAdministrativeAuthorizationService? _administrativeAuthorizationService;
    private readonly IImagingService? _imagingService;
    private readonly ILogger? _logger;
    private readonly IMachineConfigurationService _machineConfigurationService;
    private readonly IMiras _miras;
    private readonly IOperatingSystemIdentityProvider? _operatingSystemIdentityProvider;
    private readonly IPersistence? _persistence;
    private readonly Repository.Contracts.IRepository? _repository;
    private IApplication? _application;

    /// <summary>
    /// Initializes a factory with all supported application dependencies.
    /// </summary>
    public ApplicationFactory(ILogger? logger, IImagingService? imagingService, IAdministrativeAuthorizationService? administrativeAuthorizationService, IPersistence? persistence, Repository.Contracts.IRepository? repository, IOperatingSystemIdentityProvider? operatingSystemIdentityProvider, IMachineConfigurationService machineConfigurationService, IMiras miras)
    {
        _logger = logger;
        _imagingService = imagingService;
        _administrativeAuthorizationService = administrativeAuthorizationService;
        _persistence = persistence;
        _repository = repository;
        _operatingSystemIdentityProvider = operatingSystemIdentityProvider;
        _machineConfigurationService = machineConfigurationService ?? throw new ArgumentNullException(nameof(machineConfigurationService));
        _miras = miras ?? throw new ArgumentNullException(nameof(miras));
    }

    /// <summary>
    /// Initializes a factory with Core imaging and administrative authorization dependencies.
    /// </summary>
    public ApplicationFactory(IImagingService? imagingService, IAdministrativeAuthorizationService? administrativeAuthorizationService, IPersistence? persistence, Repository.Contracts.IRepository? repository, IOperatingSystemIdentityProvider? operatingSystemIdentityProvider, IMachineConfigurationService machineConfigurationService, IMiras miras) : this(logger: null, imagingService, administrativeAuthorizationService, persistence, repository, operatingSystemIdentityProvider, machineConfigurationService, miras)
    {
    }

    /// <summary>
    /// Initializes a factory with the legacy administrative dependency set.
    /// </summary>
    public ApplicationFactory(IAdministrativeAuthorizationService? administrativeAuthorizationService, IPersistence? persistence, Repository.Contracts.IRepository? repository, IOperatingSystemIdentityProvider? operatingSystemIdentityProvider, IMachineConfigurationService machineConfigurationService, IMiras miras) : this(logger: null, imagingService: null, administrativeAuthorizationService, persistence, repository, operatingSystemIdentityProvider, machineConfigurationService, miras)
    {
    }

    /// <summary>
    /// Initializes a factory with Core imaging and the existing runtime dependencies.
    /// </summary>
    public ApplicationFactory(IImagingService? imagingService, IPersistence? persistence, Repository.Contracts.IRepository? repository, IOperatingSystemIdentityProvider? operatingSystemIdentityProvider, IMachineConfigurationService machineConfigurationService, IMiras miras) : this(logger: null, imagingService, administrativeAuthorizationService: null, persistence, repository, operatingSystemIdentityProvider, machineConfigurationService, miras)
    {
    }

    /// <summary>
    /// Initializes a factory with the legacy runtime dependency set.
    /// </summary>
    public ApplicationFactory(IPersistence? persistence, Repository.Contracts.IRepository? repository, IOperatingSystemIdentityProvider? operatingSystemIdentityProvider, IMachineConfigurationService machineConfigurationService, IMiras miras) : this(logger: null, imagingService: null, administrativeAuthorizationService: null, persistence, repository, operatingSystemIdentityProvider, machineConfigurationService, miras)
    {
    }

    /// <summary>
    /// Initializes a factory with a logger, Core imaging and the existing runtime dependencies.
    /// </summary>
    public ApplicationFactory(ILogger? logger, IImagingService? imagingService, IPersistence? persistence, Repository.Contracts.IRepository? repository, IOperatingSystemIdentityProvider? operatingSystemIdentityProvider, IMachineConfigurationService machineConfigurationService, IMiras miras) : this(logger, imagingService, administrativeAuthorizationService: null, persistence, repository, operatingSystemIdentityProvider, machineConfigurationService, miras)
    {
    }

    /// <summary>
    /// Initializes a factory with the legacy logger dependency set.
    /// </summary>
    public ApplicationFactory(ILogger? logger, IPersistence? persistence, Repository.Contracts.IRepository? repository, IOperatingSystemIdentityProvider? operatingSystemIdentityProvider, IMachineConfigurationService machineConfigurationService, IMiras miras) : this(logger, imagingService: null, administrativeAuthorizationService: null, persistence, repository, operatingSystemIdentityProvider, machineConfigurationService, miras)
    {
    }

    /// <inheritdoc/>
    public IApplication Create() => _application ??= new Implementations.Application(_logger, _imagingService, _administrativeAuthorizationService, _operatingSystemIdentityProvider, _persistence, _repository, _machineConfigurationService, _miras);
}
