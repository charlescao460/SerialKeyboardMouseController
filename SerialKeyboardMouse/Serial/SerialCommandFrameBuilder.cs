using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SerialKeyboardMouse.Serial
{
    internal class SerialCommandFrameBuilder
    {
        private int _frameID;

        public SerialCommandFrameBuilder()
        {
            _frameID = -1; // Start at -1 so first returned value is 0
        }

        private ushort NextId()
        {
            int value = Interlocked.Increment(ref _frameID);
            return (ushort)value; // automatic wrap via cast
        }

        /// <summary>
        /// Construct a Key type of serial frame. (Mouse buttons, mouse scroll or keyboard press/release)
        /// </summary>
        /// <param name="type">Type of serial command</param>
        /// <param name="key">Key or value of this command</param>
        /// <returns>Constructed frame</returns>
        /// <exception cref="ArgumentException"> If type is not key/value </exception>
        /// <remarks> Key was declared as byte type instead of dynamic type to avoid performance degradation. But we have to cast from enum to byte and reversely.</remarks>
        public SerialCommandFrame OfKeyType(SerialSymbols.FrameType type, byte key)
        {
            if (!SerialSymbols.KeyFrameTypes.Contains(type))
            {
                throw new ArgumentException("Type is not Key type!");
            }
            HidFrameInfo info = null;
            switch (type)
            {
                case SerialSymbols.FrameType.MouseScroll:
                    info = new HidFrameInfo((HidFrameType)type, null, null, key, null);
                    break;
                case SerialSymbols.FrameType.MousePress:
                    info = new HidFrameInfo((HidFrameType)type, (MouseButton)key, null, null, null);
                    break;
                case SerialSymbols.FrameType.MouseRelease:
                    info = key == SerialSymbols.ReleaseAllKeys
                        ? new HidFrameInfo(HidFrameType.MouseReleaseAll, null, null, null, null)
                        : new HidFrameInfo((HidFrameType)type, (MouseButton)key, null, null, null);
                    break;
                case SerialSymbols.FrameType.KeyboardPress:
                    info = new HidFrameInfo((HidFrameType)type, null, null, null, (HidKeyboardUsage)key);
                    break;
                case SerialSymbols.FrameType.KeyboardRelease:
                    info = key == SerialSymbols.ReleaseAllKeys
                        ? new HidFrameInfo(HidFrameType.KeyboardReleaseAll, null, null, null, null)
                        : new HidFrameInfo((HidFrameType)type, null, null, null, (HidKeyboardUsage)key);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, "Type is not Key type!");
            }
            return new SerialCommandFrame(type, info, NextId(), key, null, true);
        }

        /// <summary>
        /// Construct a coordinate type of serial frame. (Mouse move or change of resolution)
        /// </summary>
        /// <param name="type">Type of serial command</param>
        /// <param name="cord">Coordinate of this command. First is X, second is Y.</param>
        /// <returns>Constructed frame</returns>
        /// <exception cref="ArgumentException"> If type is not coordinate type.</exception>
        public SerialCommandFrame OfCoordinateType(SerialSymbols.FrameType type, Tuple<ushort, ushort> cord)
        {
            HidFrameInfo info = null;
            switch (type)
            {
                case SerialSymbols.FrameType.MouseMoveRelatively:
                    info = new HidFrameInfo((HidFrameType)type, null,
                        new Tuple<int, int>((short)cord.Item1, (short)cord.Item2), null, null);
                    break;
                case SerialSymbols.FrameType.MouseMove:
                case SerialSymbols.FrameType.MouseResolution:
                    info = new HidFrameInfo((HidFrameType)type, null,
                        new Tuple<int, int>(cord.Item1, cord.Item2), null, null);
                    break;
                default:
                    throw new ArgumentException("Type is not Coordinate type!");
            }
            return new SerialCommandFrame(type, info, NextId(), null, cord, false);
        }
    }
}
