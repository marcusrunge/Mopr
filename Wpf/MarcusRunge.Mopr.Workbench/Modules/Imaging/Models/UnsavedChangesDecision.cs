namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.Models
{
    /// <summary>
    /// Represents the user's decision for pending imaging-session changes.
    /// </summary>
    public enum UnsavedChangesDecision
    {
        Cancel = 0,
        Save = 1,
        Discard = 2
    }
}