using MarcusRunge.Base;
using MarcusRunge.Mopr.Workbench.Services.Dicom.Contracts;
using System.Threading;
using System.Threading.Tasks;

namespace MarcusRunge.Mopr.Workbench.Services.Dicom.Implementations
{
    internal sealed class DicomImportService : CreateableBindableBase<IDicomImportService, DicomImportService, IDicomBase>, IDicomImportService
    {
        private IDicomBase? _base;

        protected override void OnCreate(IDicomBase @base) => _base = @base;

        protected override Task OnCreateAsync(IDicomBase @base, CancellationToken cancellationToken)
        {
            _base = @base;

            return Task.CompletedTask;
        }
    }
}