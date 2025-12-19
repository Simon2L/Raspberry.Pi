
using MudBlazor;

namespace Raspberry.Pi.Dashboard;

public class ProximityEventHandler
{
    private readonly GoveeClient _goveeClient;
    private readonly ISettingsService _settingsService;
    private readonly ProximityUiState _state;
    private readonly Dictionary<Sensor, SensorState> _sensorStates;

    private class SensorState
    {
        public SemaphoreSlim Semaphore { get; } = new(1, 1);
        public CancellationTokenSource? DecreaseCts { get; set; }
        public int CurrentBrightness { get; set; } = 0;
    }

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

        // Initialize state for all sensors
        _sensorStates = new Dictionary<Sensor, SensorState>
        {
            { Sensor.Sensor1, new SensorState() },
            { Sensor.Sensor2, new SensorState() },
            { Sensor.Sensor3, new SensorState() },
            { Sensor.Sensor4, new SensorState() },
            { Sensor.Sensor5, new SensorState() }
        };
    }

    private void OnThresholdReached(object? sender, ProximityEvent e)
    {
        _state.Update(e);
        _ = HandleProximityEventAsync(e);
    }



    private async Task HandleProximityEventAsync(ProximityEvent e)
    {
        try
        {
            if (!_sensorStates.TryGetValue(e.Sensor, out var state))
            {
                Console.WriteLine($"Unknown sensor: {e.Sensor}");
                return;
            }

            await HandleSensorAsync(e.Sensor, state);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling proximity event for {e.Sensor}: {ex}");
        }
    }

    private async Task HandleSensorAsync(Sensor sensor, SensorState state)
    {
        var settings = _settingsService.GetSettings();
        var section = GetSectionForSensor(sensor, settings);

        if (section == null || section.Count == 0)
        {
            Console.WriteLine($"No section configured for {sensor}");
            return;
        }

        // Cancel any pending decrease operation
        state.DecreaseCts?.Cancel();
        state.DecreaseCts = new CancellationTokenSource();

        // Only proceed if we need to increase brightness


        await state.Semaphore.WaitAsync();
        try
        {
            // Increase brightness smoothly (cannot be cancelled)
            Console.WriteLine($"Increaseing Brightness Smooth {settings.MaxBrightness}");
            if (state.CurrentBrightness != settings.MaxBrightness)
            {
                await _goveeClient.SetSegmentBrightnessSmoothAsync(
                    segments: section,
                    targetBrightness: settings.MaxBrightness,
                    duration: settings.SmoothDuration,
                    CancellationToken.None);
            }

            state.CurrentBrightness = settings.MaxBrightness;

            Console.WriteLine($"Holding for {settings.HoldDuration.TotalSeconds}s");
            // Hold at max brightness
            await Task.Delay(settings.HoldDuration, state.DecreaseCts.Token);

            // Start the decrease timer
            _ = StartDecreaseTimerAsync(sensor, state, state.DecreaseCts.Token);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Timer reset");
            // New event came in during hold, decrease immediately
            // await _goveeClient.SetSegmentBrightnessAsync(section, settings.MinBrightness);
            // state.CurrentBrightness = settings.MinBrightness;
        }
        finally
        {
            state.Semaphore.Release();
        }
    }

    private async Task StartDecreaseTimerAsync(Sensor sensor, SensorState state, CancellationToken token)
    {
        var settings = _settingsService.GetSettings();
        var section = GetSectionForSensor(sensor, settings);

        if (section == null || section.Count == 0)
            return;

        try
        {
            // Wait for the hold duration
            await Task.Delay(settings.HoldDuration, token);

            await state.Semaphore.WaitAsync(token);
            try
            {
                await _goveeClient.SetSegmentBrightnessAsync(section, settings.MinBrightness);
                state.CurrentBrightness = settings.MinBrightness;
            }
            finally
            {
                state.Semaphore.Release();
            }
        }
        catch (OperationCanceledException)
        {
            // Timer was cancelled by new event - expected behavior
            Console.WriteLine("Timer cancelled in DecreaseTimerAsync");
        }
    }

    private List<int>? GetSectionForSensor(Sensor sensor, Settings settings)
    {
        return sensor switch
        {
            Sensor.Sensor1 => settings.Section1,
            Sensor.Sensor2 => settings.Section2,
            Sensor.Sensor3 => settings.Section3,
            Sensor.Sensor4 => settings.Section4,
            Sensor.Sensor5 => settings.Section5,
            _ => null
        };
    }
}


/*
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
*/

public class ProximityUiState
{
    private readonly Lock _lock = new();

    private DateTime? _startTimestamp;
    public TimeSpan TimeLabelSpacing { get; private set; } = TimeSpan.FromMinutes(1);


    public List<TimeSeriesChartSeries> Series { get; private set; } = 
        [
            new TimeSeriesChartSeries() { Index = 1, Name = "Sensor 1", Data = [], LineDisplayType = LineDisplayType.Line, ShowDataMarkers = true },
            new TimeSeriesChartSeries() { Index = 2, Name = "Sensor 2", Data = [], LineDisplayType = LineDisplayType.Line, ShowDataMarkers = true },
            new TimeSeriesChartSeries() { Index = 3, Name = "Sensor 3", Data = [], LineDisplayType = LineDisplayType.Line, ShowDataMarkers = true },
            new TimeSeriesChartSeries() { Index = 4, Name = "Sensor 4", Data = [], LineDisplayType = LineDisplayType.Line, ShowDataMarkers = true },
            new TimeSeriesChartSeries() { Index = 5, Name = "Sensor 5", Data = [], LineDisplayType = LineDisplayType.Line, ShowDataMarkers = true }
        ];

    public List<ChartSeries> BarChartSeries { get; private set; } =
    [
        new ChartSeries() { Name = "Sensor 1", Data = [0], ShowDataMarkers = true },
        new ChartSeries() { Name = "Sensor 2", Data = [0] },
        new ChartSeries() { Name = "Sensor 3", Data = [0] },
        new ChartSeries() { Name = "Sensor 4", Data = [0] },
        new ChartSeries() { Name = "Sensor 5", Data = [0] },
    ];
    public string[] BarChartXAxisLabels { get; private set; } = ["Events"];

    public Dictionary<Sensor, ProximityEvent?> LastSensorEventMap { get; private set; } =
        new Dictionary<Sensor, ProximityEvent?>
        {
            { Sensor.Sensor1, null },
            { Sensor.Sensor2, null },
            { Sensor.Sensor3, null },
            { Sensor.Sensor4, null },
            { Sensor.Sensor5, null }
        };


    public event Action? OnChange;

    public void Update(ProximityEvent proximityEvent)
    {
        lock (_lock)
        {
            _startTimestamp ??= DateTime.Now;
            LastSensorEventMap[proximityEvent.Sensor] = proximityEvent;
            BarChartSeries[(int)proximityEvent.Sensor - 1].Data[0]++;
            Series[(int)proximityEvent.Sensor - 1].Data.Add(new(proximityEvent.Timestamp, proximityEvent.Value));
            UpdateTimeLabelSpacing();
        }
        OnChange?.Invoke();
    }
    private void UpdateTimeLabelSpacing()
    {
        if (_startTimestamp is null)
            return;

        var elapsed = DateTime.Now - _startTimestamp.Value;
        TimeLabelSpacing = TimeSpan.FromTicks(elapsed.Ticks / 10);
    }

}
