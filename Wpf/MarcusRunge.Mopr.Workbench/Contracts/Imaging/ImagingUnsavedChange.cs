namespace MarcusRunge.Mopr.Workbench.Contracts.Imaging
{
    /// <summary>
    /// Describes one persistable imaging change that has not been saved yet.
    /// </summary>
    public sealed record ImagingUnsavedChange(string Source, string Description);
}