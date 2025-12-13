
namespace Raspberry.Pi.Dashboard;

public class ProximityEventHandler
{
    private readonly GoveeClient _goveeClient;
    public ProximityEventHandler(ProximitySensorReaderBackgroundService reader, GoveeClient goveeClient)
    {
        reader.ProximityThresholdReached += OnThresholdReached;
        _goveeClient = goveeClient;
    }

    private async void OnThresholdReached(object? sender, ProximityEvent e)
    {
        // algorithm
        if (e.Sensor == Sensor.Sensor1)
        {

        }

        if (e.Sensor == Sensor.Sensor2)
        {

        }
    }
}

public class ProximityUiState
{
    private readonly Lock _lock = new();

    public ProximityEvent? LastEventSensor1 { get; private set; }
    public ProximityEvent? LastEventSensor2 { get; private set; }

    public event Action? OnChange;


    public void Update(ProximityEvent proximityEvent)
    {
        lock (_lock)
        {
            if (proximityEvent.Sensor == Sensor.Sensor1)
            {
                LastEventSensor1 = proximityEvent;
            }

            if (proximityEvent.Sensor == Sensor.Sensor2)
            {
                LastEventSensor2 = proximityEvent;
            }
        }

        OnChange?.Invoke();
    }
}
