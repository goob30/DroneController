using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Documents.DocumentStructures;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System;
using GPIO;
using System.ComponentModel;
using Emgu.CV;
using SharpDX.DirectInput;


namespace DroneController
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private CameraFeed _cameraFeed;
        private JoyInput _joyInput;
        public double camZoom = 1;
        private DispatcherTimer _timer;
        private ScaleTransform _scaleTransform;
        
        public MainWindow()
        {
            InitializeComponent();
            _cameraFeed = new CameraFeed();

            _scaleTransform = cameraScale;
            _joyInput = new JoyInput();
            Task.Run(() => PollJoystick());

            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(30) };
            _timer.Tick += UpdateFrame;
            _timer.Start();
        }

        private async void UpdateFrame(object sender, EventArgs e)
        {
            cameraFeed.Source = _cameraFeed.GetFrame();
            var frame = await Task.Run(() => _cameraFeed.GetFrame());

            if (_scaleTransform != null)
            {
                _scaleTransform.ScaleX = camZoom;
                _scaleTransform.ScaleY = camZoom;
            }

            if (_cameraFeed.feedOnline == false)
            {
                NoSigGrid.Visibility = Visibility.Visible;
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            _timer.Stop();
            _cameraFeed.Dispose();
            base.OnClosing(e);
        }

        private async void PollJoystick()
        {
            DateTime lastZoomChangeTime = DateTime.MinValue;
            int zoomChangeDelay = 160;

            // Check if joystick is detected
            if (_joyInput == null || _joyInput.GetJoystickInputs() == null)
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    statusLabel.Content = "No joystick detected. Polling skipped.";
                });
                return; // Exit method
            }

            while (true)
            {
                try
                {
                    var inputs = _joyInput.GetJoystickInputs();
                    var buttons = _joyInput.GetJoyButtons();

                    if (inputs != null && buttons.Length > 0)
                    {
                        await Dispatcher.InvokeAsync(() =>
                        {
                            controlXLabel.Content = $"X: {inputs["X"]}";
                            controlYLabel.Content = $"Y: {inputs["Y"]}";
                            controlZLabel.Content = $"Z: {inputs["Z"]}";
                            camHatLabel.Content = $"CAM HAT {inputs["camPOV"]}";
                            controlHatLabel.Content = $"CONT HAT {inputs["camControl"]}";

                            if (buttons.Length > 13 && buttons[13]) InitializeCamera(0) ; // H2 left

                            if (buttons.Length > 11 && buttons[11]) InitializeCamera(1); // H2 right

                            if (inputs.ContainsKey("camPOV"))
                            {
                                var povAngle = inputs["camPOV"];

                                if (povAngle == -1) return;

                                DateTime now = DateTime.Now;

                                if ((now - lastZoomChangeTime).TotalMilliseconds >= zoomChangeDelay)
                                {
                                    if (povAngle == 0)
                                    {
                                        camZoom = Math.Min(camZoom + 0.5, 3);
                                        zoomLabel.Content = $"ZOOM {camZoom}X";
                                        lastZoomChangeTime = now;
                                    }
                                    else if (povAngle == 18000)
                                    {
                                        camZoom = Math.Max(camZoom - 0.5, 1);
                                        zoomLabel.Content = $"ZOOM {camZoom}X";
                                        lastZoomChangeTime = now;
                                    }
                                }
                            }
                        });
                    }
                    else
                    {
                        statusLabel.Content = _joyInput.JoyStatusString;
                    }
                }
                catch (Exception ex)
                {
                    await Dispatcher.InvokeAsync(() =>
                    {
                        statusLabel.Content = $"Error polling joystick: {ex.Message}";
                    });
                }

                await Task.Delay(50);
            }
        }

        private void InitializeCamera(int cameraIndex)
        {
            try
            {
                _cameraFeed.ChangeCamera(cameraIndex);
            }
            catch (Exception)
            {
                
            }
        }

        private void buttonClicky(object sender, RoutedEventArgs e)
        {
            
        }

        private void menuButtonClick(object sender, RoutedEventArgs e)
        {
            MenuGrid.Visibility = Visibility.Visible;
        }

        private void menuButton2Click(object sender, RoutedEventArgs e)
        {
            MenuGrid.Visibility = Visibility.Hidden;
        }
    }
}