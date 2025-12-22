using Raspberry.Pi.Dashboard.Domain;
using Raspberry.Pi.Dashboard.Events.Models;
using Raspberry.Pi.Dashboard.Events.Publishers;

namespace Raspberry.Pi.Dashboard.Services;

public class ProximitySensorReaderBackgroundService : BackgroundService
{
    private readonly Vcnl4010 _sensor1;
    private readonly Vcnl4010 _sensor2;
    private readonly bool SensorsFailedToInitialize = false;
    private readonly ISettingsService _settingsService;
    private readonly IProximityEventPublisher _publisher;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public ProximitySensorReaderBackgroundService(ISettingsService settingsService, IProximityEventPublisher publisher, IApplicationStateService appState)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    {
        int busId = 1;
        try
        {
            _sensor1 = new(busId);
        }
        catch
        {
            Console.WriteLine($"Could not connect to sensor with bus id {busId}");
            SensorsFailedToInitialize = true;
            appState.UpdateSensorConnection(Sensor.Sensor1, isConnected: false);
        }

        busId = 3;
        try
        {
            _sensor2 = new(busId);
        }
        catch
        {
            SensorsFailedToInitialize = true;
            appState.UpdateSensorConnection(Sensor.Sensor2, isConnected: false);
            Console.WriteLine($"Could not connect to sensor with bus id {busId}");
        }

        _publisher = publisher;
        _settingsService = settingsService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (SensorsFailedToInitialize) await StopAsync(stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            var settings = _settingsService.GetSettings();

            var proximity1 = _sensor1.GetProximity();
            var proximity2 = _sensor2.GetProximity();
            
            if (proximity1 > settings.ProximityEventTreshold)
            {
                _publisher.Publish(new ProximityEvent(Sensor.Sensor1, proximity1, DateTime.Now));
            }

            if (proximity2 > settings.ProximityEventTreshold)
            {
                _publisher.Publish(new ProximityEvent(Sensor.Sensor2, proximity1, DateTime.Now));
            }
            await Task.Delay(settings.SensorDelay, stoppingToken);
        }
    }

}

