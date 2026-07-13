namespace MarcusRunge.Mopr.Workbench.Contracts.Application
{
    public interface IRepositoryConfiguration
    {
        bool AutomaticallyRepairPaths { get; }
        string DicomRepositoryPath { get; }
    }
}