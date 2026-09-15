using MarcusRunge.Base;
using MarcusRunge.Mopr.Workbench.Services.Application.Bases;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Import;

namespace MarcusRunge.Mopr.Workbench.Services.Application.Implementations.Import
{
    /// <summary>
    /// Composes the import services owned by one application import graph.
    /// </summary>
    internal sealed class ImportService : ImportServiceBase
    {
        internal ImportService(IApplicationBase? applicationBase) : base(applicationBase)
        {
            // Services are created in dependency order so every consumer observes
            // a completely composed import graph during initialization and use.
            _dicomImportDriveProvider = DicomImportDriveProvider.Create(this, CreationLifetime.Scoped);
            _dicomImportSourceResolver = DicomImportSourceResolver.Create(this, CreationLifetime.Scoped);
            _dicomImportService = Import.DicomImportService.Create(this, CreationLifetime.Scoped);
        }

        internal static IImportService? Create(IApplicationBase? applicationBase) => applicationBase is null ? null : new ImportService(applicationBase);
    }
}