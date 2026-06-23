namespace MarcusRunge.Mopr.Workbench.Services.Dicom.Contracts
{
    public interface IDicom
    {
        IDicomImportService? ImportService { get; }
        IDicomMetadataService? MetadataService { get; }
    }
}