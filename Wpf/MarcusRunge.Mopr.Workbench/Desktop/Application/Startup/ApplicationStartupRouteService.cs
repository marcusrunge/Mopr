using MarcusRunge.Mopr.Workbench.Contracts.Application.Configuration;
using MarcusRunge.Mopr.Workbench.Core;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MarcusRunge.Mopr.Workbench.Application.Startup
{
    /// <summary>
    /// Determines whether MOPR must display the machine setup or the regular workbench.
    /// </summary>
    internal sealed class ApplicationStartupRouteService(IMachineConfigurationService configurationService) : IApplicationStartupRouteService
    {
        private readonly IMachineConfigurationService _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));

        /// <inheritdoc/>
        public async Task<string> GetInitialNavigationTargetAsync(CancellationToken cancellationToken = default)
        {
            var configuration = await _configurationService.LoadAsync(cancellationToken).ConfigureAwait(false);
            var validationResult = _configurationService.ValidateForSetupCompletion(configuration);

            // Setup remains the safe destination for missing, incomplete or structurally invalid machine configuration.
            return validationResult.IsValid ? NavigationNames.Imaging : NavigationNames.Setup;
        }
    }
}