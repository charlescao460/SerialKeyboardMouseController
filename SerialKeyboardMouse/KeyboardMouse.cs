using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using SerialKeyboardMouse.Serial;
using Timer = System.Timers.Timer;

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
        private readonly ConcurrentDictionary<HidKeyboardUsage, DateTime?> _keyboardPressTimes;
        private readonly ConcurrentDictionary<MouseButton, DateTime?> _mousePressTimes;
        private Tuple<int, int> _mouseResolution;
        private Tuple<int, int> _mousePosition;
        private KeyButtonPressTimeoutMonitor _pressMonitor;

        public Tuple<int, int> MouseResolutionTuple => _mouseResolution;

        public int MouseResolutionWidth => _mouseResolution.Item1;

        public int MouseResolutionHeight => _mouseResolution.Item2;

        /// <summary>
        /// Get mouse position of last mouse move. The origin [0,0] is upper-left of the screen. 
        /// </summary>
        public Tuple<int, int> MousePositionTuple => _mousePosition;

        public int MousePositionX => _mousePosition.Item1;

        public int MousePositionY => _mousePosition.Item2;

        /// <summary>
        /// Event will be raised when we send a new HID report through hardware.
        /// </summary>
        public event KeyboardMouseEvent OnOperation;

        /// <summary>
        /// Event raised when a key or button has been pressed for a long time.
        /// Need to enable through 
        /// </summary>
        /// <see cref="EnablePressMonitor"/>
        public event PressTimeOutEvent OnPressTimeout;

        /// <summary>
        /// Constructor of the KeyboardMouse. 
        /// </summary>
        /// <param name="serial">The serial adaptor.
        /// The ownership of such object is provided to this instance.
        /// It will be managed and disposed by this instance.
        /// </param>
        public KeyboardMouse(ISerialAdaptor serial)
        {
            _sender = new ReliableFrameSender(serial);
            _keyboardPressTimes = new ConcurrentDictionary<HidKeyboardUsage, DateTime?>();
            _mousePressTimes = new ConcurrentDictionary<MouseButton, DateTime?>();
            Array.ForEach(Enum.GetValues<HidKeyboardUsage>(), key => _keyboardPressTimes[key] = null);
            Array.ForEach(Enum.GetValues<MouseButton>(), key => _mousePressTimes[key] = null);
            _mouseResolution = new Tuple<int, int>(1920, 1080); // Default values in Arduino Sketch
            _mousePosition = new Tuple<int, int>(-1, -1);
            _sender.OnSendingReport += (e) => OnOperation?.Invoke(e);
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
            SerialCommandFrame frame
                = SerialCommandFrame.OfCoordinateType(SerialSymbols.FrameType.MouseResolution,
                    new Tuple<ushort, ushort>((ushort)width, (ushort)height));
            _sender.SendFrame(frame, _ => _mouseResolution = new Tuple<int, int>(width, height));
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
            SerialCommandFrame frame
                = SerialCommandFrame.OfCoordinateType(SerialSymbols.FrameType.MouseResolution,
                    new Tuple<ushort, ushort>((ushort)width, (ushort)height));
            return _sender.SendFrameAsync(frame, _ => _mouseResolution = new Tuple<int, int>(width, height));
        }

        /// <summary>
        /// Move the absolute mouse to desired coordinate with blocking I/O. The origin [0,0] is upper-left. 
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
            _sender.SendFrame(frame, _ => _mousePosition = new Tuple<int, int>(x, y));
        }

        /// <summary>
        /// Move the absolute mouse to desired coordinate with async I/O. The origin [0,0] is upper-left. 
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
            return _sender.SendFrameAsync(frame, _ => _mousePosition = new Tuple<int, int>(x, y));
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
        /// <see cref="MouseButton"/>
        /// <seealso cref="MouseReleaseButton"/>
        /// <seealso cref="MouseReleaseAllButtons"/>
        /// <exception cref="SerialDeviceException">If command failed.</exception>
        public void MousePressButton(MouseButton button)
        {
            CheckMouseButton(button);
            SerialCommandFrame frame = SerialCommandFrame.OfKeyType(SerialSymbols.FrameType.MousePress, (byte)button);
            _sender.SendFrame(frame, t => _mousePressTimes[button] = t);
        }

        /// <summary>
        /// Press mouse's button with async I/O.
        /// </summary>
        /// <param name="button"> Button to press.</param>
        /// <see cref="MouseButton"/>
        /// <seealso cref="MouseReleaseButtonAsync"/>
        /// <seealso cref="MouseReleaseAllButtonsAsync"/>
        /// <exception cref="SerialDeviceException">If command failed.</exception>
        public Task MousePressButtonAsync(MouseButton button)
        {
            CheckMouseButton(button);
            SerialCommandFrame frame = SerialCommandFrame.OfKeyType(SerialSymbols.FrameType.MousePress, (byte)button);
            return _sender.SendFrameAsync(frame, t => _mousePressTimes[button] = t);
        }

        /// <summary>
        /// Release mouse's button with blocking I/O.
        /// </summary>
        /// <param name="button"> Button to release.</param>
        /// <see cref="MouseButton"/>
        /// <seealso cref="MousePressButton"/>
        /// <seealso cref="MouseReleaseAllButtons"/>
        /// <exception cref="SerialDeviceException">If command failed.</exception>
        public void MouseReleaseButton(MouseButton button)
        {
            CheckMouseButton(button);
            SerialCommandFrame frame = SerialCommandFrame.OfKeyType(SerialSymbols.FrameType.MouseRelease, (byte)button);
            _sender.SendFrame(frame, _ => _mousePressTimes[button] = null);
        }

        /// <summary>
        /// Release mouse's button with async I/O.
        /// </summary>
        /// <param name="button"> Button to release.</param>
        /// <see cref="MouseButton"/>
        /// <seealso cref="MousePressButtonAsync"/>
        /// <seealso cref="MouseReleaseAllButtonsAsync"/>
        /// <exception cref="SerialDeviceException">If command failed.</exception>
        public Task MouseReleaseButtonAsync(MouseButton button)
        {
            CheckMouseButton(button);
            SerialCommandFrame frame = SerialCommandFrame.OfKeyType(SerialSymbols.FrameType.MouseRelease, (byte)button);
            return _sender.SendFrameAsync(frame, _ => _mousePressTimes[button] = null);
        }

        /// <summary>
        /// Release all mouse's buttons with blocking I/O.
        /// </summary>
        /// <see cref="MouseButton"/>
        /// <seealso cref="MousePressButton"/>
        /// <seealso cref="MouseReleaseButton"/>
        /// <exception cref="SerialDeviceException">If command failed.</exception>
        public void MouseReleaseAllButtons()
        {
            SerialCommandFrame frame = SerialCommandFrame.OfKeyType(SerialSymbols.FrameType.MouseRelease, SerialSymbols.ReleaseAllKeys);
            _sender.SendFrame(frame, _ =>
            {
                foreach (var k in Enum.GetValues<MouseButton>())
                {
                    _mousePressTimes[k] = null;
                }
            });
        }


        /// <summary>
        /// Release all mouse's buttons with async I/O.
        /// </summary>
        /// <see cref="MouseButton"/>
        /// <seealso cref="MousePressButtonAsync"/>
        /// <seealso cref="MouseReleaseButtonAsync"/>
        /// <exception cref="SerialDeviceException">If command failed.</exception>
        public Task MouseReleaseAllButtonsAsync()
        {
            SerialCommandFrame frame = SerialCommandFrame.OfKeyType(SerialSymbols.FrameType.MouseRelease, SerialSymbols.ReleaseAllKeys);
            return _sender.SendFrameAsync(frame, _ =>
            {
                foreach (var k in Enum.GetValues<MouseButton>())
                {
                    _mousePressTimes[k] = null;
                }
            });
        }

        /// <summary>
        /// Press the specific key with blocking I/O.
        /// </summary>
        /// <param name="key">The HID usage id combined with modifiers.</param>
        /// <seealso cref="KeyboardRelease"/>
        /// <seealso cref="KeyboardReleaseAll"/>
        /// <seealso cref="HidHelper.GetHidUsageFromPs2Set1"/>
        /// <exception cref="SerialDeviceException">If command failed.</exception>
        public void KeyboardPress(HidKeyboardUsage key)
        {
            SerialCommandFrame frame = SerialCommandFrame.OfKeyType(SerialSymbols.FrameType.KeyboardPress, (byte)key);
            _sender.SendFrame(frame, t => _keyboardPressTimes[key] = t);
        }

        /// <summary>
        /// Press the specific key with async I/O.
        /// </summary>
        /// <param name="key">The HID usage id combined with modifiers.</param>
        /// <seealso cref="KeyboardReleaseAsync"/>
        /// <seealso cref="KeyboardReleaseAllAsync"/>
        /// <seealso cref="HidHelper.GetHidUsageFromPs2Set1"/>
        /// <exception cref="SerialDeviceException">If command failed.</exception>
        public Task KeyboardPressAsync(HidKeyboardUsage key)
        {
            SerialCommandFrame frame = SerialCommandFrame.OfKeyType(SerialSymbols.FrameType.KeyboardPress, (byte)key);
            return _sender.SendFrameAsync(frame, t => _keyboardPressTimes[key] = t);
        }

        /// <summary>
        /// Release the specific key with blocking I/O.
        /// </summary>
        /// <param name="key">The HID usage id combined with modifiers.</param>
        /// <seealso cref="KeyboardPress"/>
        /// <seealso cref="KeyboardReleaseAll"/>
        /// <seealso cref="HidHelper.GetHidUsageFromPs2Set1"/>
        /// <exception cref="SerialDeviceException">If command failed.</exception>
        /// <remarks>We don't check for repeated release, because Arduino will handle it. </remarks>
        public void KeyboardRelease(HidKeyboardUsage key)
        {
            SerialCommandFrame frame = SerialCommandFrame.OfKeyType(SerialSymbols.FrameType.KeyboardRelease, (byte)key);
            _sender.SendFrame(frame, _ => _keyboardPressTimes[key] = null);
        }

        /// <summary>
        /// Release the specific key with async I/O.
        /// </summary>
        /// <param name="key">The HID usage id combined with modifiers.</param>
        /// <seealso cref="KeyboardPressAsync"/>
        /// <seealso cref="KeyboardReleaseAllAsync"/>
        /// <seealso cref="HidHelper.GetHidUsageFromPs2Set1"/>
        /// <exception cref="SerialDeviceException">If command failed.</exception>
        /// <remarks>We don't check for repeated release, because Arduino will handle it. </remarks>
        public Task KeyboardReleaseAsync(HidKeyboardUsage key)
        {
            SerialCommandFrame frame = SerialCommandFrame.OfKeyType(SerialSymbols.FrameType.KeyboardRelease, (byte)key);
            return _sender.SendFrameAsync(frame, _ => _keyboardPressTimes[key] = null);
        }

        /// <summary>
        /// Release all keys with blocking I/O.
        /// </summary>
        /// <seealso cref="KeyboardPress"/>
        /// <seealso cref="KeyboardRelease"/>
        /// <seealso cref="HidHelper.GetHidUsageFromPs2Set1"/>
        /// <exception cref="SerialDeviceException">If command failed.</exception>
        public void KeyboardReleaseAll()
        {
            SerialCommandFrame frame = SerialCommandFrame.OfKeyType(SerialSymbols.FrameType.KeyboardRelease, SerialSymbols.ReleaseAllKeys);
            _sender.SendFrame(frame, _ =>
            {
                foreach (var k in Enum.GetValues<HidKeyboardUsage>())
                {
                    _keyboardPressTimes[k] = null;
                }
            });
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
            return _sender.SendFrameAsync(frame, _ =>
            {
                foreach (var k in Enum.GetValues<HidKeyboardUsage>())
                {
                    _keyboardPressTimes[k] = null;
                }
            });
        }

        /// <summary>
        /// Return true if a key is currently pressed.
        /// </summary>
        public bool KeyboardIsPressed(HidKeyboardUsage key)
        {
            return _keyboardPressTimes[key] != null;
        }

        /// <summary>
        /// Return true if a mouse button (left, right, middle) is currently pressed.
        /// </summary>
        public bool MouseButtonIsPressed(MouseButton button)
        {
            return _mousePressTimes[button] != null;
        }

        /// <summary>
        /// Enable the press timeout monitoring. Must set <see cref="OnPressTimeout"/> before enabling. 
        /// </summary>
        /// <param name="period">The interval of checking timeout.</param>
        /// <param name="timeout">The longest time that a key or button can be pressed.</param>
        /// <exception cref="InvalidOperationException">If <see cref="OnPressTimeout"/> is not set. Or if it is already enable. </exception>
        /// <see cref="OnPressTimeout"/>
        /// <seealso cref="DisablePressMonitor"/>
        public void EnablePressMonitor(TimeSpan period, TimeSpan timeout)
        {
            if (OnPressTimeout == null)
            {
                throw new InvalidOperationException("OnPressTimeout Event Handler Must Be Set!");
            }

            if (_pressMonitor != null)
            {
                throw new InvalidOperationException("Press Timeout Monitoring Already Enable!");
            }

            _pressMonitor = new KeyButtonPressTimeoutMonitor(this, period, timeout, arg => OnPressTimeout?.Invoke(arg));
        }

        /// <summary>
        /// Disable the press timeout monitoring.
        /// </summary>
        /// <seealso cref="EnablePressMonitor"/>
        /// <seealso cref="OnPressTimeout"/>
        public void DisablePressMonitor()
        {
            _pressMonitor.Dispose();
            _pressMonitor = null;
        }


        /// <summary>
        /// Helper function to check mouse button and throw exception.
        /// </summary>
        private static void CheckMouseButton(MouseButton button)
        {
            if (!Enum.IsDefined(button))
            {
                throw new ArgumentException($"Unknown type of mouse button {button}.");
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposedValue)
            {
                return;
            }
            if (disposing)
            {
                _pressMonitor?.Dispose();
                // Try to release all keys/buttons
                try
                {
                    KeyboardReleaseAll();
                    MouseReleaseAllButtons();
                }
                catch (Exception)
                {
                    // ignored
                }
                _sender.Dispose();
            }
            _disposedValue = true;
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// A thread monitor to repeatedly monitor the pressed keys on keyboard and mouse buttons.
        /// In case of a key that was pressed too long, it will raise the event.
        /// </summary>
        private sealed class KeyButtonPressTimeoutMonitor : IDisposable
        {
            private readonly Timer _timer;
            private readonly KeyboardMouse _km;
            private readonly TimeSpan _timeout;
            private readonly Action<PressTimeOutEventArgs> _onTimeout;

            public KeyButtonPressTimeoutMonitor(KeyboardMouse km, TimeSpan checkPeriod, TimeSpan timeout, Action<PressTimeOutEventArgs> onTimeout)
            {
                _km = km;
                _timeout = timeout;
                _onTimeout = onTimeout;
                _timer = new Timer(checkPeriod)
                {
                    AutoReset = true,
                };
                _timer.Elapsed += CheckForTimeout;
                _timer.Start();
            }

            private void CheckForTimeout(object source, ElapsedEventArgs e)
            {
                var now = DateTime.Now;
                foreach (var k in Enum.GetValues<HidKeyboardUsage>())
                {
                    var keyTime = _km._keyboardPressTimes[k];
                    if (keyTime == null || now - keyTime <= _timeout)
                    {
                        continue;
                    }
                    var args = new PressTimeOutEventArgs(null, k);
                    _onTimeout.Invoke(args);
                    if (args.ShouldRelease)
                    {
                        _km.KeyboardReleaseAsync(k); // Try to release, fire and forget
                    }
                }
                foreach (var k in Enum.GetValues<MouseButton>())
                {
                    var buttonTime = _km._mousePressTimes[k];
                    if (buttonTime == null || now - buttonTime <= _timeout)
                    {
                        continue;
                    }
                    var args = new PressTimeOutEventArgs(k, null);
                    _onTimeout.Invoke(args);
                    if (args.ShouldRelease)
                    {
                        _km.MouseReleaseButton(k); // Try to release, fire and forget
                    }
                }
            }

            // This is a sealed class, so we don't need to do the dispose pattern. 
            public void Dispose()
            {
                _timer.Elapsed -= CheckForTimeout;
                _timer.Stop();
                _timer.Dispose();
            }
        }
    }
}
