using MarcusRunge.Mopr.Workbench.Modules.Identity.Properties;
using Prism.Mvvm;

namespace MarcusRunge.Mopr.Workbench.Modules.Identity.ViewModels
{
    /// <summary>
    /// Presents a protected startup state when MIRAS cannot confirm that
    /// the imaging workbench may be opened safely.
    /// </summary>
    public sealed class MirasActionRequiredViewModel : BindableBase
    {
        /// <summary>
        /// Gets the user-oriented action description.
        /// </summary>
        public string Description => Resources.MirasActionRequiredDescription;

        /// <summary>
        /// Gets the user-oriented status title.
        /// </summary>
        public string Title => Resources.MirasActionRequiredTitle;
    }
}