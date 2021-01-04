using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using SerialKeyboardMouse.Serial;

namespace SerialKeyboardMouse
{
    public class KeyboardMouse : IDisposable
    {
        private readonly ReliableFrameSender _sender;
        private bool _disposedValue;

        public int MouseResolutionWidth { get; private set; }

        public int MouseResolutionHeight { get; private set; }

        public KeyboardMouse(ISerialAdaptor serial)
        {
            _sender = new ReliableFrameSender(serial);
        }

        public Task SetMouseResolution(int width, int height)
        {
            MouseResolutionWidth = width;
            MouseResolutionHeight = height;
            SerialCommandFrame frame
                = SerialCommandFrame.OfCoordinateType(SerialSymbols.FrameType.MouseResolution,
                    new Tuple<ushort, ushort>((ushort)width, (ushort)height));
            return _sender.SendFrame(frame.Bytes);
        }

        public Task MoveMouseToCoordinate(int x, int y)
        {
            if (x < 0 || y < 0 || x > MouseResolutionWidth || y > MouseResolutionHeight)
            {
                throw new ArgumentOutOfRangeException($"Mouse Coordinate {x},{y} is out of range {MouseResolutionWidth},{MouseResolutionHeight}!\n");
            }
            SerialCommandFrame frame
                = SerialCommandFrame.OfCoordinateType(SerialSymbols.FrameType.MouseMove,
                    new Tuple<ushort, ushort>((ushort)x, (ushort)y));
            return _sender.SendFrame(frame.Bytes);
        }

        public Task MouseScroll(sbyte value)
        {
            SerialCommandFrame frame = SerialCommandFrame.OfKeyType(SerialSymbols.FrameType.MouseScroll, (byte)value);
            return _sender.SendFrame(frame.Bytes);
        }

        public Task MousePressButton(SerialSymbols.MouseButton button)
        {
            CheckMouseButton(button);
            SerialCommandFrame frame = SerialCommandFrame.OfKeyType(SerialSymbols.FrameType.MousePress, (byte)button);
            return _sender.SendFrame(frame.Bytes);
        }

        public Task MouseReleaseButton(SerialSymbols.MouseButton button)
        {
            CheckMouseButton(button);
            SerialCommandFrame frame = SerialCommandFrame.OfKeyType(SerialSymbols.FrameType.MouseRelease, (byte)button);
            return _sender.SendFrame(frame.Bytes);
        }

        public Task MouseReleaseAllButtons()
        {
            SerialCommandFrame frame = SerialCommandFrame.OfKeyType(SerialSymbols.FrameType.MouseRelease, SerialSymbols.ReleaseAllKeys);
            return _sender.SendFrame(frame.Bytes);
        }

        public Task KeyboardPress(byte key)
        {
            SerialCommandFrame frame = SerialCommandFrame.OfKeyType(SerialSymbols.FrameType.KeyboardPress, key);
            return _sender.SendFrame(frame.Bytes);
        }

        public Task KeyboardRelease(byte key)
        {
            SerialCommandFrame frame = SerialCommandFrame.OfKeyType(SerialSymbols.FrameType.KeyboardRelease, key);
            return _sender.SendFrame(frame.Bytes);
        }

        private static void CheckMouseButton(SerialSymbols.MouseButton button)
        {
            if (!Enum.IsDefined(button))
            {
                throw new ArgumentException($"Unknown type of mouse button {button}.");
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _sender.Dispose();
                }
                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
