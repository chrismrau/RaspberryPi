using System;
using Windows.Devices.Gpio;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace RgbSensor
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        Tcs34725 _sensor;

        const Int32 LED_PIN = 12;

        void InitGPIO()
        {
            var gpio = GpioController.GetDefault();

            // Show an error if there is no GPIO controller
            if (gpio == null)
            {
                GpioStatus.Text = "There is no GPIO controller on this device.";
                return;
            }

            GpioStatus.Text = "GPIO pin initialized correctly.";

        }

        async void InitSensor()
        {
            _sensor = new Tcs34725(LED_PIN);
            await _sensor.Initialize();
        }

        public RgbSensorViewModel ViewModel { get; }

        async void OnTimerClick(Object sender)
        {
            ViewModel.Color = await _sensor.GetRgbData_Xmitted();
        }

        readonly DispatcherTimer _loopTimer;

        void LoopTimerOnTick(Object sender, Object o)
        {
            OnTimerClick(sender);
        }

        void OnLoaded(Object sender, RoutedEventArgs routedEventArgs)
        {
            _loopTimer.Start();
        }

        public MainPage()
        {
            InitializeComponent();

            InitSensor();

            ViewModel = new RgbSensorViewModel(_sensor);
            _loopTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(1000),
            };
            _loopTimer.Tick += LoopTimerOnTick;

            this.Loaded += OnLoaded;
                // new Timer(OnTimerClick, null, 2000, 200);
        }

        async void ReadButton_OnClick(Object sender, RoutedEventArgs e)
        {
            ViewModel.Color = await _sensor.GetRgbData_Reflected();
        }

    }
}
