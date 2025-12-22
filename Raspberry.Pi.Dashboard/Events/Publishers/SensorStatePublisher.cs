using Raspberry.Pi.Dashboard.Events.Models;

namespace Raspberry.Pi.Dashboard.Events.Publishers;

public class SensorStatePublisher : ISensorStatePublisher
{
    public event EventHandler<SensorStateChangedEvent>? StateChanged;

    public void Publish(SensorStateChangedEvent e)
    {
        StateChanged?.Invoke(this, e);
    }
}