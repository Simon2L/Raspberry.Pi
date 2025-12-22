using Raspberry.Pi.Dashboard.Events.Models;

namespace Raspberry.Pi.Dashboard.Events.Publishers;

public interface IProximityEventPublisher
{
    event EventHandler<ProximityEvent>? ProximityDetected;
    void Publish(ProximityEvent e);
}
