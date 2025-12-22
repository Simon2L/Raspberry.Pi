namespace Raspberry.Pi.Dashboard;

/*
public class ProximityUIState
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

    public Dictionary<Sensor, bool> SensorConnectedMap { get; set; } =
        new Dictionary<Sensor, bool>
        {
            {Sensor.Sensor1, true },
            {Sensor.Sensor2, true }
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
*/