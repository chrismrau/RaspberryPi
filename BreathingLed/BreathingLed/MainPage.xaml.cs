
using Microsoft.IoT.Lightning.Providers;
using System;
using System.Threading.Tasks;
using Windows.Devices;
using Windows.Devices.Pwm;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace BreathingLed
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        const Int32 LED_PIN = 5;
        const Double INCREMENT = 0x40;
        static readonly TimeSpan DELAY = TimeSpan.FromMilliseconds(1);

        PwmPin _pwmPin;

        async void OnLoaded(Object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            if (Microsoft.IoT.Lightning.Providers.LightningProvider.IsLightningEnabled)
                LowLevelDevicesController.DefaultProvider = LightningProvider.GetAggregateProvider();
            else
                return;

            var controllers = await PwmController.GetControllersAsync(LightningPwmProvider.GetPwmProvider());
            if (controllers == null) return;
            var controller = controllers[1];
            controller.SetDesiredFrequency(50.0);

            _pwmPin = controller.OpenPin(LED_PIN);
            _pwmPin.SetActiveDutyCyclePercentage(1.0);
            _pwmPin.Start();

            while (true)
            {
                for (var i = INCREMENT; i > 0; i--)
                {
                    var brightness = i / INCREMENT;
                    _pwmPin.SetActiveDutyCyclePercentage(brightness);
                    Task.Delay(DELAY).Wait();
                }
                for (var i = 0; i < INCREMENT; i++)
                {
                    var brightness = i / INCREMENT;
                    _pwmPin.SetActiveDutyCyclePercentage(brightness);
                    Task.Delay(DELAY).Wait();
                }
            }

        }



        public MainPage()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

    }
}
