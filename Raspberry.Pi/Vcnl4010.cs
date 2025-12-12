using System.Device.I2c;

namespace Raspberry.Pi;

public class Vcnl4010
{
    private readonly I2cDevice _device;

    private const byte REG_COMMAND = 0x80;
    private const byte REG_PROX_RATE = 0x82;
    private const byte REG_IR_LED = 0x83;
    private const byte REG_PROX_DATA = 0x87; // High byte start

    public Vcnl4010(int busId, int address = 0x13)
    {
        var settings = new I2cConnectionSettings(busId, address);
        _device = I2cDevice.Create(settings);

        Initialize();
    }

    private void Initialize()
    {
        // Reset command register
        _device.Write(new byte[] { REG_COMMAND, 0x00 });

        // IR LED current (max value)
        _device.Write(new byte[] { REG_IR_LED, 0x0F });

        // Proximity measurement rate (highest)
        _device.Write(new byte[] { REG_PROX_RATE, 0x07 });

        // Enable proximity + start a measurement
        // Bits: 0..3 = 1 → start single prox, enable prox engine, ALS optional
        _device.Write(new byte[] { REG_COMMAND, 0x0F });
    }

    public int GetProximity()
    {
        byte[] buffer = new byte[2];

        // Set pointer to PROX_DATA high byte
        _device.WriteByte(REG_PROX_DATA);

        // Read 2 bytes (auto increments to 0x88)
        _device.Read(buffer);

        return (buffer[0] << 8) | buffer[1];
    }
}