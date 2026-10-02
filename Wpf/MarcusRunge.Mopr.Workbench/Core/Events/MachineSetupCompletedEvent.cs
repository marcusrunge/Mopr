using Prism.Events;

namespace MarcusRunge.Mopr.Workbench.Core.Events
{
    /// <summary>
    /// Signals that the protected machine-wide MOPR setup completed successfully.
    /// </summary>
    /// <remarks>
    /// The event does not indicate that the complete initial application setup
    /// is finished. A personal MOPR user may still have to be provisioned before
    /// protected application functionality can be made available.
    /// </remarks>
    public sealed class MachineSetupCompletedEvent : PubSubEvent
    {
    }
}