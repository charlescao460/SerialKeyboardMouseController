using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SerialKeyboardMouse.Serial;


namespace SerialKeyboardMouse
{
    public enum HidFrameType
    {
        Invalid, // Prohibited
        MouseReleaseAll,
        KeyboardReleaseAll,
        MouseMove = 0xAA,
        MouseScroll = 0xAB,
        MousePress = 0xAC,
        MouseRelease = 0xAD,
        MouseResolution = 0xAE,
        KeyboardPress = 0xBB,
        KeyboardRelease = 0xBC,
        Unknown = 0xFF
    }

    [Flags]
    public enum MouseButton
    {
        Left = 0x01,
        Right = 0x02,
        Middle = 0x04
    }

    /// <summary>
    /// An informative class represents the HID state we sent to the hardware.
    /// </summary>
    public class HidFrameInfo
    {
        public HidFrameType Type { get; }

        /// <summary>
        /// Buttons on the mouse (left, right, middle)
        /// </summary>
        public MouseButton? MouseButton { get; }

#nullable enable
        /// <summary>
        /// Coordinates of the absolute mouse
        /// </summary>
        public Tuple<int, int>? MouseCoordinate { get; }
#nullable restore

        /// <summary>
        /// Number of lines of mouse scrolling
        /// </summary>
        public int? MouseScrollValue { get; }

        /// <summary>
        /// HID usage code of the keyboard. Represents the key on the keyboard.
        /// </summary>
        public HidKeyboardUsage? KeyboardKey { get; }

        internal HidFrameInfo(HidFrameType type, MouseButton? mouseButton, Tuple<int, int> mouseCoordinate,
            int? mouseScrollValue, HidKeyboardUsage? keyboardHidUsage)
        {
            Type = type;
            MouseButton = mouseButton;
            MouseCoordinate = mouseCoordinate;
            MouseScrollValue = mouseScrollValue;
            KeyboardKey = keyboardHidUsage;
        }

        public override string ToString()
        {
            switch (Type)
            {
                case HidFrameType.Invalid:
                case HidFrameType.Unknown:
                case HidFrameType.MouseReleaseAll:
                case HidFrameType.KeyboardReleaseAll:
                    return Type.ToString();
                case HidFrameType.MouseMove:
                case HidFrameType.MouseResolution:
                    return $"{Type}: ({MouseCoordinate?.Item1}, {MouseCoordinate?.Item2})";
                case HidFrameType.MouseScroll:
                    return $"{Type}: {MouseScrollValue:+}";
                case HidFrameType.MousePress:
                case HidFrameType.MouseRelease:
                    return $"{Type}: {MouseButton}";
                case HidFrameType.KeyboardPress:
                case HidFrameType.KeyboardRelease:
                    return $"{Type}: {KeyboardKey}";
                default:
                    throw new ArgumentOutOfRangeException("Invalid HID Frame Type!");
            }
        }
    }
}
