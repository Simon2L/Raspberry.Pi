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

while (true)
{
    int proximity1 = sensor1.GetProximity();
    int proximity2 = sensor2.GetProximity();

    if (proximity1 > 2500)
    {
        await govee.SetSegmentBrightnessAsync(start: 0, end: 6, 100, 255, 255, 255);
    }

    if (proximity2 > 2500)
    {
        await govee.SetSegmentBrightnessAsync(start: 7, end: 14, 100, 255, 255, 255);
    }

    Console.WriteLine($"Sensor 1 Proximity: {proximity1}");
    Console.WriteLine($"Sensor 2 Proximity: {proximity2}");
    Thread.Sleep(1000);
}
