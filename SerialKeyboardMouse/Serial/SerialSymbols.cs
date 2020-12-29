using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SerialKeyboardMouse.Serial
{
    public static class SerialSymbols
    {
        public const int BaudRate = 500000;

        public const byte FrameStart = 0xAB;

        public const int MaxDataLength = 6;

        public const int MaxFrameLength = MaxDataLength + 2;

        public enum FrameType
        {
            MouseMove = 0xAA,
            MouseScroll = 0xAB,
            MousePress = 0xAC,
            MouseRelease = 0xAD,
            MouseResolution = 0xAE,

            KeyboardPress = 0xBB,
            KeyboardRelease = 0xBC,

            Unknown = 0xFF
        }

    }

}
