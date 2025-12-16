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

public class GoveeClient(HttpClient httpClient, ISettingsService settingsService)
{
    private readonly HttpClient _httpClient = httpClient;
    private const string sku = "H618A";
    private const string deviceId = "25:F9:D6:09:86:46:08:31";
    private readonly ISettingsService _settingsService = settingsService;

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

    private Task<int> GetCurrentBrightnessAsync(List<int> segments)
    {
        if (segments.Count > 0 && _segmentBrightness.TryGetValue(segments.First(), out var brightness))
        {
            return Task.FromResult(brightness);
        }
        return Task.FromResult(_currentBrightness);
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
        Console.WriteLine($"content: {content}");
        Console.WriteLine($"HTTP sending request {content}");
        var resp = await _httpClient.PostAsync("device/control", content);
        resp.EnsureSuccessStatusCode();
        var respContent = await resp.Content.ReadAsStringAsync();
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
            List<int> segments,
            int targetBrightness,
            TimeSpan duration,
            CancellationToken cancellationToken = default)
    {
        var settings = _settingsService.GetSettings();
        const int minDelayMs = 100;

        int delayMs = Math.Max(minDelayMs, (int)(duration.TotalMilliseconds / settings.Steps));

        int currentBrightness = await GetCurrentBrightnessAsync(segments);

        int brightnessStep = (targetBrightness - currentBrightness) / settings.Steps;

        for (int i = 1; i <= settings.Steps; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            int newBrightness = currentBrightness + (brightnessStep * i);
            Console.WriteLine($"newBrightness: {newBrightness}, currentBrightness: {currentBrightness}");

            newBrightness = Math.Clamp(newBrightness, 1, 100);

            Console.WriteLine($"setting new brightness: {newBrightness} for {string.Join(',', segments)}");
            await SetSegmentBrightnessAsync(segments, newBrightness);

            if (i < settings.Steps)
            {
                await Task.Delay(delayMs, cancellationToken);
            }
        }

        // Ensure we hit the exact target
        // await SetSegmentBrightnessAsync(segments, targetBrightness);
    }

    public async Task SetBrightnessAsync(int brightness)
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
            brightness = brightness
        };
        await SendCommandAsync(capability);
        SetCurrentBrightness(brightness);
    }

    public async Task SetSegmentBrightnessAsync(List<int> segments, int brightness)
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

    public async Task SetSegmentColorAsync(List<int> segments, RGB rgb)
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
                rgb = rgb.ToInt()
            }
        };

        await SendCommandAsync(capability);

        foreach (var segment in segments)
        {
            _segmentColor[segment] = rgb;
        }
    }

    public RGB GetCurrentColor(List<int> segments)
    {
        if (segments.Count > 0 && _segmentColor.TryGetValue(segments.First(), out var rgb))
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
