using Raspberry.Pi;


var goveeClient = new GoveeClient();
// await goveeClient.GetDevices();
/*
await goveeClient.TurnOnOff(true);
await goveeClient.SetColorRgb(255, 255, 255);
await goveeClient.SetBrightness(5);
*/

var sensor1 = new Vcnl4010(busId: 1);
var sensor2 = new Vcnl4010(busId: 3);

bool segment1On = false;
bool segment2On = false;

const int proximityThreshold = 3000;

while (true)
{
    int proximity1 = sensor1.GetProximity();
    int proximity2 = sensor2.GetProximity();

    if (proximity1 > proximityThreshold && !segment1On)
    {
        await goveeClient.SetSegmentBrightness([0, 1, 2, 3, 4, 5, 6], 100);
        segment1On = true;
    }
    else if (segment1On)
    {
        await goveeClient.SetSegmentBrightness([0, 1, 2, 3, 4, 5, 6], 5);
        segment2On = false;
    }

    if (proximity2 > proximityThreshold && !segment2On)
    {
        await goveeClient.SetSegmentBrightness([7, 8, 9, 10, 11, 12, 13, 14], 100);
        segment2On = true;
    }
    else if (segment2On)
    {
        await goveeClient.SetSegmentBrightness([7, 8, 9, 10, 11, 12, 13, 14], 5);
        segment2On = false;
    }

    Console.WriteLine($"Sensor 1 Proximity: {proximity1}");
    Console.WriteLine($"Sensor 2 Proximity: {proximity2}");
    Thread.Sleep(1000);
}
