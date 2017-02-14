
using System;
using Windows.Devices.Gpio;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;
using Microsoft.IoT.DeviceCore;
using Microsoft.IoT.DeviceCore.Input;
using Microsoft.IoT.Devices.Input;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Encoder
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        const Int32 DT = 17;
        const Int32 CLK = 18;
        const Int32 SW = 27;

        GpioPin _pinA;
        GpioPin _pinB;
        GpioPin _pinC;
        Int32 _counter = 0;

        void PinAOnValueChanged(GpioPin sender, GpioPinValueChangedEventArgs args)
        {
            var a = args.Edge;
            var b = _pinB.Read();
            if (a == GpioPinEdge.FallingEdge)
            {
                if (b == GpioPinValue.High)
                    _counter++;
                else if (b == GpioPinValue.Low)
                    _counter--;
            }

            Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => EncoderValue.Text = _counter.ToString());
        }

        void EncoderOnRotated(IRotaryEncoder sender, RotaryEncoderRotatedEventArgs args)
        {
            if (args.Direction == RotationDirection.Clockwise)
                _counter++;
            else
                _counter--;

            Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => EncoderValue.Text = _counter.ToString());
        }

        RotaryEncoder _encoder;

        void InitGPIO()
        {
            var gpio = GpioController.GetDefault();

            // Show an error if there is no GPIO controller
            if (gpio == null)
            {
                GpioStatus.Text = "There is no GPIO controller on this device.";
                return;
            }

            _pinA = gpio.OpenPin(DT);
            _pinB = gpio.OpenPin(CLK);
            _pinC = gpio.OpenPin(SW);

            _pinA.SetDriveMode(GpioPinDriveMode.Input);
            _pinB.SetDriveMode(GpioPinDriveMode.Input);
            _pinC.SetDriveMode(GpioPinDriveMode.Input);

            _encoder = new RotaryEncoder
            {
                ClockPin = _pinB,
                DirectionPin = _pinA,
                ButtonPin = _pinC
            };

            _encoder.Rotated += EncoderOnRotated;

            GpioStatus.Text = "GPIO pin initialized correctly.";

        }


        public MainPage()
        {
            InitializeComponent();
            InitGPIO();

            EncoderValue.Text = 0.ToString();
        }
    }
}
