using System;
using System.Threading.Tasks;
using Windows.Devices.Gpio;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace DotMatrix
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

        //readonly Byte[] _codeH =
        //    { 0x01, 0xff, 0x80, 0xff, 0x01, 0x02, 0x04, 0x08, 0x10, 0x20, 0x40, 0x80, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff };
        //readonly Byte[] _codeL =
        //    { 0x00, 0x7f, 0x00, 0xfe, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xfe, 0xfd, 0xfb, 0xf7, 0xef, 0xdf, 0xbf, 0x7f };

        readonly Byte[] _codeL = {0x00,0x00,0x3c,0x42,0x42,0x3c,0x00,0x00};
        readonly Byte[] _codeH = {0xff,0xe7,0xdb,0xdb,0xdb,0xdb,0xe7,0xff};

        //unsigned char code_L[8] = {0xff,0xff,0xc3,0xbd,0xbd,0xc3,0xff,0xff};
        //unsigned char code_H[8] = {0x00,0x18,0x24,0x24,0x24,0x24,0x18,0x00};

        void hc595_in(Byte dat)
        {
            for (var i = 0; i < 8; i++)
            {
                var value = (0x80 & (dat << i)) > 0 ? GpioPinValue.High : GpioPinValue.Low;
                _sdi.Write(value);
                _srclk.Write(GpioPinValue.High);
                Task.Delay(1).Wait();
                _srclk.Write(GpioPinValue.Low);
            }
        }

        void hc595_out()
        {
            _rclk.Write(GpioPinValue.High);
            Task.Delay(1).Wait();
            _rclk.Write(GpioPinValue.Low);
        }

        public MainPage()
        {
            InitializeComponent();
            InitGPIO();

            hc595_in(0);
            hc595_in(0);
            hc595_out();

            while (true)
            {
                for (var i = 0; i < _codeH.Length; i++)
                {
                    hc595_in(_codeL[i]);
                    hc595_in(_codeH[i]);
                    hc595_out();
                    Task.Delay(100).Wait();
                }

                for (var i = _codeH.Length; i > 0; i--)
                {
                    hc595_in(_codeL[i-1]);
                    hc595_in(_codeH[i-1]);
                    hc595_out();
                    Task.Delay(100).Wait();
                }
            }
        }

    }
}
