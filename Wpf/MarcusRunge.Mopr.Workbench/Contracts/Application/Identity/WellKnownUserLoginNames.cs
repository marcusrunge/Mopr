namespace MarcusRunge.Mopr.Workbench.Contracts.Application.Identity
{
    /// <summary>
    /// Defines reserved operating-system login names used by MOPR infrastructure.
    /// </summary>
    public static class WellKnownUserLoginNames
    {
        /// <summary>
        /// Gets the reserved technical identity used to audit machine-wide setup operations.
        /// </summary>
        public const string System = @"MOPR\SYSTEM";
    }
}