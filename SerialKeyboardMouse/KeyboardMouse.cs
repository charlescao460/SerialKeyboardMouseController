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
        private readonly bool[] _keyboardKeyStates;

        public int MouseResolutionWidth { get; private set; }

        public int MouseResolutionHeight { get; private set; }

        public KeyboardMouse(ISerialAdaptor serial)
        {
            _sender = new ReliableFrameSender(serial);
            _keyboardKeyStates = new bool[byte.MaxValue + 1];
        }

        /// <summary>
        /// Set the absolute mouse's resolution.
        /// Note that in firmware, it has a limitation of 8K resolution.
        /// </summary>
        /// <param name="width">Width of resolution.</param>
        /// <param name="height">Height of resolution. </param>
        /// <exception cref="ArgumentException">If supplied with non-positive values</exception>
        /// <exception cref="SerialDeviceException">If command failed.</exception>
        public Task SetMouseResolution(int width, int height)
        {
            if (width <= 0 || height <= 0)
            {
                throw new ArgumentException("Resolution values cannot be negative!");
            }
            MouseResolutionWidth = width;
            MouseResolutionHeight = height;
            SerialCommandFrame frame
                = SerialCommandFrame.OfCoordinateType(SerialSymbols.FrameType.MouseResolution,
                    new Tuple<ushort, ushort>((ushort)width, (ushort)height));
            return _sender.SendFrame(frame);
        }

        /// <summary>
        /// Move the absolute mouse to desired coordinate.
        /// </summary>
        /// <param name="x">Coordinate X</param>
        /// <param name="y">Coordinate Y </param>
        /// <exception cref="ArgumentException">If supplied with non-positive values or out of resolution range.</exception>
        /// <exception cref="SerialDeviceException">If command failed.</exception>
        public Task MoveMouseToCoordinate(int x, int y)
        {
            if (x <= 0 || y <= 0 || x > MouseResolutionWidth || y > MouseResolutionHeight)
            {
                throw new ArgumentOutOfRangeException($"Mouse Coordinate {x},{y} is out of range {MouseResolutionWidth},{MouseResolutionHeight}!\n");
            }
            SerialCommandFrame frame
                = SerialCommandFrame.OfCoordinateType(SerialSymbols.FrameType.MouseMove,
                    new Tuple<ushort, ushort>((ushort)x, (ushort)y));
            return _sender.SendFrame(frame);
        }

        /// <summary>
        /// Scroll the wheel
        /// </summary>
        /// <param name="value">Wheel delta</param>
        /// <exception cref="SerialDeviceException">If command failed.</exception>
        public Task MouseScroll(sbyte value)
        {
            SerialCommandFrame frame = SerialCommandFrame.OfKeyType(SerialSymbols.FrameType.MouseScroll, (byte)value);
            return _sender.SendFrame(frame);
        }

        /// <summary>
        /// Press mouse's button.
        /// </summary>
        /// <param name="button"> Button to press.</param>
        /// <see cref="SerialSymbols.MouseButton"/>
        /// <seealso cref="MouseReleaseButton"/>
        /// <seealso cref="MouseReleaseAllButtons"/>
        /// <exception cref="SerialDeviceException">If command failed.</exception>
        public Task MousePressButton(SerialSymbols.MouseButton button)
        {
            CheckMouseButton(button);
            SerialCommandFrame frame = SerialCommandFrame.OfKeyType(SerialSymbols.FrameType.MousePress, (byte)button);
            return _sender.SendFrame(frame);
        }

        /// <summary>
        /// Release mouse's button.
        /// </summary>
        /// <param name="button"> Button to release.</param>
        /// <see cref="SerialSymbols.MouseButton"/>
        /// <seealso cref="MousePressButton"/>
        /// <seealso cref="MouseReleaseAllButtons"/>
        /// <exception cref="SerialDeviceException">If command failed.</exception>
        public Task MouseReleaseButton(SerialSymbols.MouseButton button)
        {
            CheckMouseButton(button);
            SerialCommandFrame frame = SerialCommandFrame.OfKeyType(SerialSymbols.FrameType.MouseRelease, (byte)button);
            return _sender.SendFrame(frame);
        }

        /// <summary>
        /// Release all mouse's buttons.
        /// </summary>
        /// <see cref="SerialSymbols.MouseButton"/>
        /// <seealso cref="MousePressButton"/>
        /// <seealso cref="MouseReleaseButton"/>
        /// <exception cref="SerialDeviceException">If command failed.</exception>
        public Task MouseReleaseAllButtons()
        {
            SerialCommandFrame frame = SerialCommandFrame.OfKeyType(SerialSymbols.FrameType.MouseRelease, SerialSymbols.ReleaseAllKeys);
            return _sender.SendFrame(frame);
        }

        /// <summary>
        /// Press the specific key. 
        /// </summary>
        /// <param name="key">The HID usage id combined with modifiers.</param>
        /// <seealso cref="KeyboardRelease"/>
        /// <seealso cref="KeyboardReleaseAll"/>
        /// <seealso cref="HidHelper.GetHidUsageFromPs2Set1"/>
        /// <exception cref="SerialDeviceException">If command failed.</exception>
        public Task KeyboardPress(byte key)
        {
            SerialCommandFrame frame = SerialCommandFrame.OfKeyType(SerialSymbols.FrameType.KeyboardPress, key);
            return _sender.SendFrame(frame).ContinueWith(task => _keyboardKeyStates[key] = true);
        }

        /// <summary>
        /// Release the specific key. 
        /// </summary>
        /// <param name="key">The HID usage id combined with modifiers.</param>
        /// <seealso cref="KeyboardPress"/>
        /// <seealso cref="KeyboardReleaseAll"/>
        /// <seealso cref="HidHelper.GetHidUsageFromPs2Set1"/>
        /// <exception cref="SerialDeviceException">If command failed.</exception>
        public Task KeyboardRelease(byte key)
        {
            SerialCommandFrame frame = SerialCommandFrame.OfKeyType(SerialSymbols.FrameType.KeyboardRelease, key);
            return _sender.SendFrame(frame).ContinueWith(task => _keyboardKeyStates[key] = false);
        }

        /// <summary>
        /// Release all keys.
        /// </summary>
        /// <seealso cref="KeyboardPress"/>
        /// <seealso cref="KeyboardRelease"/>
        /// <seealso cref="HidHelper.GetHidUsageFromPs2Set1"/>
        /// <exception cref="SerialDeviceException">If command failed.</exception>
        public Task KeyboardReleaseAll()
        {
            SerialCommandFrame frame = SerialCommandFrame.OfKeyType(SerialSymbols.FrameType.KeyboardRelease, SerialSymbols.ReleaseAllKeys);
            return _sender.SendFrame(frame).ContinueWith(task => Array.Fill(_keyboardKeyStates, false));
        }

        /// <summary>
        /// Return true if a key is currently pressed.
        /// </summary>
        public bool KeyboardIsPressed(byte key)
        {
            return _keyboardKeyStates[key];
        }

        /// <summary>
        /// Helper function to check mouse button and throw exception.
        /// </summary>
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
