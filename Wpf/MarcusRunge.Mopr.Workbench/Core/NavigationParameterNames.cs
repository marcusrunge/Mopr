namespace MarcusRunge.Mopr.Workbench.Core
{
    /// <summary>
    /// Defines application-wide Prism navigation parameter names.
    /// </summary>
    public static class NavigationParameterNames
    {
        /// <summary>
        /// Indicates that the MIRAS action-required view is hosted by an administrative workflow and must not release the imaging workbench directly.
        /// </summary>
        public const string MirasAdministrativeContext = nameof(MirasAdministrativeContext);
    }
}
