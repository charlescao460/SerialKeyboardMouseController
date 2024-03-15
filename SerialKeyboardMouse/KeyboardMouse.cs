using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using SerialKeyboardMouse.Serial;

namespace SerialKeyboardMouse
{
    /// <summary>
    /// A Keyboard Mouse controller based on the serial devices.
    /// There are both async and blocking methods in this class.
    /// The frame order called with async methods is guaranteed. 
    /// However, Due to the nature of async, we do not guarantee the order when mixing both async and blocking methods.
    /// </summary>
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
        /// Set the absolute mouse's resolution with blocking I/O.
        /// Note that in firmware, it has a limitation of 8K resolution.
        /// </summary>
        /// <param name="width">Width of resolution.</param>
        /// <param name="height">Height of resolution. </param>
        /// <exception cref="ArgumentException">If supplied with non-positive values</exception>
        /// <exception cref="SerialDeviceException">If command failed.</exception>
        public void SetMouseResolution(int width, int height)
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
            _sender.SendFrame(frame);
        }

        /// <summary>
        /// Set the absolute mouse's resolution with async I/O.
        /// Note that in firmware, it has a limitation of 8K resolution.
        /// </summary>
        /// <param name="width">Width of resolution.</param>
        /// <param name="height">Height of resolution. </param>
        /// <exception cref="ArgumentException">If supplied with non-positive values</exception>
        /// <exception cref="SerialDeviceException">If command failed.</exception>
        public Task SetMouseResolutionAsync(int width, int height)
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
            return _sender.SendFrameAsync(frame);
        }

        /// <summary>
        /// Move the absolute mouse to desired coordinate with blocking I/O.
        /// </summary>
        /// <param name="x">Coordinate X</param>
        /// <param name="y">Coordinate Y </param>
        /// <exception cref="ArgumentException">If supplied with non-positive values or out of resolution range.</exception>
        /// <exception cref="SerialDeviceException">If command failed.</exception>
        public void MoveMouseToCoordinate(int x, int y)
        {
            if (x <= 0 || y <= 0 || x > MouseResolutionWidth || y > MouseResolutionHeight)
            {
                throw new ArgumentOutOfRangeException($"Mouse Coordinate {x},{y} is out of range {MouseResolutionWidth},{MouseResolutionHeight}!\n");
            }
            SerialCommandFrame frame
                = SerialCommandFrame.OfCoordinateType(SerialSymbols.FrameType.MouseMove,
                    new Tuple<ushort, ushort>((ushort)x, (ushort)y));
            _sender.SendFrame(frame);
        }

        /// <summary>
        /// Move the absolute mouse to desired coordinate with async I/O.
        /// </summary>
        /// <param name="x">Coordinate X</param>
        /// <param name="y">Coordinate Y </param>
        /// <exception cref="ArgumentException">If supplied with non-positive values or out of resolution range.</exception>
        /// <exception cref="SerialDeviceException">If command failed.</exception>
        public Task MoveMouseToCoordinateAsync(int x, int y)
        {
            if (x <= 0 || y <= 0 || x > MouseResolutionWidth || y > MouseResolutionHeight)
            {
                throw new ArgumentOutOfRangeException($"Mouse Coordinate {x},{y} is out of range {MouseResolutionWidth},{MouseResolutionHeight}!\n");
            }
            SerialCommandFrame frame
                = SerialCommandFrame.OfCoordinateType(SerialSymbols.FrameType.MouseMove,
                    new Tuple<ushort, ushort>((ushort)x, (ushort)y));
            return _sender.SendFrameAsync(frame);
        }

        /// <summary>
        /// Scroll the wheel with blocking I/O.
        /// </summary>
        /// <param name="value">Wheel delta</param>
        /// <exception cref="SerialDeviceException">If command failed.</exception>
        public void MouseScroll(sbyte value)
        {
            SerialCommandFrame frame = SerialCommandFrame.OfKeyType(SerialSymbols.FrameType.MouseScroll, (byte)value);
            _sender.SendFrame(frame);
        }

        /// <summary>
        /// Scroll the wheel with async I/O.
        /// </summary>
        /// <param name="value">Wheel delta</param>
        /// <exception cref="SerialDeviceException">If command failed.</exception>
        public Task MouseScrollAsync(sbyte value)
        {
            SerialCommandFrame frame = SerialCommandFrame.OfKeyType(SerialSymbols.FrameType.MouseScroll, (byte)value);
            return _sender.SendFrameAsync(frame);
        }

        /// <summary>
        /// Press mouse's button with blocking I/O.
        /// </summary>
        /// <param name="button"> Button to press.</param>
        /// <see cref="SerialSymbols.MouseButton"/>
        /// <seealso cref="MouseReleaseButtonAsync"/>
        /// <seealso cref="MouseReleaseAllButtonsAsync"/>
        /// <exception cref="SerialDeviceException">If command failed.</exception>
        public void MousePressButton(SerialSymbols.MouseButton button)
        {
            CheckMouseButton(button);
            SerialCommandFrame frame = SerialCommandFrame.OfKeyType(SerialSymbols.FrameType.MousePress, (byte)button);
            _sender.SendFrame(frame);
        }

        /// <summary>
        /// Press mouse's button with async I/O.
        /// </summary>
        /// <param name="button"> Button to press.</param>
        /// <see cref="SerialSymbols.MouseButton"/>
        /// <seealso cref="MouseReleaseButtonAsync"/>
        /// <seealso cref="MouseReleaseAllButtonsAsync"/>
        /// <exception cref="SerialDeviceException">If command failed.</exception>
        public Task MousePressButtonAsync(SerialSymbols.MouseButton button)
        {
            CheckMouseButton(button);
            SerialCommandFrame frame = SerialCommandFrame.OfKeyType(SerialSymbols.FrameType.MousePress, (byte)button);
            return _sender.SendFrameAsync(frame);
        }

        /// <summary>
        /// Release mouse's button with blocking I/O.
        /// </summary>
        /// <param name="button"> Button to release.</param>
        /// <see cref="SerialSymbols.MouseButton"/>
        /// <seealso cref="MousePressButtonAsync"/>
        /// <seealso cref="MouseReleaseAllButtonsAsync"/>
        /// <exception cref="SerialDeviceException">If command failed.</exception>
        public void MouseReleaseButton(SerialSymbols.MouseButton button)
        {
            CheckMouseButton(button);
            SerialCommandFrame frame = SerialCommandFrame.OfKeyType(SerialSymbols.FrameType.MouseRelease, (byte)button);
            _sender.SendFrame(frame);
        }

        /// <summary>
        /// Release mouse's button with async I/O.
        /// </summary>
        /// <param name="button"> Button to release.</param>
        /// <see cref="SerialSymbols.MouseButton"/>
        /// <seealso cref="MousePressButtonAsync"/>
        /// <seealso cref="MouseReleaseAllButtonsAsync"/>
        /// <exception cref="SerialDeviceException">If command failed.</exception>
        public Task MouseReleaseButtonAsync(SerialSymbols.MouseButton button)
        {
            CheckMouseButton(button);
            SerialCommandFrame frame = SerialCommandFrame.OfKeyType(SerialSymbols.FrameType.MouseRelease, (byte)button);
            return _sender.SendFrameAsync(frame);
        }

        /// <summary>
        /// Release all mouse's buttons with blocking I/O.
        /// </summary>
        /// <see cref="SerialSymbols.MouseButton"/>
        /// <seealso cref="MousePressButtonAsync"/>
        /// <seealso cref="MouseReleaseButtonAsync"/>
        /// <exception cref="SerialDeviceException">If command failed.</exception>
        public void MouseReleaseAllButtons()
        {
            SerialCommandFrame frame = SerialCommandFrame.OfKeyType(SerialSymbols.FrameType.MouseRelease, SerialSymbols.ReleaseAllKeys);
            _sender.SendFrame(frame);
        }


        /// <summary>
        /// Release all mouse's buttons with async I/O.
        /// </summary>
        /// <see cref="SerialSymbols.MouseButton"/>
        /// <seealso cref="MousePressButtonAsync"/>
        /// <seealso cref="MouseReleaseButtonAsync"/>
        /// <exception cref="SerialDeviceException">If command failed.</exception>
        public Task MouseReleaseAllButtonsAsync()
        {
            SerialCommandFrame frame = SerialCommandFrame.OfKeyType(SerialSymbols.FrameType.MouseRelease, SerialSymbols.ReleaseAllKeys);
            return _sender.SendFrameAsync(frame);
        }

        /// <summary>
        /// Press the specific key with blocking I/O.
        /// </summary>
        /// <param name="key">The HID usage id combined with modifiers.</param>
        /// <seealso cref="KeyboardReleaseAsync"/>
        /// <seealso cref="KeyboardReleaseAllAsync"/>
        /// <seealso cref="HidHelper.GetHidUsageFromPs2Set1"/>
        /// <exception cref="SerialDeviceException">If command failed.</exception>
        public void KeyboardPress(byte key)
        {
            SerialCommandFrame frame = SerialCommandFrame.OfKeyType(SerialSymbols.FrameType.KeyboardPress, key);
            _sender.SendFrame(frame);
            _keyboardKeyStates[key] = true;
        }

        /// <summary>
        /// Press the specific key with async I/O.
        /// </summary>
        /// <param name="key">The HID usage id combined with modifiers.</param>
        /// <seealso cref="KeyboardReleaseAsync"/>
        /// <seealso cref="KeyboardReleaseAllAsync"/>
        /// <seealso cref="HidHelper.GetHidUsageFromPs2Set1"/>
        /// <exception cref="SerialDeviceException">If command failed.</exception>
        public Task KeyboardPressAsync(byte key)
        {
            SerialCommandFrame frame = SerialCommandFrame.OfKeyType(SerialSymbols.FrameType.KeyboardPress, key);
            return _sender.SendFrameAsync(frame).ContinueWith(task => _keyboardKeyStates[key] = true);
        }

        /// <summary>
        /// Release the specific key with blocking I/O.
        /// </summary>
        /// <param name="key">The HID usage id combined with modifiers.</param>
        /// <seealso cref="KeyboardPressAsync"/>
        /// <seealso cref="KeyboardReleaseAllAsync"/>
        /// <seealso cref="HidHelper.GetHidUsageFromPs2Set1"/>
        /// <exception cref="SerialDeviceException">If command failed.</exception>
        public void KeyboardRelease(byte key)
        {
            SerialCommandFrame frame = SerialCommandFrame.OfKeyType(SerialSymbols.FrameType.KeyboardRelease, key);
            _sender.SendFrame(frame);
            _keyboardKeyStates[key] = false;
        }

        /// <summary>
        /// Release the specific key with async I/O.
        /// </summary>
        /// <param name="key">The HID usage id combined with modifiers.</param>
        /// <seealso cref="KeyboardPressAsync"/>
        /// <seealso cref="KeyboardReleaseAllAsync"/>
        /// <seealso cref="HidHelper.GetHidUsageFromPs2Set1"/>
        /// <exception cref="SerialDeviceException">If command failed.</exception>
        public Task KeyboardReleaseAsync(byte key)
        {
            SerialCommandFrame frame = SerialCommandFrame.OfKeyType(SerialSymbols.FrameType.KeyboardRelease, key);
            return _sender.SendFrameAsync(frame).ContinueWith(task => _keyboardKeyStates[key] = false);
        }

        /// <summary>
        /// Release all keys with blocking I/O.
        /// </summary>
        /// <seealso cref="KeyboardPressAsync"/>
        /// <seealso cref="KeyboardReleaseAsync"/>
        /// <seealso cref="HidHelper.GetHidUsageFromPs2Set1"/>
        /// <exception cref="SerialDeviceException">If command failed.</exception>
        public void KeyboardReleaseAll()
        {
            SerialCommandFrame frame = SerialCommandFrame.OfKeyType(SerialSymbols.FrameType.KeyboardRelease, SerialSymbols.ReleaseAllKeys);
            _sender.SendFrame(frame);
            Array.Fill(_keyboardKeyStates, false);
        }

        /// <summary>
        /// Release all keys with async I/O.
        /// </summary>
        /// <seealso cref="KeyboardPressAsync"/>
        /// <seealso cref="KeyboardReleaseAsync"/>
        /// <seealso cref="HidHelper.GetHidUsageFromPs2Set1"/>
        /// <exception cref="SerialDeviceException">If command failed.</exception>
        public Task KeyboardReleaseAllAsync()
        {
            SerialCommandFrame frame = SerialCommandFrame.OfKeyType(SerialSymbols.FrameType.KeyboardRelease, SerialSymbols.ReleaseAllKeys);
            return _sender.SendFrameAsync(frame).ContinueWith(task => Array.Fill(_keyboardKeyStates, false));
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
