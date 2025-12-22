using MudBlazor;
using Raspberry.Pi.Dashboard.Domain;
using Raspberry.Pi.Dashboard.Events.Models;

namespace Raspberry.Pi.Dashboard.Services;

// 2. Updated Application State Service
public interface IApplicationStateService
{
    // Sensor state
    IReadOnlyDictionary<Sensor, SensorState> SensorStates { get; }
    SensorState GetSensorState(Sensor sensor);

    // LED segment state
    IReadOnlyDictionary<int, LedSegmentState> LedSegmentStates { get; }
    LedSegmentState GetSegmentState(int segmentIndex);
    List<int> GetSegmentsControlledBy(Sensor sensor);
    List<Sensor> GetSensorsControlling(int segmentIndex);

    // Chart data
    List<TimeSeriesChartSeries> ChartSeries { get; set; }
    List<ChartSeries> BarChartSeries { get; set;  }
    string[] BarChartXAxisLabels { get; }
    TimeSpan TimeLabelSpacing { get; }

    // Update methods
    void ConfigureSensorSegments(Sensor sensor, List<int> segments);
    void UpdateSensorBrightness(Sensor sensor, int brightness, SensorActivity activity);
    void UpdateSensorEvent(ProximityEvent proximityEvent);
    void UpdateSensorConnection(Sensor sensor, bool isConnected);

    // Segment updates - now sensor-aware
    void SetSegmentBrightnessForSensor(Sensor sensor, int segmentIndex, int brightness);
    void SetSegmentBrightnessForSensor(Sensor sensor, List<int> segments, int brightness);
    void ClearSensorBrightnessRequest(Sensor sensor, int segmentIndex);
    void ClearSensorBrightnessRequest(Sensor sensor, List<int> segments);

    void UpdateSegmentColor(int segmentIndex, RGB color);
    void UpdateSegmentColor(List<int> segments, RGB color);

    // Events
    event Action<Sensor>? OnSensorStateChanged;
    event Action<int>? OnSegmentStateChanged;
    event Action? OnChartDataChanged;
}
