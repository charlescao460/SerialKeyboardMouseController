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
    public class KeyboardMouseEventArgs
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
}
