
namespace Raspberry.Pi.Dashboard;

public class ProximityEventHandler
{
    private readonly GoveeClient _goveeClient;
    private readonly ProximityUiState _state;
    private readonly SemaphoreSlim _sensor1Semaphore = new(1, 1);
    private readonly SemaphoreSlim _sensor2Semaphore = new(1, 1);
    private CancellationTokenSource? _sensor1DecreaseCts;
    private CancellationTokenSource? _sensor2DecreaseCts;
    private readonly TimeSpan _duration = TimeSpan.FromSeconds(2);
    private readonly TimeSpan _holdTime = TimeSpan.FromSeconds(5);

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
            // Smoothly increase brightness to 100 (cannot be cancelled)
            await _goveeClient.SetSegmentBrightnessSmoothAsync(
                segments: [0, 1, 2, 3, 4, 5, 6],
                targetBrightness: 100,
                duration: _duration,
                CancellationToken.None); // Cannot be cancelled

            // Hold at 100% for the timer duration (can be cancelled)
            await Task.Delay(_holdTime, _sensor1DecreaseCts.Token);

            // Smoothly decrease brightness back to 10 (can be cancelled)
            await _goveeClient.SetSegmentBrightnessSmoothAsync(
                segments: [0, 1, 2, 3, 4, 5, 6],
                targetBrightness: 0,
                duration: _duration,
                _sensor1DecreaseCts.Token);
        }
        catch (OperationCanceledException)
        {
            // Expected when a new event interrupts the timer or decrease phase
            // The new event will start the increase phase
        }
        finally
        {
            _sensor1Semaphore.Release();
        }
    }

    private async Task HandleSensor2Async(ProximityEvent e)
    {
        // Cancel any ongoing decrease (timer or decrease phase)
        _sensor2DecreaseCts?.Cancel();
        _sensor2DecreaseCts = new CancellationTokenSource();

        // Wait for any current operation to finish
        await _sensor2Semaphore.WaitAsync();

        try
        {
            // Smoothly increase brightness to 100 (cannot be cancelled)
            await _goveeClient.SetSegmentBrightnessSmoothAsync(
                segments: [8, 9, 10, 11, 12, 13, 14],
                targetBrightness: 100,
                duration: _duration,
                CancellationToken.None); // Cannot be cancelled

            // Hold at 100% for the timer duration (can be cancelled)
            await Task.Delay(_holdTime, _sensor2DecreaseCts.Token);

            // Smoothly decrease brightness back to 10 (can be cancelled)
            await _goveeClient.SetSegmentBrightnessSmoothAsync(
                segments: [8, 9, 10, 11, 12, 13, 14],
                targetBrightness: 0,
                duration: _duration,
                _sensor2DecreaseCts.Token);
        }
        catch (OperationCanceledException)
        {
            // Expected when a new event interrupts the timer or decrease phase
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
