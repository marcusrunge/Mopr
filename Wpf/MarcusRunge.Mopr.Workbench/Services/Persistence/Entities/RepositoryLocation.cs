using MarcusRunge.Base;

namespace MarcusRunge.Mopr.Workbench.Services.Persistence.Entities
{
    /// <summary>
    /// Represents a configured physical repository location for DICOM files.
    /// </summary>
    public class RepositoryLocation : AuditableEntityBase
    {
        private bool _isDefault, _isEnabled = true;
        private string? _name, _rootPath;

        /// <summary>
        /// Gets the DICOM instances stored in this repository location.
        /// </summary>
        public ICollection<Instance> Instances { get; set; } = new HashSet<Instance>();

        /// <summary>
        /// Gets or sets a value indicating whether this location is the default
        /// target for imports without an explicitly selected destination.
        /// </summary>
        public bool IsDefault { get => _isDefault; set => SetProperty(ref _isDefault, value); }

        /// <summary>
        /// Gets or sets a value indicating whether this repository location
        /// may currently be selected for new imports.
        /// </summary>
        public bool IsEnabled { get => _isEnabled; set => SetProperty(ref _isEnabled, value); }

        /// <summary>
        /// Gets or sets the user-facing name of the repository location.
        /// </summary>
        public string? Name { get => _name; set => SetProperty(ref _name, value); }

        /// <summary>
        /// Gets or sets the absolute local or UNC root path of the repository.
        /// </summary>
        public string? RootPath { get => _rootPath; set => SetProperty(ref _rootPath, value); }
    }
}