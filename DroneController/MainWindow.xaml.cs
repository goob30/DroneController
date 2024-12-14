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
        
        public MainWindow()
        {
            InitializeComponent();
            _cameraFeed = new CameraFeed();

            _joyInput = new JoyInput();
            Task.Run(() => PollJoystick());

            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(30) };
            _timer.Tick += UpdateFrame;
            _timer.Start();
        }

        private async void UpdateFrame(object sender, EventArgs e)
        {
            cameraFeed.Source = _cameraFeed.GetFrame();
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

            while (true)
            {
                try
                {
                    var inputs = _joyInput.GetJoystickInputs();
                    if (inputs != null)
                    {

                        await Dispatcher.InvokeAsync(() =>
                        {
                            
                            controlXLabel.Content = $"X: {inputs["X"]}";
                            controlYLabel.Content = $"Y: {inputs["Y"]}";
                            controlZLabel.Content = $"Z: {inputs["Z"]}";

                            if(inputs.ContainsKey("camPOV") && inputs["camPOV"] != -1)
                            {
                                var povAngle = inputs["camPOV"];
                                DateTime now = DateTime.Now;

                                //POV change code
                                if((now - lastZoomChangeTime).TotalMilliseconds >= zoomChangeDelay)
                                {
                                    if (povAngle == 0)
                                    {
                                        camZoom = Math.Min(camZoom + 0.5, 3);
                                        zoomLabel.Content = $"ZOOM {camzZoom}X";
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
                }
                catch (Exception ex)
                {

                    statusLabel.Content = ($"Error polling joystick: {ex.Message}");

                }

                await Task.Delay(50);
            }
        }

        private void btnClick(object sender, RoutedEventArgs e)
        {
            statusLabel.Content = camZoom;
            
        }
    }
}