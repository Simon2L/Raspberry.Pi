using Raspberry.Pi;


var govee = new GoveeLanClient();
bool found = await govee.DiscoverAsync();
if (!found)
{
    Console.WriteLine("No Govee device found.");
    return;
}
Console.WriteLine($"Found {govee.Model} at {govee.DeviceIp}");
await govee.TurnOnAsync();
await govee.SetColorAsync(255, 255, 255);
await govee.SetBrightnessAsync(20);

var sensor1 = new Vcnl4010(busId: 1);
var sensor2 = new Vcnl4010(busId: 3);

bool segment1On = false;
bool segment2On = false;

while (true)
{
    int proximity1 = sensor1.GetProximity();
    int proximity2 = sensor2.GetProximity();

    if (proximity1 > 5000)
    {
        await govee.SetSegmentAsync(start: 0, end: 6, r: 255, g: 255, b: 255, brightness: 100);
        segment1On = true;
    }
    else if (segment1On)
    {
        await govee.SetSegmentAsync(start: 0, end: 6, r: 255, g: 255, b: 255, brightness: 20);
        segment2On = false;
    }

    if (proximity2 > 5000)
    {
        await govee.SetSegmentAsync(start: 7, end: 14, r: 255, g: 255, b: 255, brightness: 100);
        segment2On = true;
    }
    else if (segment2On)
    {
        await govee.SetSegmentAsync(start: 7, end: 14, r: 255, g: 255, b: 255, brightness: 20);
        segment2On = false;
    }

    Console.WriteLine($"Sensor 1 Proximity: {proximity1}");
    Console.WriteLine($"Sensor 2 Proximity: {proximity2}");
    Thread.Sleep(1000);
}
