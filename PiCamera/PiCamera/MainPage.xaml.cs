
using System;
using System.Collections.Immutable;
using System.IO;
using System.Threading.Tasks;
using Windows.Devices.Gpio;
using Windows.Devices.Pwm;
using Microsoft.IoT.Devices.Lights;
using Windows.Graphics.Imaging;
using Windows.Media.Capture;
using Windows.Media.Devices;
using Windows.Media.MediaProperties;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Microsoft.IoT.DeviceCore.Pwm;
using Microsoft.IoT.Devices.Pwm;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace PiCamera
{
    //[ComImport]
    //[Guid("5B0D3235-4DBA-4D44-865E-8F1D0E4FD04D")]
    //[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    //unsafe interface IMemoryBufferByteAccess
    //{
    //    void GetBuffer(out Byte* buffer, out UInt32 capacity);
    //}

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        const Int32 WIDTH = 64;
        const Int32 HEIGHT = 48;
        Byte[] _imageBuffer = new Byte[WIDTH * HEIGHT * 4];
        const Int32 CENTER = (Int32)(4 * (WIDTH * (HEIGHT + 1))) / 2;
        const Int32 TOP_LEFT = CENTER - (WIDTH * 2 * 4) - (2 * 4);
        const Int32 BOT_RITE = CENTER + (WIDTH * 2 * 4) + (2 * 4);

        RgbLed _led;

        void DetermineRgb()
        {
            var alpha = 0;
            var red = 0;
            var green = 0;
            var blue = 0;

            for (var j = 0; j < 5; ++j)
            {
                var row = TOP_LEFT + (j * WIDTH * 4);
                for (var i = 0; i < 5; ++i)
                {
                    var p = new Byte[4];
                    var loc = row + (i * 4);
                    System.Buffer.BlockCopy(_imageBuffer, loc, p, 0, p.Length);

                    alpha += p[3];
                    red += p[2];
                    green += p[1];
                    blue += p[0];
                }
            }

            red = (red / (5 * 5));
            green = (green / (5 * 5));
            blue = (blue / (5 * 5));
            _led.Color = Color.FromArgb(0xFF, (Byte)red, (Byte)green, (Byte)blue);

            AlphaText.Text = (alpha / (5 * 5)).ToString();
            RedText.Text = red.ToString();
            GreenText.Text = green.ToString();
            BlueText.Text = blue.ToString();
        }

        public MediaDeviceControl WhiteBalance
        {
            get { return _whiteBalance; }
            set { _whiteBalance = value; }
        }

        IRandomAccessStream _photoStream;

        void UpdateDisplay(Object sender, Object e)
        {
            DetermineRgb();

            WhiteBalance.TrySetValue(WhiteBalSlider.Value);

            var photo = new BitmapImage();
            _photoStream.Seek(0);
            photo.SetSource(_photoStream);
            CaptureImage.Source = photo;
        }

        DispatcherTimer _dispatcherTimer = new DispatcherTimer();
        void InitTimer()
        {
            _dispatcherTimer.Interval = TimeSpan.FromMilliseconds(200);
            _dispatcherTimer.Tick += UpdateDisplay;
            _dispatcherTimer.Start();
        }

        MediaCapture Camera { get; set; }

        async Task TakePicture()
        {
            try
            {
                using (var photo_stream = new InMemoryRandomAccessStream())
                {
                    Camera.VideoDeviceController.Focus.TrySetAuto(true);
                    await Camera.CapturePhotoToStreamAsync(_imageProperties, photo_stream);

                    photo_stream.Seek(0);
                    var decoder = await BitmapDecoder.CreateAsync(photo_stream);
                    var provider = await decoder.GetPixelDataAsync(
                        BitmapPixelFormat.Bgra8,
                        BitmapAlphaMode.Ignore,
                        new BitmapTransform(),
                        ExifOrientationMode.IgnoreExifOrientation,
                        ColorManagementMode.ColorManageToSRgb);
                    _imageBuffer = provider.DetachPixelData();

                    _photoStream = photo_stream.CloneStream();
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = ex.Message;
                Cleanup();
            }
        }

        async void InitMediaCapture()
        {
            _imageProperties = ImageEncodingProperties.CreateBmp();
            _imageProperties.Width = WIDTH;
            _imageProperties.Height = HEIGHT;

            Camera = new MediaCapture();
            var settings = new MediaCaptureInitializationSettings
            {
                MediaCategory = MediaCategory.Media
            };
            await Camera.InitializeAsync(settings);

            Camera.VideoDeviceController.DesiredOptimization = MediaCaptureOptimization.Quality;
            Camera.VideoDeviceController.PrimaryUse = CaptureUse.Photo;


            WhiteBalance = Camera.VideoDeviceController.WhiteBalance;
            WhiteBalSlider.Minimum = WhiteBalance.Capabilities.Min;
            WhiteBalSlider.Maximum = WhiteBalance.Capabilities.Max;
            Double value;
            WhiteBalance.TryGetValue(out value);
            WhiteBalSlider.Value = value;

            PreviewElement.Source = Camera;
            await Camera.StartPreviewAsync();

            await TakePicture();

            Task.Run(async () =>
            {
                while (true)
                {
                    await TakePicture();
                    Task.Delay(100).Wait();
                }
            });

            InitTimer();
        }

        [Flags]
        enum Location
        {
            Center = 0x01,
            Left = 0x02,
            Right = 0x04,
            AllMiddle = Center | Left | Right,

            Quad01 = 0x10,
            Quad02 = 0x20,
            Quad03 = 0x40,
            Quad04 = 0x80,
            QuadAll = Quad01 | Quad02 | Quad03 | Quad04,

            TopLeft = 0x100,
            TopCenter = 0x200,
            TopRight = 0x400,
            BottomLeft = 0x800,
            BottomCenter = 0x1000,
            BottomRight = 0x2000,
            AllTripple = TopLeft | TopCenter | TopRight | BottomLeft | BottomCenter | BottomRight,

        }

        ImmutableDictionary<Location, Byte[]> _pixels = ImmutableDictionary<Location, Byte[]>.Empty;
        ImageEncodingProperties _imageProperties;

        async void CaptureButton_OnClick(Object sender, RoutedEventArgs e)
        {
            await TakePicture();
        }

        const Int32 PIN_R = 17;
        const Int32 PIN_G = 18;
        const Int32 PIN_B = 27;

        PwmController _controller;
        MediaDeviceControl _whiteBalance;

        async void InitPwm()
        {
            var gpio_controller = GpioController.GetDefault();
            var manager = new PwmProviderManager();
            manager.Providers.Add(new SoftPwm());
            var controllers = await manager.GetControllersAsync();
            _controller = controllers[0];
            _controller.SetDesiredFrequency(120);

            _led = new RgbLed
            {
                RedPin = _controller.OpenPin(PIN_R),
                GreenPin = _controller.OpenPin(PIN_G),
                BluePin = _controller.OpenPin(PIN_B),
                Color = Colors.Black
            };

        }

        public MainPage()
        {
            InitializeComponent();

            InitPwm();
            InitMediaCapture();
            //InitTimer();
        }

        async void Cleanup()
        {
            var timer = _dispatcherTimer;
            if (timer != null)
            {
                timer.Stop();
                _dispatcherTimer = null;
            }

            if (Camera == null) return;

            // Cleanup MediaCapture object
            await Camera.StopPreviewAsync();
            CaptureImage.Source = null;
            await Camera.StopRecordAsync();
            Camera.Dispose();
            Camera = null;
        }


    }

}
