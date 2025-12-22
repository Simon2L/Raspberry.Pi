using Raspberry.Pi.Dashboard.Domain;
using Raspberry.Pi.Dashboard.Events.Models;
using Raspberry.Pi.Dashboard.Events.Publishers;
using Raspberry.Pi.Dashboard.Services;

namespace Raspberry.Pi.Dashboard.Handlers;

public class ProximityEventHandler : IHostedService
{
    private readonly GoveeClient _goveeClient;
    private readonly ISettingsService _settingsService;
    private readonly IProximityEventPublisher _proximityEventPublisher;
    private readonly IApplicationStateService _appState;

    public ProximityEventHandler(
        GoveeClient goveeClient,
        ISettingsService settingsService,
        IProximityEventPublisher proximityEventPublisher,
        IApplicationStateService appState)
    {
        _goveeClient = goveeClient;
        _settingsService = settingsService;
        _proximityEventPublisher = proximityEventPublisher;
        _appState = appState;

        // Configure which segments each sensor controls
        InitializeSensorSegmentMappings();
    }

    private void InitializeSensorSegmentMappings()
    {
        var settings = _settingsService.GetSettings();

        _appState.ConfigureSensorSegments(Sensor.Sensor1, settings.Section1 ?? []);
        _appState.ConfigureSensorSegments(Sensor.Sensor2, settings.Section2 ?? []);
        _appState.ConfigureSensorSegments(Sensor.Sensor3, settings.Section3 ?? []);
        _appState.ConfigureSensorSegments(Sensor.Sensor4, settings.Section4 ?? []);
        _appState.ConfigureSensorSegments(Sensor.Sensor5, settings.Section5 ?? []);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _proximityEventPublisher.ProximityDetected += OnThresholdReached;
        _settingsService.SettingsChanged += OnSettingsChanged;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _proximityEventPublisher.ProximityDetected -= OnThresholdReached;
        _settingsService.SettingsChanged -= OnSettingsChanged;
        return Task.CompletedTask;
    }

    private void OnSettingsChanged(object? sender, EventArgs e)
    {
        InitializeSensorSegmentMappings();
    }

    private void OnThresholdReached(object? sender, ProximityEvent e)
    {
        _ = HandleProximityEventAsync(e);
    }

    private async Task HandleProximityEventAsync(ProximityEvent e)
    {
        try
        {
            var sensorState = _appState.GetSensorState(e.Sensor);
            await HandleSensorAsync(e.Sensor, sensorState);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling proximity event for {e.Sensor}: {ex}");
        }
    }

    private async Task HandleSensorAsync(Sensor sensor, SensorState state)
    {
        if (state.ControlledSegments.Count == 0)
        {
            Console.WriteLine($"No section configured for {sensor}");
            return;
        }

        var settings = _settingsService.GetSettings();

        List<int> sectionToIncrease = [.. state.ControlledSegments];
        foreach (var otherSensor in Enum.GetValues<Sensor>())
        {
            if (otherSensor == sensor)
                continue;
            var otherState = _appState.GetSensorState(otherSensor);
            if (otherState.Activity == SensorActivity.Increasing)
            {
                foreach (var seg in otherState.ControlledSegments)
                {
                    sectionToIncrease.Remove(seg);
                }
            }
        }

        state.DecreaseCts?.Cancel();
        state.DecreaseCts = new CancellationTokenSource();

        await state.Semaphore.WaitAsync();
        try
        {
            if (state.CurrentBrightness != settings.MaxBrightness)
            {
                _appState.UpdateSensorBrightness(sensor, state.CurrentBrightness, SensorActivity.Increasing);
                //if (sectionToIncrease.Count > 0)
                //{
                    await _goveeClient.SetSegmentBrightnessSmoothAsync(
                        segments: sectionToIncrease,
                        targetBrightness: settings.MaxBrightness,
                        duration: settings.SmoothDuration,
                        sensor,
                        CancellationToken.None);
                //}
                _appState.SetSegmentBrightnessForSensor(sensor, state.ControlledSegments, settings.MaxBrightness);
            }

            state.CurrentBrightness = settings.MaxBrightness;

            _ = StartDecreaseTimerAsync(sensor, state, state.DecreaseCts.Token);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine($"Sensor {sensor}: Timer reset");
        }
        finally
        {
            state.Semaphore.Release();
        }
    }

    private async Task StartDecreaseTimerAsync(Sensor sensor, SensorState state, CancellationToken token)
    {
        var settings = _settingsService.GetSettings();

        try
        {
            _appState.UpdateSensorBrightness(sensor, settings.MaxBrightness, SensorActivity.Holding);

            await Task.Delay(settings.HoldDuration, token);

            List<int> section = [];
            foreach (var seg in state.ControlledSegments)
            {
                var ledState = _appState.GetSegmentState(seg);
                if (ledState.SensorBrightnessRequests.TryGetValue(sensor, out _) && ledState.SensorBrightnessRequests.Keys.Count == 1)
                {
                    section.Add(seg);
                }
                else
                {
                    ledState.SensorBrightnessRequests.Remove(sensor);
                }
            }

            if (section.Count == 0)
            {
                _appState.UpdateSensorBrightness(sensor, settings.MinBrightness, SensorActivity.Idle);
                _appState.ClearSensorBrightnessRequest(sensor, state.ControlledSegments);
                return;
            }

            await state.Semaphore.WaitAsync(token);
            try
            {
                _appState.UpdateSensorBrightness(sensor, settings.MaxBrightness, SensorActivity.Decreasing);

                await _goveeClient.SetSegmentBrightnessAsync(sensor, section, settings.MinBrightness);
                state.CurrentBrightness = settings.MinBrightness;

                _appState.UpdateSensorBrightness(sensor, settings.MinBrightness, SensorActivity.Idle);
                _appState.ClearSensorBrightnessRequest(sensor, section);
            }
            finally
            {
                state.Semaphore.Release();
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine($"Sensor {sensor}: Decrease cancelled");
        }
    }
}