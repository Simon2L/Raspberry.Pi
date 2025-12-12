using System.Device.I2c;

namespace Raspberry.Pi
{


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
            // 1. Set IR LED current (max = 200 mA / 20 = 10 decimal)
            _device.Write(new byte[] { REG_IR_LED, 0x0A });

            // 2. Set proximity rate (e.g., 31.25 measurements/s)
            _device.Write(new byte[] { REG_PROX_RATE, 0x05 });

            // 3. Enable proximity continuous mode
            // REG_COMMAND bit 3 = proximity enable (continuous)
            _device.Write(new byte[] { REG_COMMAND, 0x08 });
        }

        public int GetProximity()
        {
            byte[] data = new byte[2];

            // Send starting register (0x87) then read 2 bytes
            _device.WriteByte(REG_PROX_DATA);
            _device.Read(data);

            int high = data[0];
            int low = data[1];

            return (high << 8) | low;
        }
    }
}