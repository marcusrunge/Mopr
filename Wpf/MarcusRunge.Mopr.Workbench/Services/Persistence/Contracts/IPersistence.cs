namespace MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts
{
    /// <summary>
    /// Defines the public contract of the assembly.
    /// </summary>
    public interface IPersistence
    {
        /// <summary>
        /// Occurs when an exception is thrown.
        /// </summary>
        event Action<Exception> ExceptionThrown;
        /// <summary>
        /// Gets the instance repository.
        /// </summary>
        /// <value>
        /// The instance repository.
        /// </value>
        IInstanceRepository? Instance { get; }
        /// <summary>
        /// Gets the measurement repository.
        /// </summary>
        /// <value>
        /// The measurement repository.
        /// </value>
        IMeasurementRepository? Measurement { get; }
        /// <summary>
        /// Gets the series repository.
        /// </summary>
        /// <value>
        /// The series repository.
        /// </value>
        ISeriesRepository? Series { get; }
        /// <summary>
        /// Gets the study repository.
        /// </summary>
        /// <value>
        /// The study repository.
        /// </value>
        IStudyRepository? Study { get; }
        /// <summary>
        /// Gets the user repository.
        /// </summary>
        /// <value>
        /// The user repository.
        /// </value>
        IUserRepository? User { get; }
    }
}