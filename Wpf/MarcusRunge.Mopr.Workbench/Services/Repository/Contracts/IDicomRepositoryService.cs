namespace MarcusRunge.Mopr.Workbench.Services.Repository.Contracts
{
    public interface IDicomRepositoryService
    {
        string CreateRelativePath(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid);

        bool Exists(string relativePath);

        string GetAbsolutePath(string relativePath);
    }
}