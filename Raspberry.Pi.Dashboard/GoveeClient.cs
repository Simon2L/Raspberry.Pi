using System.Text;
using System.Text.Json;

namespace Raspberry.Pi.Dashboard;

public record RGB(int R, int G, int B)
{

    public bool EqualsScuffed(RGB other)
    {
        return (R == other.R && G == other.G && B == other.B);
    }

    public int ToInt()
    {
        return (R << 16) | (G << 8) | B;
    }

};

public class GoveeClient(HttpClient httpClient)
{
    private readonly HttpClient _httpClient = httpClient;
    private const string sku = "H618A";
    private const string deviceId = "25:F9:D6:09:86:46:08:31";
    private int httpCounter = 0;

    // You'll need to track current brightness since Govee API 
    // doesn't provide easy state queries
    private int _currentBrightness = 0;
    private readonly Dictionary<int, int> _segmentBrightness = [];
    private readonly Dictionary<int, RGB> _segmentColor = [];

    private Task<int> GetCurrentBrightnessAsync()
    {
        return Task.FromResult(_currentBrightness);
    }

    private void SetCurrentBrightness(int brightness)
    {
        _segmentBrightness.Clear();
        _currentBrightness = brightness;
    }

    private Task<int> GetCurrentBrightnessAsync(int[] segments)
    {
        // Assume all segments in the array have the same brightness
        // Return the brightness of the first segment
        if (segments.Length > 0 && _segmentBrightness.TryGetValue(segments[0], out var brightness))
        {
            return Task.FromResult(brightness);
        }
        return Task.FromResult(_currentBrightness); // Default
    }

    public async Task SendCommandAsync(object capability)
    {
        Console.WriteLine($"HTTP sending request {capability}");
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
        // httpCounter++;
        Console.WriteLine();
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

    public Task SetColorRgbAsync(RGB rgb)
    {
        // int rgb = RgbToInt(r, g, b);
        var capability = new
        {
            type = "devices.capabilities.color_setting",
            instance = "colorRgb",
            value = rgb.ToInt(),
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
        const int steps = 1; // Number of incremental changes
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
            Console.WriteLine($"newBrightness: {newBrightness}, currentBrightness: {currentBrightness}");

            // Clamp to 1-100 range
            newBrightness = Math.Clamp(newBrightness, 1, 100);

            Console.WriteLine($"setting new brightness: {newBrightness} for {string.Join(',', segments)}");
            await SetSegmentBrightnessAsync(segments, newBrightness);

            // Don't delay after the last step
            if (i < steps)
            {
                await Task.Delay(delayMs, cancellationToken);
            }
        }

        // Ensure we hit the exact target
        // await SetSegmentBrightnessAsync(segments, targetBrightness);
    }

    /*
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
    }*/


    // Update your existing methods to track state
    public async Task SetBrightnessAsync(int brightness /* 1-100 */)
    {
        var currentBrightness = await GetCurrentBrightnessAsync();
        if (currentBrightness == brightness)
        {
            Console.WriteLine("skip sent");
            return; 
        }

        var capability = new
        {
            type = "devices.capabilities.range",
            instance = "brightness",
            value = brightness
        };
        await SendCommandAsync(capability);
        SetCurrentBrightness(brightness);
    }

    public async Task SetSegmentBrightnessAsync(int[] segments, int brightness)
    {
        var currentBrightness = await GetCurrentBrightnessAsync(segments);
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

    public async Task SetSegmentColorAsync(int[] segments, RGB rgb)
    {
        var currentRgb = GetCurrentColor(segments);
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
                value = rgb.ToInt()
            }
        };

        await SendCommandAsync(capability);

        foreach (var segment in segments)
        {
            _segmentColor[segment] = rgb;
        }
    }

    public RGB GetCurrentColor(int[] segments)
    {
        if (segments.Length > 0 && _segmentColor.TryGetValue(segments[0], out var rgb))
        {
            return rgb;
        }
        return new RGB(255, 0, 0);
        //return _currentBrightness; // Default
    }


    private static int RgbToInt(int r, int g, int b)
    {
        return (r << 16) | (g << 8) | b;
    }

}
