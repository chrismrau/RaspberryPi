
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.Gpio;
using Windows.Devices.I2c;
using Windows.UI;

namespace RgbSensor
{
    //Create a class for the raw color data (Red, Green, Blue, Clear)
    public class ColorData
    {
        public UInt16 Red { get; set; }
        public UInt16 Green { get; set; }
        public UInt16 Blue { get; set; }
        public UInt16 Clear { get; set; }
    }

    //Create a class for the RGB data (Red, Green, Blue)
    public class RgbData
    {
        public Int32 Red { get; set; }
        public Int32 Green { get; set; }
        public Int32 Blue { get; set; }

        #region Overrides of Object

        public override Boolean Equals(Object obj)
        {
            var that = obj as RgbData;
            if (that == null) return false;

            return
                this.Red == that.Red &&
                this.Green == that.Green &&
                this.Blue == that.Blue;
        }

        public override Int32 GetHashCode()
        {
            return 0x08080808;
        }

        #endregion
    }

    public class Tcs34725
    {
        //Address values set according to the datasheet: http://www.adafruit.com/datasheets/TCS34725.pdf
        const Byte TCS34725_ADDRESS = 0x29;

        const Byte TCS34725_ENABLE = 0x00;
        const Byte TCS34725_ENABLE_PON = 0x01; //Power on: 1 activates the internal oscillator, 0 disables it
        const Byte TCS34725_ENABLE_AEN = 0x02; //RGBC Enable: 1 actives the ADC, 0 disables it 

        const Byte TCS34725_ID = 0x12;
        const Byte TCS34725_ATIME = 0x01;   //Integration time
        const Byte TCS34725_CONTROL = 0x0F; //Set the gain level for the sensor

        const Byte TCS34725_COMMAND_BIT = 0x80; // Have to | addresses with this value when asking for values

        //String for the friendly name of the I2C bus 
        const String I2_C_CONTROLLER_NAME = "I2C1";
        //Create an I2C device
        I2cDevice _colorSensor = null;

        //Create a GPIO Controller for the LED pin on the sensor
        GpioController _gpio;
        //Create a GPIO pin for the LED pin on the sensor
        GpioPin _ledControlGPIOPin;
        //Create a variable to store the GPIO pin number for the sensor LED
        readonly Int32 _ledControlPin;

        //Create a list of common colors for approximations
        readonly String[] _limitColorList =
        {
            "Black", "White", "Blue", "Red",
            "Green", "Purple", "Yellow", "Orange",
            "DarkSlateBlue", "DarkGray", "Pink"
        };

        public struct KnownColor
        {
            public Color ColorValue;
            public readonly String ColorName;

            public KnownColor(Color value, String name)
            {
                ColorValue = value;
                ColorName = name;
            }
        };
        List<KnownColor> _colorList;

        public Tcs34725(Int32 ledControlPin = 12)
        {
            Debug.WriteLine("New TCS34725");
            //Set the LED control pin
            _ledControlPin = ledControlPin;
        }

        void InitColorList()
        {
            _colorList = new List<KnownColor>();

            foreach (var property in typeof(Colors).GetProperties())
            {
                if (!_limitColorList.Contains(property.Name)) continue;

                var temp = new KnownColor((Color)property.GetValue(null), property.Name);
                _colorList.Add(temp);
            }
        }

        public async Task Initialize()
        {
            Debug.WriteLine("TCS34725::Initialize");

            try
            {
                var settings = new I2cConnectionSettings(TCS34725_ADDRESS)
                {
                    BusSpeed = I2cBusSpeed.FastMode
                };

                var query_string = I2cDevice.GetDeviceSelector(I2_C_CONTROLLER_NAME);
                var device_information_collection = await DeviceInformation.FindAllAsync(query_string);
                _colorSensor = await I2cDevice.FromIdAsync(device_information_collection[0].Id, settings);

                _gpio = GpioController.GetDefault();
                _ledControlGPIOPin = _gpio.OpenPin(_ledControlPin);
                _ledControlGPIOPin.SetDriveMode(GpioPinDriveMode.Output);

                InitColorList();
            }
            catch (Exception e)
            {
                Debug.WriteLine("Exception: " + e.Message + "\n" + e.StackTrace);
                throw;
            }

        }

