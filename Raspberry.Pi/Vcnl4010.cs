using System.Device.I2c;


// | Register | Purpose |
// | -------- | -------------------------- |
// | `0x80`   | Command register |
// | `0x81`   | Proximity rate |
// | `0x82`   | Ambient light command      |
// | `0x85`   | Proximity data (high byte) |
// | `0x86`   | Proximity data (low byte)  |


public class Vcnl4010
{
    private readonly I2cDevice _device;

    private const byte COMMAND_REG = 0x80;
    private const byte PROXIMITY_DATA_HIGH = 0x85;
    private const byte PROXIMITY_DATA_LOW = 0x86;

    public Vcnl4010(int busId, int address = 0x13)
    {
        var settings = new I2cConnectionSettings(busId, address);
        _device = I2cDevice.Create(settings);

        Initialize();
    }

    private void Initialize()
    {
        // Turn on proximity measurement
        // COMMAND_REG: bit 3 = 1 → enable proximity
        _device.Write(new byte[] { COMMAND_REG, 0x08 });
    }

    public int GetProximity()
    {
        // Read high + low bytes
        _device.WriteByte(PROXIMITY_DATA_HIGH);
        byte high = _device.ReadByte();

        _device.WriteByte(PROXIMITY_DATA_LOW);
        byte low = _device.ReadByte();

        return (high << 8) | low;
    }
}
