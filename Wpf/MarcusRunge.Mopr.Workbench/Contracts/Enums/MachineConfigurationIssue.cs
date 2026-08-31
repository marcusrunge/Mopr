namespace MarcusRunge.Mopr.Workbench.Contracts.Enums
{
    /// <summary>
    /// Identifies a structural issue in the machine-wide MOPR configuration.
    /// </summary>
    public enum MachineConfigurationIssue
    {
        /// <summary>
        /// The setup version is invalid or unsupported.
        /// </summary>
        InvalidSetupVersion = 0,

        /// <summary>
        /// The database connection string is missing.
        /// </summary>
        DatabaseConnectionStringMissing = 1,

        /// <summary>
        /// The configuration is not marked as fully configured.
        /// </summary>
        SetupNotCompleted = 2
    }
}