        public enum ELedState { On, Off };
        ELedState _ledState = ELedState.On;
        public ELedState LedState
        {
            get { return _ledState; }
            set
            {
                Debug.WriteLine("TCS34725::LedState::set");
                if (_ledControlGPIOPin == null) return;

                var new_value = (value == ELedState.On ? GpioPinValue.High : GpioPinValue.Low);
                _ledControlGPIOPin.Write(new_value);
                _ledState = value;
            }
        }

        enum ETcs34725IntegrationTime
        {
            Tcs34725Integrationtime2_4Ms = 0xFF,   //2.4ms - 1 cycle    - Max Count: 1024
            Tcs34725Integrationtime24Ms = 0xF6,    //24ms  - 10 cycles  - Max Count: 10240
            Tcs34725Integrationtime50Ms = 0xEB,    //50ms  - 20 cycles  - Max Count: 20480 
            Tcs34725Integrationtime101Ms = 0xD5,   //101ms - 42 cycles  - Max Count: 43008
            Tcs34725Integrationtime154Ms = 0xC0,   //154ms - 64 cycles  - Max Count: 65535 
            Tcs34725Integrationtime700Ms = 0x00    //700ms - 256 cycles - Max Count: 65535 
        };

        ETcs34725IntegrationTime _tcs34725IntegrationTime = 
            ETcs34725IntegrationTime.Tcs34725Integrationtime700Ms;

        enum ETcs34725Gain
        {
            Tcs34725Gain_1X = 0x00,   // No gain 
            Tcs34725Gain_4X = 0x01,   // 2x gain
            Tcs34725Gain16X = 0x02,  // 16x gain
            Tcs34725Gain60X = 0x03   // 60x gain 
        };

        ETcs34725Gain _tcs34725Gain = ETcs34725Gain.Tcs34725Gain60X;

        Boolean _init = false;

        async void SetIntegrationTime(ETcs34725IntegrationTime integrationTime)
        {
            if (!_init) await Begin();
            _tcs34725IntegrationTime = integrationTime;
            var write_buffer = new Byte[] {
                TCS34725_ATIME | TCS34725_COMMAND_BIT, (Byte)_tcs34725IntegrationTime };
            _colorSensor.Write(write_buffer);
        }

        async void SetGain(ETcs34725Gain gain)
        {
            if (!_init) await Begin();
            _tcs34725Gain = gain;
            var write_buffer = new Byte[] {
                TCS34725_CONTROL | TCS34725_COMMAND_BIT, (Byte)_tcs34725Gain };
            _colorSensor.Write(write_buffer);
        }

        public async Task Enable()
        {
            Debug.WriteLine("TCS34725::enable");
            if (!_init) await Begin();

            var write_buffer = new Byte[] { 0x00, 0x00 };

            write_buffer[0] = TCS34725_ENABLE | TCS34725_COMMAND_BIT;
            write_buffer[1] = TCS34725_ENABLE_PON;
            _colorSensor.Write(write_buffer);

            await Task.Delay(3);

            write_buffer[1] = (TCS34725_ENABLE_PON | TCS34725_ENABLE_AEN);
            _colorSensor.Write(write_buffer);
        }

        async Task Begin()
        {
            Debug.WriteLine("TCS34725::Begin");
            var write_buffer = new Byte[] { TCS34725_ID | TCS34725_COMMAND_BIT };
            var read_buffer = new Byte[] { 0xFF };

            //Read and check the device signature
            _colorSensor.WriteRead(write_buffer, read_buffer);
            Debug.WriteLine("TCS34725 Signature: " + read_buffer[0].ToString());

            if (read_buffer[0] != 0x44)
            {
                Debug.WriteLine("TCS34725::Begin Signature Mismatch.");
                return;
            }

            _init = true;
            SetIntegrationTime(_tcs34725IntegrationTime);
            SetGain(_tcs34725Gain);

            await Enable();
        }

        public async Task Disable()
        {
            Debug.WriteLine("TCS34725::disable");
            if (!_init) await Begin();

            var write_buffer = new Byte[] { TCS34725_ENABLE | TCS34725_COMMAND_BIT };
            var read_buffer = new Byte[] { 0xFF };

            //Read the enable buffer
            _colorSensor.WriteRead(write_buffer, read_buffer);

            Byte on_state = (TCS34725_ENABLE_PON | TCS34725_ENABLE_AEN);
            var off_state = (Byte)~on_state;
            off_state &= read_buffer[0];
            var off_buffer = new Byte[] { TCS34725_ENABLE, off_state };

            _colorSensor.Write(off_buffer);
        }

