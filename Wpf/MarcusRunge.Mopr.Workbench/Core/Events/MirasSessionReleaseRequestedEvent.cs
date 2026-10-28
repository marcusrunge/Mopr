using Prism.Events;

namespace MarcusRunge.Mopr.Workbench.Core.Events
{
    /// <summary>
    /// Signals that the protected MIRAS startup state permits the current session
    /// to enter the imaging workbench after an explicit user acknowledgement.
    /// </summary>
    public sealed class MirasSessionReleaseRequestedEvent : PubSubEvent
    {
    }
}
