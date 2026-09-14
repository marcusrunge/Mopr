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
            // This ImportService instance supplies all dependencies and forms the stable
            // scope identity. Independent application graphs must never share their
            // DICOM import facade or its captured application context.
            _dicomImportService = Import.DicomImportService.Create(this, CreationLifetime.Scoped);
        }

        internal static IImportService? Create(IApplicationBase? applicationBase) => applicationBase is null ? null : new ImportService(applicationBase);
    }
}