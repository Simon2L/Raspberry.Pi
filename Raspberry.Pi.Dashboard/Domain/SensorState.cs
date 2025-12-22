using Raspberry.Pi.Dashboard.Events.Models;

namespace Raspberry.Pi.Dashboard.Domain;

public class SensorState
{
    public Sensor Sensor { get; init; }
    public int CurrentBrightness { get; set; }
    public SensorActivity Activity { get; set; } = SensorActivity.Idle;
    public DateTime LastActivity { get; set; }
    public bool IsConnected { get; set; } = true;
    public ProximityEvent? LastEvent { get; set; }
    public int EventCount { get; set; }
    public List<int> ControlledSegments { get; set; } = []; // Which segments this sensor controls

    // Operational state (internal to handler)
    public SemaphoreSlim Semaphore { get; } = new(1, 1);
    public CancellationTokenSource? DecreaseCts { get; set; }
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