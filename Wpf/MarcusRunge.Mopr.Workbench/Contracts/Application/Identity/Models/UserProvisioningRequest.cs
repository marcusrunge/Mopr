namespace MarcusRunge.Mopr.Workbench.Contracts.Application.Identity.Models
{
    /// <summary>
    /// Contains the user-editable values required to create a persistent MOPR user.
    /// </summary>
    public sealed record UserProvisioningRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UserProvisioningRequest"/> class.
        /// </summary>
        /// <param name="firstName">The first name entered or confirmed by the administrator.</param>
        /// <param name="lastName">The last name entered or confirmed by the administrator.</param>
        /// <param name="shortName">The short name entered or confirmed by the administrator.</param>
        /// <param name="academicTitle">The optional academic title.</param>
        /// <param name="personnelNumber">The optional organization-specific personnel number.</param>
        public UserProvisioningRequest(string? firstName, string? lastName, string? shortName, string? academicTitle = null, string? personnelNumber = null)
        {
            FirstName = firstName;
            LastName = lastName;
            ShortName = shortName;
            AcademicTitle = academicTitle;
            PersonnelNumber = personnelNumber;
        }

        /// <summary>
        /// Gets the optional academic title entered or confirmed by the administrator.
        /// </summary>
        public string? AcademicTitle { get; }

        /// <summary>
        /// Gets the first name entered or confirmed by the administrator.
        /// </summary>
        public string? FirstName { get; }

        /// <summary>
        /// Gets the last name entered or confirmed by the administrator.
        /// </summary>
        public string? LastName { get; }

        /// <summary>
        /// Gets the optional organization-specific personnel number.
        /// </summary>
        public string? PersonnelNumber { get; }

        /// <summary>
        /// Gets the short name entered or confirmed by the administrator.
        /// </summary>
        public string? ShortName { get; }
    }
}