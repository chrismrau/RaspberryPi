using System;
using Windows.Devices.Gpio;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace ADXL345
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        const Int32 PIN_A = 17;
        const Int32 PIN_B = 18;
        const Int32 PIN_C = 27;

        GpioPin _pinA;
        GpioPin _pinB;
        GpioPin _pinC;

        void InitGPIO()
        {
            var gpio = GpioController.GetDefault();

            // Show an error if there is no GPIO controller
            if (gpio == null)
            {
                GpioStatus.Text = "There is no GPIO controller on this device.";
                return;
            }

            _pinA = gpio.OpenPin(PIN_A);
            _pinB = gpio.OpenPin(PIN_B);
            _pinC = gpio.OpenPin(PIN_C);

            _pinA.SetDriveMode(GpioPinDriveMode.Input);
            _pinB.SetDriveMode(GpioPinDriveMode.Input);
            _pinC.SetDriveMode(GpioPinDriveMode.Input);

            GpioStatus.Text = "GPIO pin initialized correctly.";

        }

        public MainPage()
        {
            InitializeComponent();
        }

    }
}
