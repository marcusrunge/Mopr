using MarcusRunge.Mopr.Workbench.Services.Application.Bases;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Import;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Media;
using MarcusRunge.Mopr.Workbench.Services.Application.Implementations.Media;

namespace MarcusRunge.Mopr.Workbench.Services.Application.Implementations.Import
{
    internal class ImportService : ImportServiceBase
    {
        internal ImportService(IApplicationBase? applicationBase) : base(applicationBase)
        {
            _dicomImportService = Import.DicomImportService.Create(this);
        }

        internal static IImportService? Create(IApplicationBase? applicationBase) => applicationBase is null ? null : new ImportService(applicationBase);
    }
}
