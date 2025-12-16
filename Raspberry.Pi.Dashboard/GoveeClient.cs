using System.Text;
using System.Text.Json;

namespace Raspberry.Pi.Dashboard;

public class GoveeClient(HttpClient httpClient)
{
    private readonly HttpClient _httpClient = httpClient;
    private const string sku = "H618A";
    private const string deviceId = "25:F9:D6:09:86:46:08:31";
    private int httpCounter = 0;

    // You'll need to track current brightness since Govee API 
    // doesn't provide easy state queries
    private int _currentBrightness = 50;
    private readonly Dictionary<int, int> _segmentBrightness = [];
    private Task<int> GetCurrentBrightnessAsync()
    {
        return Task.FromResult(_currentBrightness);
    }

    private Task<int> GetCurrentBrightnessAsync(int[] segments)
    {
        // Assume all segments in the array have the same brightness
        // Return the brightness of the first segment
        if (segments.Length > 0 && _segmentBrightness.TryGetValue(segments[0], out var brightness))
        {
            return Task.FromResult(brightness);
        }
        return Task.FromResult(10); // Default
    }

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
        // Console.WriteLine($"Response: {respContent}");
        httpCounter++;
        Console.WriteLine("HTTP request sent " + httpCounter);
    }

    public async Task<GoveeDevicesResponse> GetDevicesAsync()
    {
        var resp = await _httpClient.GetAsync("user/devices");
        resp.EnsureSuccessStatusCode();
        var deviceResponse = await JsonSerializer.DeserializeAsync<GoveeDevicesResponse>(resp.Content.ReadAsStream());

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

    public Task SetColorRgbAsync(int r, int g, int b)
    {
        int rgb = RgbToInt(r, g, b);
        var capability = new
        {
            type = "devices.capabilities.color_setting",
            instance = "colorRgb",
            value = rgb
        };
        return SendCommandAsync(capability);
    }

    public Task SetColorTemperatureAsync(int kelvin)
    {
        var capability = new
        {
            type = "devices.capabilities.color_setting",
            instance = "colorTemperatureK",
            value = kelvin
        };
        return SendCommandAsync(capability);
    }

    public async Task SetSegmentBrightnessSmoothAsync(
            int[] segments,
            int targetBrightness,
            TimeSpan duration,
            CancellationToken cancellationToken = default)
    {
        const int steps = 5; // Number of incremental changes
        const int minDelayMs = 50; // Minimum delay between API calls

        // Calculate delay between steps
        int delayMs = Math.Max(minDelayMs, (int)(duration.TotalMilliseconds / steps));

        // Get current brightness - you'll need to track this or fetch from device
        int currentBrightness = await GetCurrentBrightnessAsync(segments);

        int brightnessStep = (targetBrightness - currentBrightness) / steps;

        for (int i = 1; i <= steps; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            int newBrightness = currentBrightness + (brightnessStep * i);

            // Clamp to 1-100 range
            newBrightness = Math.Clamp(newBrightness, 1, 100);

            await SetSegmentBrightnessAsync(segments, newBrightness);

            // Don't delay after the last step
            if (i < steps)
            {
                await Task.Delay(delayMs, cancellationToken);
            }
        }

        // Ensure we hit the exact target
        await SetSegmentBrightnessAsync(segments, targetBrightness);
    }

    public async Task SetBrightnessSmoothAsync(
        int targetBrightness,
        TimeSpan duration,
        CancellationToken cancellationToken = default)
    {
        const int steps = 20;
        const int minDelayMs = 50;

        int delayMs = Math.Max(minDelayMs, (int)(duration.TotalMilliseconds / steps));

        int currentBrightness = await GetCurrentBrightnessAsync();
        int brightnessStep = (targetBrightness - currentBrightness) / steps;

        for (int i = 1; i <= steps; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            int newBrightness = currentBrightness + (brightnessStep * i);
            newBrightness = Math.Clamp(newBrightness, 10, 100);

            await SetBrightnessAsync(newBrightness);

            if (i < steps)
            {
                await Task.Delay(delayMs, cancellationToken);
            }
        }

        await SetBrightnessAsync(targetBrightness);
    }


    // Update your existing methods to track state
    public async Task SetBrightnessAsync(int brightness /* 1-100 */)
    {
        var capability = new
        {
            type = "devices.capabilities.range",
            instance = "brightness",
            value = brightness
        };
        await SendCommandAsync(capability);
        _currentBrightness = brightness;
    }

    public async Task SetSegmentBrightnessAsync(int[] segments, int brightness)
    {
        var currentBrightness = await GetCurrentBrightnessAsync();
        if (currentBrightness == brightness)
        {
            Console.WriteLine("skipped because same brightness");
        }

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

        // Track brightness for each segment
        foreach (var segment in segments)
        {
            _segmentBrightness[segment] = brightness;
        }
    }
    private static int RgbToInt(int r, int g, int b)
    {
        return (r << 16) | (g << 8) | b;
    }
}
