
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
    private readonly Vcnl4010 _sensor1;
    private readonly Vcnl4010 _sensor2;
    private const int ProximityEventThreshold = 3000;
    private readonly bool SensorsFailedToInitialize = false;
    private readonly ISettingsService _settingsService;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public ProximitySensorReaderBackgroundService(ISettingsService settingsService)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    {
        _settingsService = settingsService;
        try
        {
            Console.WriteLine("Connecting to the sensors.");
            _sensor1 = new(busId: 1);
            _sensor2 = new(busId: 3);
        }
        catch
        {
            Console.WriteLine("Could not connect to the sensors.");
            SensorsFailedToInitialize = true;
        }
    }

    public event EventHandler<ProximityEvent>? ProximityThresholdReached;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (SensorsFailedToInitialize) await StopAsync(stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            var proximity1 = _sensor1.GetProximity();
            var proximity2 = _sensor2.GetProximity();
            // Console.WriteLine($"proximity1: {proximity1}, proximity2: {proximity2}");
            
            if (proximity1 > ProximityEventThreshold)
            {
                Console.WriteLine("Sensor1 Event");
                ProximityThresholdReached?.Invoke(
                    this,
                    new ProximityEvent(Sensor.Sensor1, proximity1, DateTime.Now));
            }

            if (proximity2 > ProximityEventThreshold)
            {
                Console.WriteLine("Sensor2 Event");
                ProximityThresholdReached?.Invoke(
                    this,
                    new ProximityEvent(Sensor.Sensor2, proximity2, DateTime.Now));
            }
            var settings = _settingsService.GetSettings();
            await Task.Delay(settings.SensorDelay.Milliseconds, stoppingToken);
        }
    }

}

