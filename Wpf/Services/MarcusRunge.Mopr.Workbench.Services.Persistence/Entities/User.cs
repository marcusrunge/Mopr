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
        /// Gets or sets the first name.
        /// </summary>
        public string? FirstName { get => _firstName; set => SetProperty(ref _firstName, value); }

        /// <summary>
        /// Gets or sets the last name.
        /// </summary>
        public string? LastName { get => _lastName; set => SetProperty(ref _lastName, value); }

        /// <summary>
        /// Gets or sets the middle name.
        /// </summary>
        public string? MiddleName { get => _middleName; set => SetProperty(ref _middleName, value); }

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

        /// <summary>
        /// Gets or sets the login name.
        /// </summary>
        public string? LoginName { get => _loginName; set => SetProperty(ref _loginName, value); }
    }
}