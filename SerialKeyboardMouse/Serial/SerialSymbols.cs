using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace SerialKeyboardMouse.Serial
{
    /// <summary>
    /// Corresponding to serial_symbols.h in the Arduino Sketch 
    /// </summary>
    internal static class SerialSymbols
    {
        public const int BaudRate = 500000;

        public const byte FrameStart = 0xAB;

        public const int MinDataLength = 5; // <ID (2-byte)> <Type> <Value> <Checksum>

        public const int MinFrameLength = 7; // 0xAB <Length> <ID (2-byte)> <Type> <Value> <Checksum>

        public const int MaxDataLength = 16;

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

        public enum ReplyType : byte
        {
            OperationSucceed = 0x01,
            OperationError = 0x02,
            KeyboardLockStatus = 0x20,
            HostStatus = 0x21,
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
            { FrameType.MouseMoveRelatively, 10 }, // 0xAB <2-byte ID> 0x06 0xA0 <4-byte coordinate> <Checksum>
            { FrameType.MouseMove, 10 }, // 0xAB <2-byte ID> 0x06 0xAA <4-byte coordinate> <Checksum>
            { FrameType.MouseScroll, 7 }, // 0xAB <2-byte ID> 0x03 0xAB <Value> <Checksum>
            { FrameType.MousePress, 7 }, // 0xAB <2-byte ID> 0x03 0xAC <Key> <Checksum>
            { FrameType.MouseRelease, 7 }, // 0xAB <2-byte ID> 0x03 0xAD <Key> <Checksum>
            { FrameType.MouseResolution, 10 }, // 0xAB <2-byte ID> 0x06 0xAA <4-byte resolution> <Checksum>

            { FrameType.KeyboardPress, 7 }, // 0xAB <2-byte ID> 0x03 0xBB <Key> <Checksum>
            { FrameType.KeyboardRelease, 7 } // 0xAB <2-byte ID> 0x03 0xBC <Key> <Checksum>
        };

        /// <summary>
        /// All valid frame types
        /// </summary>
        public static readonly HashSet<FrameType> ValidFrameTypes = [.. FrameLengthLookup.Keys];

        public static byte CrcChecksum(Span<byte> data)
        {
            byte crc = 0x00;

            foreach (byte b in data)
            {
                crc ^= b;

                for (int i = 0; i < 8; i++)
                {
                    if ((crc & 0x80) != 0)
                    {
                        crc = (byte)((crc << 1) ^ 0x07);
                    }
                    else
                    {
                        crc <<= 1;
                    }
                }
            }

            return crc;
        }

        internal static bool CrcChecker(Span<byte> memory, byte desired)
        {
            return CrcChecksum(memory) == desired;
        }
    }
}
