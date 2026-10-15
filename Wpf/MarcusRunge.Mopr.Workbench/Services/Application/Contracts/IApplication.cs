using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Dialog;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Identity;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Import;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Media;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.RepositoryAdministration;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.UserAdministration;

namespace MarcusRunge.Mopr.Workbench.Services.Application.Contracts;

/// <summary>
/// Defines the public contract of the assembly.
/// </summary>
public interface IApplication
{
    event Action<Exception> ExceptionThrown;

    IDialogService? DialogService { get; }

    IIdentityService? IdentityService { get; }

    IImportService? ImportService { get; }

    IMediaService? MediaService { get; }

    IRepositoryLocationAdministrationService? RepositoryAdministration { get; }

    IUserAdministrationService? UserAdministration { get; }
}
