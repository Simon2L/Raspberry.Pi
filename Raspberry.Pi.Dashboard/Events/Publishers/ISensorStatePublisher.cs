using Raspberry.Pi.Dashboard.Events.Models;

namespace Raspberry.Pi.Dashboard.Events.Publishers;

// REMOVE?
public interface ISensorStatePublisher
{
    event EventHandler<SensorStateChangedEvent>? StateChanged;
    void Publish(SensorStateChangedEvent e);
}
