using Raspberry.Pi.Dashboard.Domain;

namespace Raspberry.Pi.Dashboard.Events.Models;

public class SensorStateChangedEvent
{
    public Sensor Sensor { get; init; }
    public int Brightness { get; init; }
    public SensorActivity Activity { get; init; }
    public DateTime Timestamp { get; init; }
}
