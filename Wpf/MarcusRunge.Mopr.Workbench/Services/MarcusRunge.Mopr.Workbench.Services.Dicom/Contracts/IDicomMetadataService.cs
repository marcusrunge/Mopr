using System.Threading;
using System.Threading.Tasks;

namespace MarcusRunge.Mopr.Workbench.Services.Dicom.Contracts
{
    public interface IDicomMetadataService
    {
        bool IsDicomFile(string filePath);

        Task<DicomFileMetadata?> ReadMetadataAsync(string filePath, CancellationToken cancellationToken = default);
    }
}