using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SerialKeyboardMouse
{
    /// <summary>
    /// Event will be raised when we send a new HID report through hardware.
    /// </summary>
    /// <param name="args"></param>
    public delegate void KeyboardMouseEvent(KeyboardMouseEventArgs args);

    /// <summary>
    /// Event arguments for Keyboard-Mouse event.
    /// Such event will be raised when we send a new HID report through hardware.
    /// </summary>
    public class KeyboardMouseEventArgs : EventArgs
    {
        /// <summary>
        /// Time immediately before sending the frame to hardware through serial. 
        /// </summary>
        public DateTime Time { get; }

        /// <summary>
        /// Frame Info
        /// </summary>
        public HidFrameInfo Info { get; }

        internal KeyboardMouseEventArgs(DateTime time, HidFrameInfo info)
        {
            Time = time;
            Info = info;
        }
    }

    /// <summary>
    /// Event raised when a keyboard key or a mouse button has been pressed for a long time.
    /// </summary>
    public delegate void PressTimeOutEvent(PressTimeOutEventArgs args);

    public class PressTimeOutEventArgs : EventArgs
    {
        public MouseButton? MouseButton { get; }

        public HidKeyboardUsage? KeyboardKey { get; }

        /// <summary>
        /// Set true if you want the button or key being release after the event handler returned.
        /// </summary>
        public bool ShouldRelease { get; set; }

        internal PressTimeOutEventArgs(MouseButton? mouseButton, HidKeyboardUsage? keyboardKey)
        {
            MouseButton = mouseButton;
            KeyboardKey = keyboardKey;
        }
    }
}
