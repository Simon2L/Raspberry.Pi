using Raspberry.Pi;



var sensor1 = new Vcnl4010(busId: 1);
// var sensor2 = new Vcnl4010(busId: 3);

while (true)
{
    int proximity1 = sensor1.GetProximity();
    // int proximity2 = sensor2.GetProximity();
    Console.WriteLine($"Sensor 1 Proximity: {proximity1}");
    // Console.WriteLine($"Sensor 2 Proximity: {proximity2}");
    Thread.Sleep(500);
}
