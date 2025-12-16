
namespace Raspberry.Pi.Dashboard;

public class ProximityEventHandler
{
    private readonly GoveeClient _goveeClient;
    private readonly ISettingsService _settingsService;
    private readonly ProximityUiState _state;
    private readonly SemaphoreSlim _sensor1Semaphore = new(1, 1);
    private readonly SemaphoreSlim _sensor2Semaphore = new(1, 1);
    private CancellationTokenSource? _sensor1DecreaseCts;
    private CancellationTokenSource? _sensor2DecreaseCts;

    public ProximityEventHandler(
        ProximitySensorReaderBackgroundService reader,
        GoveeClient goveeClient,
        ProximityUiState state,
        ISettingsService settingsService)
    {
        reader.ProximityThresholdReached += OnThresholdReached;
        _goveeClient = goveeClient;
        _state = state;
        _settingsService = settingsService;
    }

    private void OnThresholdReached(object? sender, ProximityEvent e)
    {
        Console.WriteLine("Event happened :D");
        _state.Update(e);

        // Fire-and-forget with proper task handling
        _ = HandleProximityEventAsync(e);
    }

    private async Task HandleProximityEventAsync(ProximityEvent e)
    {
        try
        {
            if (e.Sensor == Sensor.Sensor1)
            {
                await HandleSensor1Async(e);
            }
            else if (e.Sensor == Sensor.Sensor2)
            {
                await HandleSensor2Async(e);
            }
        }
        catch (Exception ex)
        {
            // Log the exception properly
            Console.WriteLine($"Error handling proximity event: {ex}");
        }
    }

    private async Task HandleSensor1Async(ProximityEvent e)
    {
        // Cancel any ongoing decrease (timer or decrease phase)
        _sensor1DecreaseCts?.Cancel();
        _sensor1DecreaseCts = new CancellationTokenSource();

        // Wait for any current operation to finish
        await _sensor1Semaphore.WaitAsync();

        try
        {
            var settings = _settingsService.GetSettings();

            await _goveeClient.SetSegmentBrightnessSmoothAsync(
                 segments: settings.Section1,
                 targetBrightness: settings.MaxBrightness,
                 duration: settings.SmoothDuration,
                 CancellationToken.None); // Cannot be cancelled

            await Task.Delay(settings.HoldDuration, _sensor1DecreaseCts.Token);

            await _goveeClient.SetSegmentBrightnessAsync(settings.Section1, settings.MinBrightness);

        }
        catch (OperationCanceledException)
        {
            // Expected when a new event interrupts the timer or decrease phase
            // The new event will start the increase phase
            Console.WriteLine("Sensor 1 timer reset");
        }
        finally
        {
            _sensor1Semaphore.Release();
        }
    }

    private async Task HandleSensor2Async(ProximityEvent e)
    {
        _sensor2DecreaseCts?.Cancel();
        _sensor2DecreaseCts = new CancellationTokenSource();

        await _sensor2Semaphore.WaitAsync();

        var settings = _settingsService.GetSettings();

        try
        {
            await _goveeClient.SetSegmentBrightnessSmoothAsync(
                segments: settings.Section2,
                targetBrightness: settings.MaxBrightness,
                duration: settings.SmoothDuration,
                CancellationToken.None); // Cannot be cancelled

            await Task.Delay(settings.HoldDuration, _sensor2DecreaseCts.Token);

            await _goveeClient.SetSegmentBrightnessAsync(settings.Section2, settings.MinBrightness);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Sensor 2 timer reset");
        }
        finally
        {
            _sensor2Semaphore.Release();
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
