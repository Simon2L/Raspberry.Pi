using Raspberry.Pi.Dashboard.Domain;

namespace Raspberry.Pi.Dashboard.Events.Models;

public record ProximityEvent(Sensor Sensor, int Value, DateTime Timestamp);
