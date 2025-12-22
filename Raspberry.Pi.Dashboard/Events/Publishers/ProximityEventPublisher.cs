using Raspberry.Pi.Dashboard.Events.Models;

namespace Raspberry.Pi.Dashboard.Events.Publishers;

public class ProximityEventPublisher : IProximityEventPublisher
{
    public event EventHandler<ProximityEvent>? ProximityDetected;

    public void Publish(ProximityEvent e)
    {
        ProximityDetected?.Invoke(this, e);
    }
}
