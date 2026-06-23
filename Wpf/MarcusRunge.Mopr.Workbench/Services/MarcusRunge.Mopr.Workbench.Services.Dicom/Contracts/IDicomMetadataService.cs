namespace MarcusRunge.Mopr.Workbench.Services.Dicom.Contracts
{
    public interface IDicomMetadataService
    {
        bool IsDicomFile(string filePath);
    }
}