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

        /// <summary>
        /// Frame Id
        /// </summary>
        public ushort FrameId { get; }

        private readonly byte[] _bytes;

        /// <summary>
        /// Bytes that are ready to send
        /// </summary>
        public Span<byte> Bytes => new Span<byte>(_bytes, 0, Length);

        private readonly bool _isKeyType;

        internal SerialCommandFrame(SerialSymbols.FrameType type, HidFrameInfo info, ushort frameId, byte? key, Tuple<ushort, ushort> cord, bool keyType)
        {
            Type = type;
            Info = info;
            Key = key;
            Coordinate = cord;
            FrameId = frameId;
            _bytes = FrameArrayPool.Rent(SerialSymbols.MaxFrameLength);
            _isKeyType = keyType;
            FillFrameBytes();
        }

        private void FillFrameBytes()
        {
            _bytes[0] = SerialSymbols.FrameStart;
            _bytes[1] = (byte)(Length - 2);
            BitConverter.TryWriteBytes(new Span<byte>(_bytes, 2, 2), FrameId); // Should always succeed as we are hard-coded length
            _bytes[4] = (byte)Type;
            if (_isKeyType)
            {
                _bytes[5] = Key.Value;
            }
            else
            {
                ushort x = Coordinate.Item1;
                ushort y = Coordinate.Item2;
                BitConverter.TryWriteBytes(new Span<byte>(_bytes, 5, 2), x);
                BitConverter.TryWriteBytes(new Span<byte>(_bytes, 7, 2), y);
            }
            _bytes[Length - 1] = SerialSymbols.CrcChecksum(new Span<byte>(_bytes, 2, Length - 3));
        }

        ~SerialCommandFrame()
        {
            FrameArrayPool.Return(_bytes, true);
        }


    }
}
