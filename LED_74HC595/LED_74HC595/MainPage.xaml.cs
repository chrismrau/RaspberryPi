using System;
using System.Threading.Tasks;
using Windows.Devices.Gpio;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace LED_74HC595
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        const Int32 PIN_A = 17;
        const Int32 PIN_B = 18;
        const Int32 PIN_C = 27;

        GpioPin _sdi;
        GpioPin _rclk;
        GpioPin _srclk;

        void InitGPIO()
        {
            var gpio = GpioController.GetDefault();

            // Show an error if there is no GPIO controller
            if (gpio == null)
            {
                GpioStatus.Text = "There is no GPIO controller on this device.";
                return;
            }

            _sdi = gpio.OpenPin(PIN_A);
            _rclk = gpio.OpenPin(PIN_B);
            _srclk = gpio.OpenPin(PIN_C);

            _sdi.SetDriveMode(GpioPinDriveMode.Output);
            _rclk.SetDriveMode(GpioPinDriveMode.Output);
            _srclk.SetDriveMode(GpioPinDriveMode.Output);

            _sdi.Write(GpioPinValue.Low);
            _rclk.Write(GpioPinValue.Low);
            _srclk.Write(GpioPinValue.Low);

            GpioStatus.Text = "GPIO pin initialized correctly.";

        }

        void Pulse(GpioPin pin)
        {
            pin.Write(GpioPinValue.Low);
            pin.Write(GpioPinValue.High);
        }

        void SIPO(Byte buffer)
        {
            for (var i = 0; i < 8; ++i)
            {
                var value = (buffer & (0x80 >> i)) > 0 ? GpioPinValue.High : GpioPinValue.Low;
                _sdi.Write(value);
                Pulse(_srclk);
            }
        }

        public MainPage()
        {
            InitializeComponent();
            InitGPIO();

            Task.Run(() =>
            {
                while (true)
                {
                    for (var i = 0; i < 8; ++i)
                    {
                        var value = Convert.ToByte(1 << i);
                        SIPO(value);
                        Pulse(_rclk);
                        Task.Delay(150).Wait();
                    }
                    Task.Delay(500).Wait();

                    for (var i = 0; i < 3; ++i)
                    {
                        SIPO(0xff);
                        Pulse(_rclk);
                        Task.Delay(100).Wait();
                        SIPO(0x00);
                        Pulse(_rclk);
                        Task.Delay(100).Wait();
                    }
                    Task.Delay(500).Wait();

                    for (var i = 0; i < 8; ++i)
                    {
                        var value = Convert.ToByte(1 << (7 - i));
                        SIPO(value);
                        Pulse(_rclk);
                        Task.Delay(150).Wait();
                    }
                    Task.Delay(500).Wait();

                    for (var i = 0; i < 3; ++i)
                    {
                        SIPO(0xff);
                        Pulse(_rclk);
                        Task.Delay(100).Wait();
                        SIPO(0x00);
                        Pulse(_rclk);
                        Task.Delay(100).Wait();
                    }
                    Task.Delay(500).Wait();

                }
            });
        }

    }
}
