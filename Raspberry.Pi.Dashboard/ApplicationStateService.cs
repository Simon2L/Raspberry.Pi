using MudBlazor;
using Raspberry.Pi.Dashboard.Domain;
using Raspberry.Pi.Dashboard.Events.Models;
using Raspberry.Pi.Dashboard.Services;

namespace Raspberry.Pi.Dashboard;

// 3. Implementation
public class ApplicationStateService : IApplicationStateService
{
    private readonly Lock _lock = new();
    private readonly Dictionary<Sensor, SensorState> _sensorStates;
    private readonly Dictionary<int, LedSegmentState> _ledSegmentStates;

    private DateTime? _startTimestamp;
    private TimeSpan _timeLabelSpacing = TimeSpan.FromMinutes(1);

    public event Action<Sensor>? OnSensorStateChanged;
    public event Action<int>? OnSegmentStateChanged;
    public event Action? OnChartDataChanged;

    public ApplicationStateService()
    {
        _sensorStates = new Dictionary<Sensor, SensorState>
        {
            { Sensor.Sensor1, new SensorState { Sensor = Sensor.Sensor1 } },
            { Sensor.Sensor2, new SensorState { Sensor = Sensor.Sensor2 } },
            { Sensor.Sensor3, new SensorState { Sensor = Sensor.Sensor3 } },
            { Sensor.Sensor4, new SensorState { Sensor = Sensor.Sensor4 } },
            { Sensor.Sensor5, new SensorState { Sensor = Sensor.Sensor5 } }
        };

        _ledSegmentStates = [];
        for (int i = 0; i < 15; i++)
        {
            _ledSegmentStates[i] = new LedSegmentState
            {
                SegmentIndex = i,
                Brightness = 1,
                Color = new RGB(255, 255, 255)
            };
        }

        ChartSeries =
        [
            new() { Index = 1, Name = "Sensor 1", Data = [], LineDisplayType = LineDisplayType.Line, ShowDataMarkers = true },
            new() { Index = 2, Name = "Sensor 2", Data = [], LineDisplayType = LineDisplayType.Line, ShowDataMarkers = true },
            new() { Index = 3, Name = "Sensor 3", Data = [], LineDisplayType = LineDisplayType.Line, ShowDataMarkers = true },
            new() { Index = 4, Name = "Sensor 4", Data = [], LineDisplayType = LineDisplayType.Line, ShowDataMarkers = true },
            new() { Index = 5, Name = "Sensor 5", Data = [], LineDisplayType = LineDisplayType.Line, ShowDataMarkers = true }
        ];

        BarChartSeries =
        [
            new() { Name = "Sensor 1", Data = [0] },
            new() { Name = "Sensor 2", Data = [0] },
            new() { Name = "Sensor 3", Data = [0] },
            new() { Name = "Sensor 4", Data = [0] },
            new() { Name = "Sensor 5", Data = [0] }
        ];
    }

    public IReadOnlyDictionary<Sensor, SensorState> SensorStates => _sensorStates;
    public IReadOnlyDictionary<int, LedSegmentState> LedSegmentStates => _ledSegmentStates;
    public List<TimeSeriesChartSeries> ChartSeries { get; set; }
    public List<ChartSeries> BarChartSeries { get; set; }
    public string[] BarChartXAxisLabels { get; private set; } = ["Events"];
    public TimeSpan TimeLabelSpacing => _timeLabelSpacing;

    public SensorState GetSensorState(Sensor sensor)
    {
        lock (_lock)
        {
            return _sensorStates[sensor];
        }
    }

    public LedSegmentState GetSegmentState(int segmentIndex)
    {
        lock (_lock)
        {
            return _ledSegmentStates.TryGetValue(segmentIndex, out var state)
                ? state
                : throw new ArgumentException($"Segment {segmentIndex} not found");
        }
    }

    public List<int> GetSegmentsControlledBy(Sensor sensor)
    {
        lock (_lock)
        {
            return _sensorStates[sensor].ControlledSegments.ToList();
        }
    }

    public List<Sensor> GetSensorsControlling(int segmentIndex)
    {
        lock (_lock)
        {
            if (_ledSegmentStates.TryGetValue(segmentIndex, out var state))
            {
                return state.ControlledBySensors.ToList();
            }
            return [];
        }
    }

    public void ConfigureSensorSegments(Sensor sensor, List<int> segments)
    {
        lock (_lock)
        {
            var sensorState = _sensorStates[sensor];

            // Remove old mappings
            foreach (var oldSegment in sensorState.ControlledSegments)
            {
                if (_ledSegmentStates.TryGetValue(oldSegment, out var segmentState))
                {
                    segmentState.ControlledBySensors.Remove(sensor);
                    segmentState.SensorBrightnessRequests.Remove(sensor);
                }
            }

            // Add new mappings
            sensorState.ControlledSegments = segments.ToList();
            foreach (var segment in segments)
            {
                if (!_ledSegmentStates.TryGetValue(segment, out LedSegmentState? value))
                {
                    value = new LedSegmentState { SegmentIndex = segment };
                    _ledSegmentStates[segment] = value;
                }

                value.ControlledBySensors.Add(sensor);
            }
        }

        OnSensorStateChanged?.Invoke(sensor);
    }

