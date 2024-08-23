using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SerialKeyboardMouse.Serial
{
    /// <summary>
    /// Corresponding to serial_symbols.h in the Arduino Sketch 
    /// </summary>
    internal static class SerialSymbols
    {
        public const int BaudRate = 500000;

        public const byte FrameStart = 0xAB;

        public const int MinFrameLength = 5; // 0xAB <Length> <Type> <Value> <Checksum>

        public const int MaxDataLength = 6; // Coordinate type, 4-byte coordinates + <Type> + <Checksum>

        public const int MaxFrameLength = MaxDataLength + 2;

        public enum FrameType : byte
        {
            MouseMoveRelatively = 0xA0,
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
        public static readonly HashSet<FrameType> KeyFrameTypes =
        [
            FrameType.MouseScroll,
            FrameType.MousePress,
            FrameType.MouseRelease,
            FrameType.KeyboardPress,
            FrameType.KeyboardRelease
        ];

        /// <summary>
        /// Set of all coordinate frame type (E.g. Mouse move or change resolution).
        /// </summary>
        public static readonly HashSet<FrameType> CoordinateFrameTypes =
        [
            FrameType.MouseMove,
            FrameType.MouseResolution
        ];

        /// <summary>
        /// Dictionary mapped frame type to frame length
        /// </summary>
        public static readonly Dictionary<FrameType, int> FrameLengthLookup = new()
        {
            { FrameType.MouseMoveRelatively, 8 }, // 0xAB 0x06 0xA0 <4-byte coordinate> <Checksum>
            { FrameType.MouseMove, 8 }, // 0xAB 0x06 0xAA <4-byte coordinate> <Checksum>
            { FrameType.MouseScroll, 5 }, // 0xAB 0x03 0xAB <Value> <Checksum>
            { FrameType.MousePress, 5 }, // 0xAB 0x03 0xAC <Key> <Checksum>
            { FrameType.MouseRelease, 5 }, // 0xAB 0x03 0xAD <Key> <Checksum>
            { FrameType.MouseResolution, 8 }, // 0xAB 0x06 0xAA <4-byte resolution> <Checksum>

            { FrameType.KeyboardPress, 5 }, // 0xAB 0x03 0xBB <Key> <Checksum>
            { FrameType.KeyboardRelease, 5 } // 0xAB 0x03 0xBC <Key> <Checksum>
        };

        /// <summary>
        /// All valid frame types
        /// </summary>
        public static readonly HashSet<FrameType> ValidFrameTypes = [.. FrameLengthLookup.Keys];

        public static byte XorChecksum(Span<byte> memory)
        {
            if (memory.Length == 0)
            {
                return 0;
            }
            byte ret = memory[0];
            for (int i = 1; i < memory.Length; ++i)
            {
                ret ^= memory[i];
            }
            return ret;
        }

        internal static bool XorChecker(Span<byte> memory, byte desired)
        {
            return XorChecksum(memory) == desired;
        }
    }
}
