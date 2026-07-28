using MarcusRunge.Base;

namespace MarcusRunge.Mopr.Workbench.Services.Persistence.Entities
{
    /// <summary>
    /// Represents a persisted user within the MOPR system.
    /// </summary>
    public class User : BindableEntityBase
    {
        private string? _firstName, _lastName, _loginName, _middleName, _shortName, _suffix, _title;

        /// <summary>
        /// Gets or sets the collection of instances created by the user.
        /// </summary>
        public ICollection<Instance> CreatedInstances { get; set; } = new HashSet<Instance>();

        /// <summary>
        /// Gets or sets the collection of measurements created by the user.
        /// </summary>
        public ICollection<Measurement> CreatedMeasurements { get; set; } = new HashSet<Measurement>();

        /// <summary>
        /// Gets or sets the repository locations created by the user.
        /// </summary>
        public ICollection<RepositoryLocation> CreatedRepositoryLocations { get; set; } = new HashSet<RepositoryLocation>();

        /// <summary>
        /// Gets or sets the collection of series created by the user.
        /// </summary>
        public ICollection<Series> CreatedSeries { get; set; } = new HashSet<Series>();

        /// <summary>
        /// Gets or sets the collection of studies created by the user.
        /// </summary>
        public ICollection<Study> CreatedStudies { get; set; } = new HashSet<Study>();

        /// <summary>
        /// Gets or sets the collection of unreal objects created by the user.
        /// </summary>
        public ICollection<UnrealObject> CreatedUnrealObjects { get; set; } = new HashSet<UnrealObject>();

        /// <summary>
        /// Gets or sets the first name.
        /// </summary>
        public string? FirstName { get => _firstName; set => SetProperty(ref _firstName, value); }

        /// <summary>
        /// Gets or sets the last name.
        /// </summary>
        public string? LastName { get => _lastName; set => SetProperty(ref _lastName, value); }

        /// <summary>
        /// Gets or sets the login name.
        /// </summary>
        public string? LoginName { get => _loginName; set => SetProperty(ref _loginName, value); }

        /// <summary>
        /// Gets or sets the middle name.
        /// </summary>
        public string? MiddleName { get => _middleName; set => SetProperty(ref _middleName, value); }

        /// <summary>
        /// Gets or sets the collection of instances modified by the user.
        /// </summary>
        public ICollection<Instance> ModifiedInstances { get; set; } = new HashSet<Instance>();

        /// <summary>
        /// Gets or sets the collection of measurements modified by the user.
        /// </summary>
        public ICollection<Measurement> ModifiedMeasurements { get; set; } = new HashSet<Measurement>();

        /// <summary>
        /// Gets or sets the repository locations modified by the user.
        /// </summary>
        public ICollection<RepositoryLocation> ModifiedRepositoryLocations { get; set; } = new HashSet<RepositoryLocation>();

        /// <summary>
        /// Gets or sets the collection of series modified by the user.
        /// </summary>
        public ICollection<Series> ModifiedSeries { get; set; } = new HashSet<Series>();

        /// <summary>
        /// Gets or sets the collection of studies modified by the user.
        /// </summary>
        public ICollection<Study> ModifiedStudies { get; set; } = new HashSet<Study>();

        /// <summary>
        /// Gets or sets the collection of unreal objects modified by the user.
        /// </summary>
        public ICollection<UnrealObject> ModifiedUnrealObjects { get; set; } = new HashSet<UnrealObject>();

        /// <summary>
        /// Gets or sets the short name.
        /// </summary>
        public string? ShortName { get => _shortName; set => SetProperty(ref _shortName, value); }

        /// <summary>
        /// Gets or sets the suffix.
        /// </summary>
        public string? Suffix { get => _suffix; set => SetProperty(ref _suffix, value); }

        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        public string? Title { get => _title; set => SetProperty(ref _title, value); }
    }
}