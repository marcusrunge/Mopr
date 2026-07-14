using MarcusRunge.Base;

namespace MarcusRunge.Mopr.Workbench.Services.Persistence.Entities
{
    /// <summary>
    /// Represents an auditable persistent entity.
    /// </summary>
    public abstract class AuditableEntityBase : BindableEntityBase
    {
        private DateTime _createdAtUtc = DateTime.UtcNow;
        private User? _createdByUser, _modifiedByUser;
        private int _createdByUserId;
        private int? _modifiedByUserId;
        private DateTime? _modifiedAtUtc;

        /// <summary>
        /// Gets or sets the UTC date and time when the entity was created.
        /// </summary>
        public DateTime CreatedAtUtc { get => _createdAtUtc; set => SetProperty(ref _createdAtUtc, value); }

        /// <summary>
        /// Gets or sets the user who created the entity.
        /// </summary>
        public User? CreatedByUser { get => _createdByUser; set => SetProperty(ref _createdByUser, value); }

        /// <summary>
        /// Gets or sets the ID of the user who created the entity.
        /// </summary>
        public int CreatedByUserId { get => _createdByUserId; set => SetProperty(ref _createdByUserId, value); }

        /// <summary>
        /// Gets or sets the UTC date and time when the entity was last modified.
        /// </summary>
        public DateTime? ModifiedAtUtc { get => _modifiedAtUtc; set => SetProperty(ref _modifiedAtUtc, value); }

        /// <summary>
        /// Gets or sets the user who last modified the entity.
        /// </summary>
        public User? ModifiedByUser { get => _modifiedByUser; set => SetProperty(ref _modifiedByUser, value); }

        /// <summary>
        /// Gets or sets the ID of the user who last modified the entity.
        /// </summary>
        public int? ModifiedByUserId { get => _modifiedByUserId; set => SetProperty(ref _modifiedByUserId, value); }
    }
}