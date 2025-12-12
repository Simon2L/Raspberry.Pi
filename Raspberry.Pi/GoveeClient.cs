namespace Raspberry.Pi;

using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public static class GoveeApi
{
    private static readonly string ApiKey = "560fbc36-952c-42f7-925b-a7151394f3c5";
    private static readonly string DeviceMac = "98:17:3C:57:DD:EE";
    private static readonly string Sku = "H618A";
    private static readonly string BaseUrl = "https://openapi.api.govee.com/router/api/v1/device/control";

    private static readonly HttpClient client = new();

    static void Main()
    {
        client.DefaultRequestHeaders.Add("Govee-API-Key", ApiKey);

        // Examples
        //await TurnOnOff(true);
        //await SetBrightness(10);
        //await SetColorRgb(255, 255, 255); // Red
        //await SetSegmentBrightness(new int[] { 0, 1, 2 }, 60);
        //await SetSegmentColor(new int[] { 0, 1, 2 }, 0, 255, 0); // Green
    }

    private static async Task SendCommand(object payload)
    {
        var requestObj = new
        {
            requestId = Guid.NewGuid().ToString(),
            payload
        };

        var json = JsonSerializer.Serialize(requestObj, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });

        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await client.PostAsync(BaseUrl, content);
        var responseContent = await response.Content.ReadAsStringAsync();
        Console.WriteLine(responseContent);
    }

    // Turn device on/off
    public static async Task TurnOnOff(bool on)
    {
        var payload = new
        {
            sku = Sku,
            device = DeviceMac,
            capability = new
            {
                type = "devices.capabilities.on_off",
                instance = "powerSwitch",
                value = on ? 1 : 0
            }
        };

        await SendCommand(payload);
    }

    // Set brightness 0-100
    public static async Task SetBrightness(int brightness)
    {
        var payload = new
        {
            sku = Sku,
            device = DeviceMac,
            capability = new
            {
                type = "devices.capabilities.range",
                instance = "brightness",
                value = brightness
            }
        };

        await SendCommand(payload);
    }

    // Set RGB color
    public static async Task SetColorRgb(int r, int g, int b)
    {
        int rgb = (r << 16) + (g << 8) + b;

        var payload = new
        {
            sku = Sku,
            device = DeviceMac,
            capability = new
            {
                type = "devices.capabilities.color_setting",
                instance = "colorRgb",
                value = rgb
            }
        };

        await SendCommand(payload);
    }

    // Set segmented brightness
    public static async Task SetSegmentBrightness(int[] segments, int brightness)
    {
        var payload = new
        {
            sku = Sku,
            device = DeviceMac,
            capability = new
            {
                type = "devices.capabilities.segment_color_setting",
                instance = "segmentedBrightness",
                value = new
                {
                    segment = segments,
                    brightness = brightness
                }
            }
        };

        await SendCommand(payload);
    }

    // Set segmented color
    public static async Task SetSegmentColor(int[] segments, int r, int g, int b)
    {
        int rgb = (r << 16) + (g << 8) + b;

        var payload = new
        {
            sku = Sku,
            device = DeviceMac,
            capability = new
            {
                type = "devices.capabilities.segment_color_setting",
                instance = "segmentedColorRgb",
                value = new
                {
                    segment = segments,
                    rgb = rgb
                }
            }
        };

        await SendCommand(payload);
    }
}

