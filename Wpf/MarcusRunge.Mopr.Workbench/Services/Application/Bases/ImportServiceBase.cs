using MarcusRunge.Mopr.Workbench.Services.Application.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Import;
using MarcusRunge.Mopr.Workbench.Services.Application.Implementations.Import;

namespace MarcusRunge.Mopr.Workbench.Services.Application.Bases
{
    internal class ImportServiceBase(IApplicationBase? applicationBase) : IImportServiceBase, IImportService
    {
        protected IDicomImportDriveProvider? _dicomImportDriveProvider;
        protected IDicomImportService? _dicomImportService;
        protected IDicomImportSourceResolver? _dicomImportSourceResolver;

        /// <inheritdoc/>
        IApplicationBase? IServiceBase.ApplicationBase => applicationBase;

        /// <inheritdoc/>
        IDicomImportDriveProvider? IImportServiceBase.DicomImportDriveProvider => _dicomImportDriveProvider;

        /// <inheritdoc/>
        public IDicomImportService? DicomImportService => _dicomImportService;

        /// <inheritdoc/>
        IDicomImportSourceResolver? IImportServiceBase.DicomImportSourceResolver => _dicomImportSourceResolver;
    }
}