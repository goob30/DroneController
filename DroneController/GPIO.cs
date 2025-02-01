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
using System.Diagnostics;

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

    public class FltControl
    {
        public SerialPort port;
        public int baudRate = 9600;
        public string comPort = "COM4";
        private Thread fltSignalThread;
        public bool isSending = false;
        private JoyInput joyInput;  // Create an instance of JoyInput

        public FltControl()
        {
            // Start the serial port and joystick initialization in a background thread
            Task.Run(() =>
            {
                // This will run on a background thread
                Debug.WriteLine("Initializing FltControl");

                try
                {
                    // Initialize the serial port
                    port = new SerialPort(comPort, baudRate);
                    port.Open();
                    Debug.WriteLine($"Serial port opened at {comPort} with {baudRate} baud rate.");

                    // Initialize the joystick input
                    joyInput = new JoyInput();
                    Debug.WriteLine("JoyInput initialized.");

                    // Wait for 2 seconds for any necessary setup
                    Thread.Sleep(2000);  // Wait for the necessary time (this won't block the UI thread)
                    Debug.WriteLine("2 seconds sleep complete.");

                    // You can access joystick data after initialization
                    Debug.WriteLine($"Joystick Status: {joyInput.JoyStatusString}");

                    // Now you can start sending serial data, etc.
                    startSerial();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error initializing FltControl: {ex.Message}");
                }
            });
        }



        public void startSerial()
        {
            if (isSending) { return; }

            isSending = true;

            // Start the thread to send serial data
            fltSignalThread = new Thread(() =>
            {
                while (isSending)
                {
                    try
                    {
                        // Access joystick input in each iteration to get the latest data
                        var inputs = joyInput.GetJoystickInputs();
                        if (inputs == null || !inputs.ContainsKey("X") || !inputs.ContainsKey("Y"))
                        {
                            Debug.WriteLine("No joystick data available.");  // Debug output
                            continue;  // Skip this iteration if no data is available
                        }

                        // Create joysSerial string with joystick X and Y values
                        string joysSerial = $"X {inputs["X"]} Y {inputs["Y"]}";
                        Debug.WriteLine($"Sending serial: {joysSerial}");  // Debug output

                        // Ensure the port is open before writing to it
                        if (!port.IsOpen)
                        {
                            Debug.WriteLine("Port is closed. Attempting to reopen...");
                            try
                            {
                                port.Open();  // Try to reopen the port if it's closed
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"Failed to reopen port: {ex.Message}");
                                continue;  // Skip this iteration if port can't be reopened
                            }
                        }

                        // Send the latest joystick data
                        port.WriteLine(joysSerial);
                        Debug.WriteLine($"Sent: {joysSerial}");  // Debug output
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine($"Error: {e.Message}");
                        StopSending();
                    }

                    // Sleep for a short duration before next loop iteration
                    Thread.Sleep(50);
                }
            });

            fltSignalThread.IsBackground = true;
            fltSignalThread.Start();
        }



        public void StopSending()
        {
            isSending = false;

            if (fltSignalThread != null && fltSignalThread.IsAlive)
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



}

