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
        public bool feedOnline;
        public int CameraSelect = 1;

        public CameraFeed()
        {
            _videoCapture = new VideoCapture(CameraSelect); //Change value as required for camera
            _frame = new Mat();
            _videoCapture.Set(OpenCvSharp.VideoCaptureProperties.FrameWidth, 1920); // Reduce resolution
            _videoCapture.Set(OpenCvSharp.VideoCaptureProperties.FrameHeight, 1080); // Reduce resolution
            _videoCapture.Set(OpenCvSharp.VideoCaptureProperties.Fps, 30);
            feedOnline = true;
            if (!_videoCapture.IsOpened())
            {
                throw new Exception("Unable to access webcam");
                feedOnline = false;

            }
        }

        public BitmapSource GetFrame()
        {
            _videoCapture.Read(_frame);
            if (_frame.Empty())
            {
                feedOnline = false;
            }
            else
            {
                feedOnline = true;
            }
            return _frame.ToBitmapSource();
        }

        public void Dispose()
        {
            _videoCapture.Release();
            _frame.Dispose();
        }

        public void ChangeCamera(int newCameraSelect)
        {
            CameraSelect = newCameraSelect;

            // Dispose of the existing video capture object
            _videoCapture.Release();
            _videoCapture.Dispose();

            // Initialize the video capture with the new camera source
            _videoCapture = new VideoCapture(CameraSelect);
            if (!_videoCapture.IsOpened())
            {
                feedOnline = false;
                throw new Exception($"Unable to access webcam {CameraSelect}");
            }

            feedOnline = true;
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
                { "Z", state.RotationZ },
                { "camPOV", state.PointOfViewControllers[0] },
                { "camControl", state.PointOfViewControllers[1] },
            };

            return inputs;
        }
    }
}
