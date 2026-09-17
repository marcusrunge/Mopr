using System;

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
        /// <param name="firstName">The first name entered or confirmed by the user.</param>
        /// <param name="lastName">The last name entered or confirmed by the user.</param>
        /// <param name="shortName">The short name entered or confirmed by the user.</param>
        public UserProvisioningRequest(string? firstName, string? lastName, string? shortName)
        {
            FirstName = firstName;
            LastName = lastName;
            ShortName = shortName;
        }

        /// <summary>
        /// Gets the first name entered or confirmed by the user.
        /// </summary>
        public string? FirstName { get; }

        /// <summary>
        /// Gets the last name entered or confirmed by the user.
        /// </summary>
        public string? LastName { get; }

        /// <summary>
        /// Gets the short name entered or confirmed by the user.
        /// </summary>
        public string? ShortName { get; }
    }
}