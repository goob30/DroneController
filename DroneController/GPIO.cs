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
                var buttons = state.Buttons;

                return new Dictionary<string, int>{
                    { "X", state.X },
                    { "Y", state.Y },
                    { "Z", state.RotationZ },
                    { "camControl", state.PointOfViewControllers[1] },
                    { "camPOV", state.PointOfViewControllers[0] },
                    { "Trigger", buttons.Length > 0 && buttons[0] ? 1 : 0 } // Trigger is Button 0
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
            if (isSending) return;

            isSending = true;

            Debug.WriteLine("Attempting to start fltSignal thread...");

            fltSignalThread = new Thread(() =>
            {
                try
                {
                    if (!port.IsOpen)
                    {
                        port.Open();
                    }

                    while (isSending)
                    {
                        var inputs = joyInput.GetJoystickInputs();
                        if (inputs == null || !inputs.ContainsKey("X") || !inputs.ContainsKey("Y") || !inputs.ContainsKey("Trigger"))
                        {
                            Debug.WriteLine("No joystick data available.");
                            continue;
                        }

                        // Get trigger state
                        int triggerValue = inputs["Trigger"];

                        // Format serial string
                        string joysSerial = $"X {inputs["X"]} Y {inputs["Y"]} T {triggerValue}";
                        Debug.WriteLine($"Sending serial: {joysSerial}");
                        port.WriteLine(joysSerial);

                        if (!WaitForResponse("ACK_JOY", 100))
                        {
                            Debug.WriteLine("No ACK received, retrying...");
                            continue;
                        }

                        port.WriteLine("REQ_DST");

                        string distanceResponse = ReadLineWithTimeout(100);
                        if (distanceResponse.StartsWith("DST "))
                        {
                            string distanceStr = distanceResponse.Substring(4);
                            if (float.TryParse(distanceStr, out float distance))
                            {
                                Debug.WriteLine($"Distance {distance}");
                            }
                        }

                        Thread.Sleep(50);
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine($"Error: {e.Message}");
                }
                finally
                {
                    if (port.IsOpen)
                    {
                        port.Close();
                    }
                    isSending = false;
                }
            });

            // ✅ **Thread starts here, outside the declaration!**
            fltSignalThread.IsBackground = true;
            fltSignalThread.Start();
        }


        private bool WaitForResponse(string expectedResponse, int timeoutMs)
        {
            DateTime start = DateTime.Now;
            while ((DateTime.Now - start).TotalMilliseconds < timeoutMs)
            {
                if (port.BytesToRead > 0)
                {
                    string response = port.ReadLine().Trim();
                    if (response == expectedResponse) return true;
                }
                Thread.Sleep(5);
            }
            return false;
        }

        private string ReadLineWithTimeout(int timeoutMs)
        {
            DateTime start = DateTime.Now;
            while((DateTime.Now - start).TotalMilliseconds < timeoutMs)
            {
                if(port.BytesToRead > 0)
                {
                    return port.ReadLine().Trim();
                }
                Thread.Sleep(5);
            }
            return "";
        }


        
    }



}

