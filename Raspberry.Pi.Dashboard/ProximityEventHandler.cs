
namespace Raspberry.Pi.Dashboard;

public class ProximityEventHandler
{
    private readonly GoveeClient _goveeClient;
    private readonly ProximityUiState _state;
    private readonly SemaphoreSlim _sensor1Semaphore = new(1, 1);
    private readonly SemaphoreSlim _sensor2Semaphore = new(1, 1);
    private CancellationTokenSource? _sensor1Cts;
    private CancellationTokenSource? _sensor2Cts;

    public ProximityEventHandler(
        ProximitySensorReaderBackgroundService reader, 
        GoveeClient goveeClient, 
        ProximityUiState state)
    {
        reader.ProximityThresholdReached += OnThresholdReached;
        _goveeClient = goveeClient;
        _state = state;
    }

    private void OnThresholdReached(object? sender, ProximityEvent e)
    {
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
        // Only allow one operation at a time per sensor
        if (!await _sensor1Semaphore.WaitAsync(0))
        {
            // Already processing, skip this event
            return;
        }

        try
        {
            // Cancel any ongoing smooth brightness transition
            _sensor1Cts?.Cancel();
            _sensor1Cts = new CancellationTokenSource();

            // Your Govee logic here
            await _goveeClient.SetBrightnessSmooth(
                brightness: CalculateBrightness(e.Value),
                duration: TimeSpan.FromSeconds(2),
                _sensor1Cts.Token);
        }
        finally
        {
            _sensor1Semaphore.Release();
        }
    }

    private async Task HandleSensor2Async(ProximityEvent e)
    {
        if (!await _sensor2Semaphore.WaitAsync(0))
        {
            return;
        }

        try
        {
            _sensor2Cts?.Cancel();
            _sensor2Cts = new CancellationTokenSource();

            await _goveeClient.SetBrightnessSmooth(
                brightness: CalculateBrightness(e.Value),
                duration: TimeSpan.FromSeconds(2),
                _sensor2Cts.Token);
        }
        finally
        {
            _sensor2Semaphore.Release();
        }
    }

    private int CalculateBrightness(int proximityValue)
    {
        // Your brightness calculation logic
        return Math.Clamp((proximityValue - 3000) / 10, 0, 100);
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
