using Raspberry.Pi.Dashboard.Domain;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using static System.Collections.Specialized.BitVector32;

namespace Raspberry.Pi.Dashboard.Services;

public class GoveeClient(
    HttpClient httpClient,
    ISettingsService settingsService,
    IApplicationStateService appState)
{
    private readonly HttpClient _httpClient = httpClient;
    private const string sku = "H618A";
    private const string deviceId = "25:F9:D6:09:86:46:08:31";
    private readonly ISettingsService _settingsService = settingsService;
    private readonly IApplicationStateService _appState = appState;

    public async Task SendCommandAsync(object capability)
    {
        var payload = new
        {
            requestId = Guid.NewGuid().ToString(),
            payload = new
            {
                sku = sku,
                device = deviceId,
                capability = capability,
            }
        };
        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var resp = await _httpClient.PostAsync("device/control", content);
        resp.EnsureSuccessStatusCode();
        var respContent = await resp.Content.ReadAsStringAsync();
        Console.WriteLine(respContent);
        Console.WriteLine();
    }

    public async Task<GoveeDevicesResponse> GetDevicesAsync()
    {
        var resp = await _httpClient.GetAsync("user/devices");
        resp.EnsureSuccessStatusCode();
        var deviceResponse = await JsonSerializer.DeserializeAsync<GoveeDevicesResponse>(
            resp.Content.ReadAsStream());

        return deviceResponse ?? new();
    }

    public Task TurnOnOffAsync(bool on)
    {
        var capability = new
        {
            type = "devices.capabilities.on_off",
            instance = "powerSwitch",
            value = on ? 1 : 0
        };
        return SendCommandAsync(capability);
    }

    public async Task SetColorRgbAsync(RGB rgb)
    {
        var capability = new
        {
            type = "devices.capabilities.color_setting",
            instance = "colorRgb",
            value = rgb.ToInt(),
        };
        await SendCommandAsync(capability);
    }

    public async Task SetColorTemperatureAsync(int kelvin)
    {
        var capability = new
        {
            type = "devices.capabilities.color_setting",
            instance = "colorTemperatureK",
            value = kelvin
        };
        await SendCommandAsync(capability);
    }

    public async Task SetSegmentBrightnessSmoothAsync(
        List<int> segments,
        int targetBrightness,
        TimeSpan duration,
        Sensor sensor,
        CancellationToken cancellationToken = default)
    {
        var settings = _settingsService.GetSettings();
        const int minDelayMs = 100;

        int delayMs = Math.Max(minDelayMs, (int)(duration.TotalMilliseconds / settings.Steps));

        int currentBrightness = GetCurrentBrightnessFromState(segments);

        int brightnessStep = (targetBrightness - currentBrightness) / settings.Steps;

        for (int i = 1; i <= settings.Steps; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            int newBrightness = currentBrightness + (brightnessStep * i);

            newBrightness = Math.Clamp(newBrightness, 1, 100);

            _appState.SetSegmentBrightnessForSensor(sensor, segments, newBrightness);
            _appState.UpdateSensorBrightness(sensor, newBrightness, SensorActivity.Increasing);
            await SetSegmentBrightnessAsync(sensor, segments, newBrightness);

            if (i < settings.Steps)
            {
                await Task.Delay(delayMs, cancellationToken);
            }
        }
    }

    public async Task SetBrightnessAsync(int brightness)
    {
        var capability = new
        {
            type = "devices.capabilities.range",
            instance = "brightness",
            value = brightness
        };
        await SendCommandAsync(capability);
    }

    public async Task SetSegmentBrightnessAsync(Sensor sensor, List<int> segments, int brightness)
    {
        Console.WriteLine($"setting new brightness: {brightness} for {string.Join(',', segments)}");

        var capability = new
        {
            type = "devices.capabilities.segment_color_setting",
            instance = "segmentedBrightness",
            value = new
            {
                segment = segments,
                brightness = brightness
            }
        };

        await SendCommandAsync(capability);
    }

    public async Task SetSegmentColorAsync(List<int> segments, RGB rgb)
    {
        var currentRgb = GetCurrentColorFromState(segments);

        if (currentRgb.EqualsScuffed(rgb))
        {
            Console.WriteLine("Skipped because same RGB");
            return;
        }

        var capability = new
        {
            type = "devices.capabilities.segment_color_setting",
            instance = "segmentedColorRgb",
            value = new
            {
                segment = segments,
                rgb = rgb.ToInt()
            }
        };

        await SendCommandAsync(capability);

        // Update state after successful command
        _appState.UpdateSegmentColor(segments, rgb);
    }

    private int GetCurrentBrightnessFromState(List<int> segments)
    {
        if (segments.Count == 0)
            return 1;

        List<int> segmentStatesBrightness = [];
        foreach (var seg in segments)
        {
            var segmentState = _appState.GetSegmentState(seg);
            segmentStatesBrightness.Add(segmentState.Brightness);
        }

        return segmentStatesBrightness.DefaultIfEmpty(1).Min();
    }

    private RGB GetCurrentColorFromState(List<int> segments)
    {
        if (segments.Count == 0)
            return new RGB(0, 0, 0);

        try
        {
            var segmentState = _appState.GetSegmentState(segments[0]);
            return segmentState.Color;
        }
        catch
        {
            return new RGB(0, 0, 0);
        }
    }
}