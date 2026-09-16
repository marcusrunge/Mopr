using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Dialog;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Identity;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Import;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Media;

namespace MarcusRunge.Mopr.Workbench.Services.Application.Contracts
{
    /// <summary>
    /// Defines the public contract of the assembly.
    /// </summary>
    public interface IApplication
    {
        /// <summary>
        /// Occurs when an exception is thrown.
        /// </summary>
        event Action<Exception> ExceptionThrown;

        /// <summary>
        /// Gets the IDialogService instance exposed by the assembly, if available.
        /// </summary>
        IDialogService? DialogService { get; }

        /// <summary>
        /// Gets the IIdentityService instance exposed by the assembly, if available.
        /// </summary>
        IIdentityService? IdentityService { get; }

        /// <summary>
        /// Gets the IImportService instance exposed by the assembly, if available.
        /// </summary>
        IImportService? ImportService { get; }

        /// <summary>
        /// Gets the IMediaService instance exposed by the assembly, if available.
        /// </summary>
        IMediaService? MediaService { get; }
    }
}