    public void UpdateSensorBrightness(Sensor sensor, int brightness, SensorActivity activity)
    {
        lock (_lock)
        {
            var state = _sensorStates[sensor];
            state.CurrentBrightness = brightness;
            state.Activity = activity;
            state.LastActivity = DateTime.Now;
        }

        OnSensorStateChanged?.Invoke(sensor);
    }

    public void UpdateSensorEvent(ProximityEvent proximityEvent)
    {
        lock (_lock)
        {
            _startTimestamp ??= DateTime.Now;

            var state = _sensorStates[proximityEvent.Sensor];
            state.LastEvent = proximityEvent;
            state.EventCount++;
            state.LastActivity = proximityEvent.Timestamp;

            // Update charts
            int sensorIndex = (int)proximityEvent.Sensor - 1;
            BarChartSeries[sensorIndex].Data[0]++;
            ChartSeries[sensorIndex].Data.Add(new(proximityEvent.Timestamp, proximityEvent.Value));

            UpdateTimeLabelSpacing();
        }

        OnSensorStateChanged?.Invoke(proximityEvent.Sensor);
        OnChartDataChanged?.Invoke();
    }

    public void UpdateSensorConnection(Sensor sensor, bool isConnected)
    {
        lock (_lock)
        {
            _sensorStates[sensor].IsConnected = isConnected;
        }

        OnSensorStateChanged?.Invoke(sensor);
    }

    public void SetSegmentBrightnessForSensor(Sensor sensor, int segmentIndex, int brightness)
    {
        SetSegmentBrightnessForSensor(sensor, [segmentIndex], brightness);
    }

    public void SetSegmentBrightnessForSensor(Sensor sensor, List<int> segments, int brightness)
    {
        var affectedSegments = new HashSet<int>();

        lock (_lock)
        {
            foreach (var segmentIndex in segments)
            {
                if (!_ledSegmentStates.TryGetValue(segmentIndex, out LedSegmentState? state))
                {
                    state = new LedSegmentState { SegmentIndex = segmentIndex };
                    _ledSegmentStates[segmentIndex] = state;
                }

                state.SensorBrightnessRequests[sensor] = brightness;

                // Recalculate effective brightness (MAX of all requests)
                int newEffectiveBrightness = state.EffectiveBrightness;
                if (state.Brightness != newEffectiveBrightness)
                {
                    state.Brightness = newEffectiveBrightness;
                    state.LastUpdate = DateTime.Now;
                    affectedSegments.Add(segmentIndex);
                }
            }
        }

        // Notify outside the lock
        foreach (var segment in affectedSegments)
        {
            OnSegmentStateChanged?.Invoke(segment);
        }
    }

    public void ClearSensorBrightnessRequest(Sensor sensor, int segmentIndex)
    {
        ClearSensorBrightnessRequest(sensor, [segmentIndex]);
    }

    public void ClearSensorBrightnessRequest(Sensor sensor, List<int> segments)
    {
        var affectedSegments = new HashSet<int>();

        lock (_lock)
        {
            foreach (var segmentIndex in segments)
            {
                if (_ledSegmentStates.TryGetValue(segmentIndex, out var state))
                {
                    state.SensorBrightnessRequests.Remove(sensor);

                    // Recalculate effective brightness
                    int newEffectiveBrightness = state.EffectiveBrightness;
                    if (state.Brightness != newEffectiveBrightness)
                    {
                        state.Brightness = newEffectiveBrightness;
                        state.LastUpdate = DateTime.Now;
                        affectedSegments.Add(segmentIndex);
                    }
                }
            }
        }

        foreach (var segment in affectedSegments)
        {
            OnSegmentStateChanged?.Invoke(segment);
        }
    }

    public void UpdateSegmentColor(int segmentIndex, RGB color)
    {
        UpdateSegmentColor([segmentIndex], color);
    }

    public void UpdateSegmentColor(List<int> segments, RGB color)
    {
        lock (_lock)
        {
            foreach (var segmentIndex in segments)
            {
                if (_ledSegmentStates.TryGetValue(segmentIndex, out var state))
                {
                    state.Color = color;
                    state.LastUpdate = DateTime.Now;
                }
            }
        }

        foreach (var segment in segments)
        {
            OnSegmentStateChanged?.Invoke(segment);
        }
    }

    private void UpdateTimeLabelSpacing()
    {
        if (_startTimestamp is null)
            return;

        var elapsed = DateTime.Now - _startTimestamp.Value;
        _timeLabelSpacing = TimeSpan.FromTicks(elapsed.Ticks / 10);
    }
}
