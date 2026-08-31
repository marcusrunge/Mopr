using MarcusRunge.Mopr.Workbench.Contracts.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MarcusRunge.Mopr.Workbench.Contracts.Models.Configuration
{
    /// <summary>
    /// Represents the structural validation result of a machine-wide MOPR configuration.
    /// </summary>
    public sealed class MachineConfigurationValidationResult(IEnumerable<MachineConfigurationIssue> issues)
    {
        private readonly IReadOnlyList<MachineConfigurationIssue> _issues = (issues ?? throw new ArgumentNullException(nameof(issues))).Distinct().ToArray();

        /// <summary>
        /// Gets a successful result without structural configuration issues.
        /// </summary>
        public static MachineConfigurationValidationResult Success { get; } = new(Array.Empty<MachineConfigurationIssue>());

        /// <summary>
        /// Gets a value indicating whether the machine configuration is structurally valid.
        /// </summary>
        public bool IsValid => _issues.Count == 0;

        /// <summary>
        /// Gets the detected structural configuration issues.
        /// </summary>
        public IReadOnlyList<MachineConfigurationIssue> Issues => _issues;
    }
}