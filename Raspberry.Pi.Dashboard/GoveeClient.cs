using System.Text;
using System.Text.Json;

namespace Raspberry.Pi.Dashboard;

public class GoveeClient(HttpClient httpClient)
{
    private readonly HttpClient _httpClient = httpClient;
    private const string sku = "H618A";
    private const string deviceId = "25:F9:D6:09:86:46:08:31";

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
        Console.WriteLine($"Response: {respContent}");
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

    public Task SetBrightnessAsync(int brightness /* 1-100 */)
    {
        var capability = new
        {
            type = "devices.capabilities.range",
            instance = "brightness",
            value = brightness
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

    /// <summary>
    /// 0-14 segments?
    /// </summary>
    /// <param name="segments">0 to 14?</param>
    /// <param name="r"></param>
    /// <param name="g"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public Task SetSegmentColorAsync(int[] segments, int r, int g, int b)
    {
        int rgb = RgbToInt(r, g, b);
        var capability = new
        {
            type = "devices.capabilities.segment_color_setting",
            instance = "segmentedColorRgb",
            value = new
            {
                segment = segments,
                rgb = rgb
            }
        };
        return SendCommandAsync(capability);
    }

    public Task SetSegmentBrightnessAsync(int[] segments, int brightness)
    {
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
        return SendCommandAsync(capability);
    }

    /*
    public Task SetSegmentAsync(int[] segments, int r, int g, int b, int brightness)
    {
        int rgb = RgbToInt(r, g, b);
        var capability = new
        {
            type = "devices.capabilities.segment_color_setting",
            instance = "segmentedBrightness", // kolla upp i dokumentationen
            value = new
            {
                segment = segments,
                brightness = brightness,
                rgb = rgb
            }
        };
        return SendCommandAsync(capability);
    }
    */

    private static int RgbToInt(int r, int g, int b)
    {
        return (r << 16) | (g << 8) | b;
    }
}
