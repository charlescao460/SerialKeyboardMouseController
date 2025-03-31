using System;
using System.Buffers;
using System.Numerics;

namespace SerialKeyboardMouse.Serial
{
    internal class SerialCommandFrame
    {
        private static readonly ArrayPool<byte> FrameArrayPool
            = ArrayPool<byte>.Create(SerialSymbols.MaxFrameLength, 1000);

        /// <summary>
        /// Length of raw frame in bytes
        /// </summary>
        public int Length { get; private init; }

        private readonly SerialSymbols.FrameType _type;

        /// <summary>
        /// Type of this serial frame
        /// </summary>
        public SerialSymbols.FrameType Type
        {
            get => _type;
            private init
            {
                if (!SerialSymbols.FrameLengthLookup.TryGetValue(value, out int length))
                {
                    throw new ArgumentException("Invalid type of serial frame!");
                }
                _type = value;
                Length = length;
            }
        }

        /// <summary>
        /// Frame information for event callback provided by the library-user.
        /// </summary>
        public HidFrameInfo Info { get; }

        /// <summary>
        /// Key or value of press/release or scroll type, null otherwise
        /// </summary>
        public byte? Key { get; }

        /// <summary>
        /// Coordinate of move or resolution type, null otherwise
        /// </summary>
        public Tuple<ushort, ushort> Coordinate { get; }

        private readonly byte[] _bytes;
        private readonly Lazy<Memory<byte>> _bytesMemory;

        /// <summary>
        /// Bytes that are ready to send
        /// </summary>
        public Memory<byte> Bytes => _bytesMemory.Value;

        private readonly bool _isKeyType;

        private SerialCommandFrame(SerialSymbols.FrameType type, HidFrameInfo info, byte? key, Tuple<ushort, ushort> cord, bool keyType)
        {
            Type = type;
            Info = info;
            Key = key;
            Coordinate = cord;
            _bytes = FrameArrayPool.Rent(SerialSymbols.MaxFrameLength);
            _isKeyType = keyType;
            _bytesMemory = new Lazy<Memory<byte>>(() =>
            {
                FillFrameBytes();
                return new Memory<byte>(_bytes, 0, Length);
            });
        }

        private void FillFrameBytes()
        {
            _bytes[0] = SerialSymbols.FrameStart;
            _bytes[1] = (byte)(Length - 2);
            _bytes[2] = (byte)Type;
            if (_isKeyType)
            {
                _bytes[3] = Key.Value;
                _bytes[4] = SerialSymbols.XorChecksum(new Span<byte>(_bytes, 2, 2));
            }
            else
            {
                ushort x = Coordinate.Item1;
                ushort y = Coordinate.Item2;
                if (!BitConverter.TryWriteBytes(new Span<byte>(_bytes, 3, 2), x)
                    || !BitConverter.TryWriteBytes(new Span<byte>(_bytes, 5, 2), y))
                {
                    throw new Exception("BitConverter failed.");
                }
                _bytes[7] = SerialSymbols.XorChecksum(new Span<byte>(_bytes, 2, 5));
            }
        }

        ~SerialCommandFrame()
        {
            FrameArrayPool.Return(_bytes, true);
        }

        /// <summary>
        /// Construct a Key type of serial frame. (Mouse buttons, mouse scroll or keyboard press/release)
        /// </summary>
        /// <param name="type">Type of serial command</param>
        /// <param name="key">Key or value of this command</param>
        /// <returns>Constructed frame</returns>
        /// <exception cref="ArgumentException"> If type is not key/value </exception>
        /// <remarks> Key was declared as byte type instead of dynamic type to avoid performance degradation. But we have to cast from enum to byte and reversely.</remarks>
        public static SerialCommandFrame OfKeyType(SerialSymbols.FrameType type, byte key)
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
            return new SerialCommandFrame(type, info, key, null, true);
        }

        /// <summary>
        /// Construct a coordinate type of serial frame. (Mouse move or change of resolution)
        /// </summary>
        /// <param name="type">Type of serial command</param>
        /// <param name="cord">Coordinate of this command. First is X, second is Y.</param>
        /// <returns>Constructed frame</returns>
        /// <exception cref="ArgumentException"> If type is not coordinate type.</exception>
        public static SerialCommandFrame OfCoordinateType(SerialSymbols.FrameType type, Tuple<ushort, ushort> cord)
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
            return new SerialCommandFrame(type, info, null, cord, false);
        }
    }
}
