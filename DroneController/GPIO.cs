using System;
using System.Collections.Generic;
using System.Threading;
using System.Drawing;
using System.Device.Gpio;
using SharpDX.DirectInput;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using System.Windows.Media.Imaging;

namespace GPIO
{
    public class CameraFeed
    {
        private VideoCapture _videoCapture;
        private Mat _frame;

        public CameraFeed()
        {
            _videoCapture = new VideoCapture(1);
            _frame = new Mat();
            _videoCapture.Set(OpenCvSharp.VideoCaptureProperties.FrameWidth, 1920);
            _videoCapture.Set(OpenCvSharp.VideoCaptureProperties.FrameWidth, 1080);
            if (!_videoCapture.IsOpened())
            {
                throw new Exception("Unable to access webcam");
            }
        }

        public BitmapSource GetFrame()
        {
            _videoCapture.Read(_frame);
            if (_frame.Empty())
            {
                throw new Exception("Failed to capture frame");
            }
            return _frame.ToBitmapSource();
        }

        public void Dispose()
        {
            _videoCapture.Release();
            _frame.Dispose();
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

            //Input definitions
            var inputs = new Dictionary<string, int>
            {
                { "X", state.X },
                { "Y", state.Y },
                { "Z", state.RotationX },
                { "camPOV", state.PointOfViewControllers[0] },
                { "camControl", state.PointOfViewControllers[1] },
            };

            return inputs;
        }
    }
}
