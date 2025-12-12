using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json.Nodes;

namespace Raspberry.Pi;

public class GoveeLanClient
{
    private const string MULTICAST_ADDR = "239.255.255.250";
    private const int DISCOVERY_PORT = 4001;
    private const int LISTEN_PORT = 4002;
    private const int CONTROL_PORT = 4003;

    public string DeviceIp { get; private set; }
    public string DeviceId { get; private set; }
    public string Model { get; private set; }

    // ---------------------------
    // DISCOVERY
    // ---------------------------
    public async Task<bool> DiscoverAsync(int timeoutMs = 3000)
    {
        using var scanClient = new UdpClient();
        scanClient.JoinMulticastGroup(IPAddress.Parse(MULTICAST_ADDR));

        // Send scan request
        var scanMsg = Encoding.UTF8.GetBytes(
            @"{""msg"":{""cmd"":""scan"",""data"":{""account_topic"":""reserve""}}}"
        );

        await scanClient.SendAsync(scanMsg, scanMsg.Length, MULTICAST_ADDR, DISCOVERY_PORT);

        using var listener = new UdpClient(LISTEN_PORT);
        var receiveTask = listener.ReceiveAsync();
        var completed = await Task.WhenAny(receiveTask, Task.Delay(timeoutMs));

        if (completed != receiveTask)
            return false; // no response

        var result = receiveTask.Result;
        string respJson = Encoding.UTF8.GetString(result.Buffer);

        try
        {
            var json = JsonNode.Parse(respJson);

            DeviceIp = json?["msg"]?["data"]?["ip"]?.ToString();
            DeviceId = json?["msg"]?["data"]?["device"]?.ToString();
            Model = json?["msg"]?["data"]?["sku"]?.ToString();

            return DeviceIp != null;
        }
        catch
        {
            return false;
        }
    }

    // ---------------------------
    // SEND ANY COMMAND
    // ---------------------------
    private async Task SendCommandAsync(string json)
    {
        if (DeviceIp is null)
            throw new InvalidOperationException("Device not discovered. Call DiscoverAsync() first.");

        using var udp = new UdpClient();
        udp.Connect(DeviceIp, CONTROL_PORT);

        byte[] bytes = Encoding.UTF8.GetBytes(json);
        await udp.SendAsync(bytes, bytes.Length);
    }

    // ---------------------------
    // BASIC COMMANDS
    // ---------------------------
    public Task TurnOnAsync() =>
        SendCommandAsync(@"{""msg"":{""cmd"":""turn"",""data"":{""value"":1}}}");

    public Task TurnOffAsync() =>
        SendCommandAsync(@"{""msg"":{""cmd"":""turn"",""data"":{""value"":0}}}");

    public Task SetBrightnessAsync(int value)
    {
        value = Math.Clamp(value, 0, 100);
        return SendCommandAsync(
            $@"{{""msg"":{{""cmd"":""brightness"",""data"":{{""value"":{value}}}}}}}"
        );
    }

    // 2000K - 9000K (0 då r g b)
    public Task SetColorAsync(int r, int g, int b)
    {
        r = Math.Clamp(r, 0, 255);
        g = Math.Clamp(g, 0, 255);
        b = Math.Clamp(b, 0, 255);

        return SendCommandAsync(
            $@"{{""msg"":{{""cmd"":""colorwc"",""data"":{{""color"":{{""r"":{r},""g"":{g},""b"":{b}}},""colorTemInKelvin"":0}}}}}}"
        );

    }

    // ---------------------------
    // SEGMENTED LIGHTING
    // ---------------------------
    public Task SetSegmentAsync(int start, int end, int r, int g, int b, int brightness = 100)
    {
        r = Math.Clamp(r, 0, 255);
        g = Math.Clamp(g, 0, 255);
        b = Math.Clamp(b, 0, 255);
        brightness = Math.Clamp(brightness, 1, 100);

        // include onOff and brightness at the data level to avoid devices interpreting the command as "off"
        string json =
        $@"{{
        ""msg"": {{
            ""cmd"": ""colorwc"",
            ""data"": {{
                ""onOff"": 1,
                ""brightness"": {brightness},
                ""segments"": [
                    {{
                        ""start"": {start},
                        ""end"": {end},
                        ""color"": {{ ""r"": {r}, ""g"": {g}, ""b"": {b} }},
                        ""colorTemInKelvin"": 0
                    }}
                ]
            }}
        }}
    }}";

        return SendCommandAsync(json);
    }

    public Task SetSegmentBrightnessAsync(int start, int end, int percent, int r, int g, int b)
    {
        percent = Math.Clamp(percent, 0, 100);
        double scale = percent / 100.0;

        int sr = (int)Math.Round(r * scale);
        int sg = (int)Math.Round(g * scale);
        int sb = (int)Math.Round(b * scale);

        // use the SetSegmentAsync (which includes onOff + brightness)
        return SetSegmentAsync(start, end, sr, sg, sb, brightness: Math.Max(1, percent));
    }

    public Task SetSegmentsAsync((int start, int end, int r, int g, int b)[] segs)
    {
        var sb = new StringBuilder();
        sb.Append(@"{""msg"":{""cmd"":""colorwc"",""data"":{""segments"":[");

        for (int i = 0; i < segs.Length; i++)
        {
            var s = segs[i];
            sb.Append(
                $@"{{""start"":{s.start},""end"":{s.end},""r"":{s.r},""g"":{s.g},""b"":{s.b}}}"
            );
            if (i < segs.Length - 1)
                sb.Append(",");
        }

        sb.Append("]}}}");

        return SendCommandAsync(sb.ToString());
    }
}
