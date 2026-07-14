namespace MarcusRunge.Mopr.Workbench.Services.Dicom.Contracts
{
    public interface IDicom
    {
        IDicomImageService? ImageService { get; }
        IDicomImportService? ImportService { get; }
        IDicomMetadataService? MetadataService { get; }
    }
}