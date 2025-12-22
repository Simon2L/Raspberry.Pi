using Raspberry.Pi.Dashboard.Domain;

namespace Raspberry.Pi.Dashboard;

public class LedSegmentState
{
    public int SegmentIndex { get; init; }
    public int Brightness { get; set; }
    public RGB Color { get; set; } = new(255, 255, 255);
    public DateTime LastUpdate { get; set; }

    // Track which sensors are controlling this segment
    public HashSet<Sensor> ControlledBySensors { get; set; } = [];

    // Track brightness requests from each sensor
    public Dictionary<Sensor, int> SensorBrightnessRequests { get; set; } = [];

    // The effective brightness is the MAX of all sensor requests
    public int EffectiveBrightness => SensorBrightnessRequests.Values.DefaultIfEmpty(1).Max();
}

// 5. Blazor Component - Visualizing Segment Control
/*
@page "/segments"
@inject IApplicationStateService AppState
@implements IDisposable

<h3>LED Segment Visualization</h3>

<div class="segment-grid">
    @for (int i = 0; i < 100; i++)
    {
        var segment = AppState.GetSegmentState(i);
        var sensors = AppState.GetSensorsControlling(i);
        
        <div class="segment" style="background-color: rgba(255, 255, 255, @(segment.Brightness / 100.0))">
            <div class="segment-info">
                <small>Seg @i</small>
                <small>Br: @segment.EffectiveBrightness</small>
                @if (sensors.Any())
                {
                    <small>@string.Join(", ", sensors.Select(s => $"S{(int)s}"))</small>
                }
                @if (segment.SensorBrightnessRequests.Any())
                {
                    foreach (var req in segment.SensorBrightnessRequests)
                    {
                        <tiny>S@((int)req.Key): @req.Value</tiny>
                    }
                }
            </div>
        </div>
    }
</div>

@code {
    protected override void OnInitialized()
    {
        AppState.OnSegmentStateChanged += OnSegmentChanged;
    }

    private void OnSegmentChanged(int segmentIndex)
    {
        InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        AppState.OnSegmentStateChanged -= OnSegmentChanged;
    }
}
*/