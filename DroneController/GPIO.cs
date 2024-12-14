using System;
using System.Collections.Generic;
using System.Threading;
using System.Drawing;
using System.Device.Gpio;
using System.Threading.Tasks;
using SharpDX.DirectInput;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Drawing.Imaging;
using System.Windows.Threading;
using System.Windows;

namespace GPIO
{
    public class CameraFeed
    {
        private VideoCapture _videoCapture;
        private DispatcherTimer _timer;
        private WriteableBitmap _writeableBitmap;
        private bool _isRunning;

        public CameraFeed()
        {
            try
            {
                _videoCapture = new VideoCapture(0); // Open default camera
                if (!_videoCapture.IsOpened)
                {
                    MessageBox.Show("Unable to access the camera!");
                    return;
                }

                // Initialize timer to update frames every 30 ms (~33 FPS)
                _timer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(30)
                };
                _timer.Tick += Timer_Tick;

                _isRunning = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing webcam: {ex.Message}");
            }
        }

        public WriteableBitmap GetWriteableBitmap(int width, int height)
        {
            // Initialize WriteableBitmap for display
            _writeableBitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgr24, null);
            return _writeableBitmap;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            // Capture frame from camera
            Mat frame = new Mat();
            _videoCapture.Read(frame);

            if (!frame.IsEmpty)
            {
                // Convert Mat frame to byte array
                byte[] pixels = frame.ToImage<Bgr, byte>(true).Bytes;


                // Update WriteableBitmap with new frame
                _writeableBitmap.WritePixels(new Int32Rect(0, 0, frame.Width, frame.Height),
                                              pixels, frame.Width * 3, 0);
            }
        }

        public void Start()
        {
            // Start the timer to capture frames
            _timer.Start();
        }

        public void Stop()
        {
            // Stop the camera feed and release resources
            _timer.Stop();
            _videoCapture.Release();
        }
    }

    public class FltControl
    {
        // Placeholder for future flight control implementation
    }

    public class CamControl
    {
        // Placeholder for camera control logic
    }

    public class JoyInput
    {
        private DirectInput _directInput;
        private Joystick _joystick;
        public string JoyStatusString = "";

        public JoyInput()
        {
            _directInput = new DirectInput();

            var joystickGuid = Guid.NewGuid();
            foreach (var deviceInstance in _directInput.GetDevices(DeviceType.Joystick, DeviceEnumerationFlags.AttachedOnly))
            {
                joystickGuid = deviceInstance.InstanceGuid;
                break;
            }

            if (joystickGuid == Guid.Empty)
            {
                JoyStatusString = "No Joystick Found";
                return;
            }

            _joystick = new Joystick(_directInput, joystickGuid);
            _joystick.Acquire();
        }

        public Dictionary<string, int> GetJoystickInputs()
        {
            if (_joystick == null)
                return null;

            _joystick.Poll();
            var state = _joystick.GetCurrentState();

            var inputs = new Dictionary<string, int>
            {
                { "X", state.X },
                { "Y", state.Y },
                { "Z", state.RotationX },
                { "camPOV", state.PointOfViewControllers[0] },
            };

            return inputs;
        }
    }
}