        static UInt16 ColorFromBuffer(IReadOnlyList<Byte> buffer)
        {
            UInt16 color = 0x00;

            color = buffer[1];
            color <<= 8;
            color |= buffer[0];

            return color;
        }

        const Byte TCS34725_CDATAL = 0x14;  //Clear channel data 
        const Byte TCS34725_CDATAH = 0x15;
        const Byte TCS34725_RDATAL = 0x16;  //Red channel data
        const Byte TCS34725_RDATAH = 0x17;
        const Byte TCS34725_GDATAL = 0x18;  //Green channel data
        const Byte TCS34725_GDATAH = 0x19;
        const Byte TCS34725_BDATAL = 0x1A;  //Blue channel data */
        const Byte TCS34725_BDATAH = 0x1B;
        //Method to read the raw color data
        public async Task<ColorData> GetRawData()
        {
            //Create an object to store the raw color data
            var color_data = new ColorData();

            //Make sure the I2C device is initialized
            if (!_init) await Begin();

            var write_buffer = new Byte[] { 0x00 };
            var read_buffer = new Byte[] { 0x00, 0x00 };

            write_buffer[0] = TCS34725_CDATAL | TCS34725_COMMAND_BIT;
            _colorSensor.WriteRead(write_buffer, read_buffer);
            color_data.Clear = ColorFromBuffer(read_buffer);

            write_buffer[0] = TCS34725_RDATAL | TCS34725_COMMAND_BIT;
            _colorSensor.WriteRead(write_buffer, read_buffer);
            color_data.Red = ColorFromBuffer(read_buffer);

            write_buffer[0] = TCS34725_GDATAL | TCS34725_COMMAND_BIT;
            _colorSensor.WriteRead(write_buffer, read_buffer);
            color_data.Green = ColorFromBuffer(read_buffer);

            write_buffer[0] = TCS34725_BDATAL | TCS34725_COMMAND_BIT;
            _colorSensor.WriteRead(write_buffer, read_buffer);
            color_data.Blue = ColorFromBuffer(read_buffer);

            Debug.WriteLine("Raw Data - red: {0}, green: {1}, blue: {2}, clear: {3}",
                            color_data.Red, color_data.Green, color_data.Blue, color_data.Clear);

            return color_data;
        }

        public async Task<RgbData> GetRgbData_Reflected()
        {
            var rgb_data = new RgbData();
            var color_data = await GetRawData();

            if (color_data.Clear > 0)
            {
                rgb_data.Red = (color_data.Red * 255 / color_data.Clear);
                rgb_data.Blue = (color_data.Blue * 255 / color_data.Clear);
                rgb_data.Green = (color_data.Green * 255 / color_data.Clear);
            }
            Debug.WriteLine("RGB Data - red: {0}, green: {1}, blue: {2}", rgb_data.Red, rgb_data.Green, rgb_data.Blue);

            return rgb_data;
        }

        public async Task<RgbData> GetRgbData_Xmitted()
        {
            var rgb_data = new RgbData();
            var color_data = await GetRawData();

            rgb_data.Red = (color_data.Red * 255 / UInt16.MaxValue);
            rgb_data.Blue = (color_data.Blue * 255 / UInt16.MaxValue);
            rgb_data.Green = (color_data.Green * 255 / UInt16.MaxValue);

            Debug.WriteLine("RGB Data - red: {0}, green: {1}, blue: {2}", rgb_data.Red, rgb_data.Green, rgb_data.Blue);
            return rgb_data;
        }

        public async Task<String> GetClosestColor()
        {
            var rgb_data = await GetRgbData_Reflected();
            var closest_color = _colorList[7];
            var min_diff = Double.MaxValue;

            foreach (var color in _colorList)
            {
                var color_value = color.ColorValue;
                var diff = Math.Pow((color_value.R - rgb_data.Red), 2) +
                              Math.Pow((color_value.G - rgb_data.Green), 2) +
                              Math.Pow((color_value.B - rgb_data.Blue), 2);
                diff = (Int32)Math.Sqrt(diff);
                if (diff < min_diff)
                {
                    min_diff = diff;
                    closest_color = color;
                }
            }
            
            Debug.WriteLine("Approximate color: " + closest_color.ColorName + " - "
                                                  + closest_color.ColorValue.ToString());

            return closest_color.ColorName;
        }

    }
}

