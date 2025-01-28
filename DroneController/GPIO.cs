using System;
using System.Collections.Generic;
using System.Threading;
using System.Drawing;
using System.Device.Gpio;
using SharpDX.DirectInput;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using System.Windows.Media.Imaging;
using System.IO.Ports;

namespace GPIO
{
    
    public class CameraFeed
    {
        private VideoCapture _videoCapture;
        private Mat _frame;
        public bool feedOnline;
        public int CameraSelect = 0;

        public CameraFeed()
        {
            _videoCapture = new VideoCapture(CameraSelect);
            _frame = new Mat();
            _videoCapture.Set(OpenCvSharp.VideoCaptureProperties.FrameWidth, 1280); // Reduce resolution
            _videoCapture.Set(OpenCvSharp.VideoCaptureProperties.FrameHeight, 720); // Reduce resolution
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
                throw new Exception("Failed to capture frame");
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
        public SerialPort port;
        public int baudRate = 9600;
        public string comPort = "COM12";
        private Thread fltSignalThread;
        private bool isSending = false;
        public FltControl()
        {
            port = new SerialPort(comPort, baudRate);
            port.Open();
        }

        public void startSerial()
        {
            if (isSending) { return; }

            isSending = true;
            fltSignalThread = new Thread(() =>
            {
                while (isSending)
                {
                    try
                    {
                        if (port.IsOpen)
                        {
                            port.WriteLine("hi");
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.ToString());
                        StopSending();
                    }
                    Thread.Sleep(50);
                }
            });
            
            fltSignalThread.IsBackground = true;
            fltSignalThread.Start();
        }

        public void StopSending()
        {
            isSending = false;

            if(fltSignalThread != null && fltSignalThread.IsAlive)
            {
                fltSignalThread.Join();
            }
        }

        public void Dispose()
        {
            StopSending();

            if (port != null && port.IsOpen)
            {
                port.Close();
            }
        }
    }

    public class CamControl
    {
        // Placeholder for camera control logic
    }

    public class JoyInput
    {
        private DirectInput _directInput;
        private Joystick _joystick;
        public string JoyStatusString = "No Joystick Found";

        public JoyInput()
        {
            _directInput = new DirectInput();

            var joystickGuid = Guid.Empty;

            // Search for a connected joystick
            foreach (var deviceInstance in _directInput.GetDevices(DeviceType.Joystick, DeviceEnumerationFlags.AttachedOnly))
            {
                joystickGuid = deviceInstance.InstanceGuid;
                break;
            }

            if (joystickGuid == Guid.Empty)
            {
                _joystick = null;
                return;
            }

            try
            {
                _joystick = new Joystick(_directInput, joystickGuid);
                _joystick.Acquire();
                JoyStatusString = "Joystick Connected";
            }
            catch (Exception ex)
            {
                _joystick = null;
                JoyStatusString = $"Joystick Error: {ex.Message}";
            }
        }

        public Dictionary<string, int> GetJoystickInputs()
        {
            if (_joystick == null)
                return null;

            try
            {
                _joystick.Poll();
                var state = _joystick.GetCurrentState();

                return new Dictionary<string, int>
            {
                { "X", state.X },
                { "Y", state.Y },
                { "Z", state.RotationZ },
                { "camControl", state.PointOfViewControllers[1] },
                { "camPOV", state.PointOfViewControllers[0] },
            };
            }
            catch (Exception)
            {
                return null;
            }
        }

        public bool[] GetJoyButtons()
        {
            if (_joystick == null)
            {
                return new bool[0];
            }

            try
            {
                _joystick.Poll();
                var state = _joystick.GetCurrentState();
                return state.Buttons;
            }
            catch (Exception)
            {
                return new bool[0];
            }
        }
    }

}

