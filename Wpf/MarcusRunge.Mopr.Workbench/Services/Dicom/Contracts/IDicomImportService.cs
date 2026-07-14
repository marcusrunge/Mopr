using System.Threading;
using System.Threading.Tasks;

namespace MarcusRunge.Mopr.Workbench.Services.Dicom.Contracts
{
    public interface IDicomImportService
    {
        Task<DicomImportResult?> ImportFolderAsync(string folderPath, CancellationToken cancellationToken = default);
    }
}