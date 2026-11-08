using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Dialog;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Identity;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Import;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Media;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.RepositoryAdministration;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.RepositoryRecovery;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Studies;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.SystemCheck;

using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.UserAdministration;

namespace MarcusRunge.Mopr.Workbench.Services.Application.Contracts;

/// <summary>
/// Defines the public contract of the application-service assembly.
/// </summary>
public interface IApplication
{
    /// <summary>
    /// Occurs when an application-service exception is reported.
    /// </summary>
    event Action<Exception> ExceptionThrown;

    /// <summary>
    /// Gets the dialog service.
    /// </summary>
    IDialogService? DialogService { get; }

    /// <summary>
    /// Gets the identity service.
    /// </summary>
    IIdentityService? IdentityService { get; }

    /// <summary>
    /// Gets the read-only imported-study catalog service.
    /// </summary>
    IImportedSeriesLoadService? ImportedSeriesLoader { get; }

    /// <summary>
    /// Gets the import service.
    /// </summary>
    IImportService? ImportService { get; }

    /// <summary>
    /// Gets the media service.
    /// </summary>
    IMediaService? MediaService { get; }

    /// <summary>
    /// Gets the repository-location administration service.
    /// </summary>
    IRepositoryLocationAdministrationService? RepositoryAdministration { get; }

    /// <summary>
    /// Gets the repository recovery action service.
    /// </summary>
    IRepositoryRecoveryActionService? RepositoryRecovery { get; }

    /// <summary>
    /// Gets the system-check service.
    /// </summary>
    IStudyCatalogService? StudyCatalog { get; }

    /// <summary>
    /// Gets the system-check service.
    /// </summary>
    ISystemCheckService? SystemCheck { get; }

    /// <summary>
    /// Gets the user administration service.
    /// </summary>
    IUserAdministrationService? UserAdministration { get; }
}