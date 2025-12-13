
namespace Raspberry.Pi.Dashboard;

public record ProximityEvent(Sensor Sensor, int Value, DateTime Timestamp);
public enum Sensor
{
    None = 0,
    Sensor1 = 1,
    Sensor2 = 2,
}

public class ProximitySensorReaderBackgroundService : BackgroundService
{
    private readonly Vcnl4010 _sensor1 = new(busId: 1);
    private readonly Vcnl4010 _sensor2 = new(busId: 3);
    private const int ProximityEventThreshold = 3000;

    public event EventHandler<ProximityEvent>? ProximityThresholdReached;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var proximity1 = _sensor1.GetProximity();
            var proximity2 = _sensor2.GetProximity();

            if (proximity1 > ProximityEventThreshold)
            {
                ProximityThresholdReached?.Invoke(
                    this,
                    new ProximityEvent(Sensor.Sensor1, proximity1, DateTime.Now));
            }

            if (proximity2 > ProximityEventThreshold)
            {
                ProximityThresholdReached?.Invoke(
                    this,
                    new ProximityEvent(Sensor.Sensor2, proximity2, DateTime.Now));
            }

            await Task.Delay(1000, stoppingToken);
        }
    }
}

