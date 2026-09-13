using MarcusRunge.Mopr.Workbench.Services.Application.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Import;

namespace MarcusRunge.Mopr.Workbench.Services.Application.Bases
{
    internal class ImportServiceBase(IApplicationBase? applicationBase) : IImportServiceBase, IImportService
    {
        protected IDicomImportService? _dicomImportService;

        /// <inheritdoc/>
        public IDicomImportService? DicomImportService => _dicomImportService;
                
        IApplicationBase? IServiceBase.ApplicationBase => applicationBase;
    }
}