using MarcusRunge.Base;

namespace MarcusRunge.Mopr.Workbench.Services.Persistence.Entities
{
    /// <summary>
    /// Represents a persisted user within the MOPR system.
    /// </summary>
    public class User : BindableEntityBase
    {
        private string? _academicTitle, _firstName, _lastName, _loginName, _middleName, _personnelNumber, _securityIdentifier, _shortName, _suffix;
        private bool _isActive = true;

        /// <summary>
        /// Gets or sets the optional academic title displayed before the user's name.
        /// </summary>
        public string? AcademicTitle { get => _academicTitle; set => SetProperty(ref _academicTitle, value); }

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
        /// Gets or sets a value indicating whether the user may perform authenticated MOPR operations.
        /// </summary>
        public bool IsActive { get => _isActive; set => SetProperty(ref _isActive, value); }

        /// <summary>
        /// Gets or sets the last name.
        /// </summary>
        public string? LastName { get => _lastName; set => SetProperty(ref _lastName, value); }

        /// <summary>
        /// Gets or sets the operating-system login name assigned to the user.
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
        /// Gets or sets the optional organization-specific personnel number.
        /// </summary>
        public string? PersonnelNumber { get => _personnelNumber; set => SetProperty(ref _personnelNumber, value); }

        /// <summary>
        /// Gets or sets the optional Windows security identifier assigned to the user.
        /// </summary>
        /// <remarks>
        /// The security identifier becomes the durable operating-system identity
        /// after the SID-based sign-in migration has been completed.
        /// </remarks>
        public string? SecurityIdentifier { get => _securityIdentifier; set => SetProperty(ref _securityIdentifier, value); }

        /// <summary>
        /// Gets or sets the short name.
        /// </summary>
        public string? ShortName { get => _shortName; set => SetProperty(ref _shortName, value); }

        /// <summary>
        /// Gets or sets the suffix.
        /// </summary>
        public string? Suffix { get => _suffix; set => SetProperty(ref _suffix, value); }
    }
}