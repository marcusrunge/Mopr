using Prism.Events;

namespace MarcusRunge.Mopr.Workbench.Core.Events
{
    /// <summary>
    /// Signals that the first personal MOPR user was provisioned, activated and
    /// published to the application-wide current-user context.
    /// </summary>
    /// <remarks>
    /// The composition root handles the remaining protected startup sequence,
    /// including MIRAS initialization and navigation to the imaging workbench.
    /// </remarks>
    public sealed class InitialUserProvisioningCompletedEvent : PubSubEvent
    {
    }
}