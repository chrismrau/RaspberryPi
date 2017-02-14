
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Devices.Gpio;

namespace SevenSegmentBackground
{
    public sealed class StartupTask : IBackgroundTask
    {
        BackgroundTaskDeferral _deferral;

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
                throw new NullReferenceException("There are no GPIO Controllers on this device");

            _sdi = gpio.OpenPin(PIN_A);
            _rclk = gpio.OpenPin(PIN_B);
            _srclk = gpio.OpenPin(PIN_C);

            _sdi.SetDriveMode(GpioPinDriveMode.Output);
            _rclk.SetDriveMode(GpioPinDriveMode.Output);
            _srclk.SetDriveMode(GpioPinDriveMode.Output);

            _sdi.Write(GpioPinValue.Low);
            _rclk.Write(GpioPinValue.Low);
            _srclk.Write(GpioPinValue.Low);

        }

        static readonly Byte[] SegCode = new Byte[]
            {0x3f, 0x06, 0x5b, 0x4f, 0x66, 0x6d, 0x7d, 0x07, 0x7f, 0x6f, 0x77, 0x7c, 0x39, 0x5e, 0x79, 0x71, 0x80};

        void hc595_shift(Byte dat)
        {
            for (var i = 0; i < 8; i++)
            {
                var value = (0x80 & (dat << i)) > 0 ? GpioPinValue.High : GpioPinValue.Low;
                _sdi.Write(value);
                _srclk.Write(GpioPinValue.Low);
                Task.Delay(1).Wait();
                _srclk.Write(GpioPinValue.High);
            }

            _rclk.Write(GpioPinValue.Low);
            Task.Delay(1).Wait();
            _rclk.Write(GpioPinValue.High);
        }

        void Run()
        {
            while (true)
            {
                for (var i = 0; i < 17; i++)
                {
                    hc595_shift(SegCode[i]);
                    Task.Delay(500).Wait();
                }
            }
        }

        #region Implementation of IBackgroundTask

        public void Run(IBackgroundTaskInstance taskInstance)
        {
            _deferral = taskInstance.GetDeferral();

            InitGPIO();
            Run();

            _deferral.Complete();
        }

        #endregion
    }
}
