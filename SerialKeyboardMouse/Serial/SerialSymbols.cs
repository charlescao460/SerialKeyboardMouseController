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

        public const int MinFrameLength = 5; // 0xAB <Length> <Type> <Value> <Checksum>

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

        public const int ReleaseAllKeys = 0x00;

        /// <summary>
        /// Set of all key/value frame type. (E.g. Scroll or key press)
        /// </summary>
        public static HashSet<FrameType> KeyFrameTypes = new HashSet<FrameType>
        {
            FrameType.MouseScroll,
            FrameType.MousePress,
            FrameType.MouseRelease,
            FrameType.KeyboardPress,
            FrameType.KeyboardRelease,
        };

        /// <summary>
        /// Set of all coordinate frame type (E.g. Mouse move or change resolution).
        /// </summary>
        public static HashSet<FrameType> CoordinateFrameTypes = new HashSet<FrameType>
        {
            FrameType.MouseMove,
            FrameType.MouseResolution,
        };

        /// <summary>
        /// Dictionary mapped frame type to frame length
        /// </summary>
        public static Dictionary<FrameType, int> FrameLengthLookup =
            new Dictionary<FrameType, int>
            {
                {FrameType.MouseMove, 8}, // 0xAB 0x06 0xAA <4-byte coordinate> <Checksum>
                {FrameType.MouseScroll, 5}, // 0xAB 0x03 0xAB <Value> <Checksum>
                {FrameType.MousePress, 5}, // 0xAB 0x03 0xAC <Key> <Checksum>
                {FrameType.MouseRelease, 5}, // 0xAB 0x03 0xAD <Key> <Checksum>
                {FrameType.MouseResolution, 8}, // 0xAB 0x06 0xAA <4-byte resolution> <Checksum>

                {FrameType.KeyboardPress, 5}, // 0xAB 0x03 0xBB <Key> <Checksum>
                {FrameType.KeyboardRelease, 5} // 0xAB 0x03 0xBC <Key> <Checksum>
            };

        /// <summary>
        /// All valid frame types
        /// </summary>
        public static HashSet<FrameType> ValidFrameTypes
            = new HashSet<FrameType>(FrameLengthLookup.Keys);

        public static byte XorChecksum(Memory<byte> memory)
        {
            if (memory.Length == 0)
            {
                return 0;
            }
            Span<byte> arr = memory.Span;
            byte ret = arr[0];
            for (int i = 1; i < arr.Length; ++i)
            {
                ret ^= arr[i];
            }
            return ret;
        }

        public static bool XorChecker(Memory<byte> memory, byte desired)
        {
            return XorChecksum(memory) == desired;
        }



    }

}
