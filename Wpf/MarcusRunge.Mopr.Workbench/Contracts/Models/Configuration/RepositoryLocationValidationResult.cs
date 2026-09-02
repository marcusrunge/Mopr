namespace MarcusRunge.Mopr.Workbench.Contracts.Models.Configuration
{
    /// <summary>
    /// Represents the validation result of a potential DICOM repository location.
    /// </summary>
    public sealed class RepositoryLocationValidationResult
    {
        /// <summary>
        /// Gets a value indicating whether the directory exists.
        /// </summary>
        public bool Exists { get; init; }

        /// <summary>
        /// Gets a value indicating whether the directory can be enumerated.
        /// </summary>
        public bool IsReadable { get; init; }

        /// <summary>
        /// Gets a value indicating whether a temporary file can be written and removed.
        /// </summary>
        public bool IsWritable { get; init; }

        /// <summary>
        /// Gets a value indicating whether the location satisfies all requirements.
        /// </summary>
        public bool IsValid => Exists && IsReadable && IsWritable;

        /// <summary>
        /// Gets the normalized absolute repository path when normalization succeeded.
        /// </summary>
        public string? NormalizedPath { get; init; }
    }
}