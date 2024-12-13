using System;
using System.Device.Gpio;
using System.Threading.Tasks;
using SharpDX.DirectInput;

namespace GPIO
{


    public class CameraFeed
    {
        public bool feedOnline = true;
        public int knob1 = 0;
        //public float crosshairOpacity = knob1/1023;
    }
    public class FltControl
    {

    }
    public class CamControl
    {

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
        public Dictionary <string, int> GetJoystickInputs()
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