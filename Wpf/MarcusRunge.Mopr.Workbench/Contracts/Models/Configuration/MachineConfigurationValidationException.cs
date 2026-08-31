using System;

namespace MarcusRunge.Mopr.Workbench.Contracts.Models.Configuration
{
    /// <summary>
    /// Represents an attempt to save an invalid machine-wide MOPR configuration.
    /// </summary>
    public sealed class MachineConfigurationValidationException(MachineConfigurationValidationResult validationResult) : Exception("The machine-wide MOPR configuration is not valid.")
    {
        /// <summary>
        /// Gets the structural configuration validation result.
        /// </summary>
        public MachineConfigurationValidationResult ValidationResult { get; } = validationResult ?? throw new ArgumentNullException(nameof(validationResult));
    }
}