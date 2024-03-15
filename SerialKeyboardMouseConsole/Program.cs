using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using CommandLine;
using SerialKeyboardMouse.Serial;
using SerialKeyboardMouse;

namespace SerialKeyboardMouseConsole
{
    public class Program
    {
        private class Options
        {
            [Option(shortName: 'c', longName: "com", Required = true, HelpText = "COM ports to open. E.g. COM1")]
            public string ComPort { get; set; }

            [Option(shortName: 'f', longName: "flow-control", Required = false, Default = false, HelpText = "Whether to enable Hardware Flow Control (RTS/CTS).")]
            public bool HardwareFlowControl { get; set; }

            [Option(shortName: 'w', longName: "width", Required = false, Default = 1920, HelpText = "Width of absolute mouse")]
            public int Width { get; set; }

            [Option(shortName: 'h', longName: "height", Required = false, Default = 1080, HelpText = "Height of absolute mouse")]
            public int Height { get; set; }
        }

        static int Main(string[] args)
        {
            int ret = CommandLine.Parser.Default.ParseArguments<Options>(args).MapResult(RunAndReturn, OnParseError);
            return ret;
        }

        private static int OnParseError(IEnumerable<Error> errors)
        {
            foreach (var error in errors)
            {
                Console.Error.WriteLine(Enum.GetName(typeof(ErrorType), error.Tag));
            }
            return -1;
        }


        private static readonly Stopwatch MouseThrottleStopwatch = new Stopwatch();
        private static readonly Stopwatch GeneralPurposeStopwatch = new Stopwatch();
        private static KeyboardMouse _keyboardMouse;
        private static Form _form;
        private static Options _options;

        private static Form CreateForm(int width, int height)
        {
            Form form = new Form
            {
                Width = (int)(width / 1.5),
                Height = (int)(height / 1.5),
                FormBorderStyle = FormBorderStyle.FixedSingle,
                MaximizeBox = false,
                MinimizeBox = false,
                Text = "Operations in this windows will be transferred to target."
            };
            return form;
        }

        private static async void MouseMoveEventHandler(object source, MouseEventArgs e)
        {
            if (e.X < 0 || e.Y < 0 || e.X > _form.Width || e.Y > _form.Height)
            {
                return;
            }
            if (MouseThrottleStopwatch.ElapsedMilliseconds < (1000.0 / 125.0))
            {
                return;
            }

            double relativeX = (double)e.X / _form.Width * 1.2;
            double relativeY = (double)e.Y / _form.Height * 1.2;
            relativeX = relativeX > 1 ? 1 : relativeX;
            relativeY = relativeY > 1 ? 1 : relativeY;
            int x = (int)(relativeX * _options.Width);
            int y = (int)(relativeY * _options.Height);
            x = x == 0 ? 1 : x;
            y = y == 0 ? 1 : y;

            GeneralPurposeStopwatch.Restart();
            try
            {
                await _keyboardMouse.MoveMouseToCoordinate(x, y);
            }
            catch (SerialDeviceException ex)
            {
                Console.WriteLine($"Serial device overload:{ex.Message}, input: {x},{y}.");
                return;
            }
            GeneralPurposeStopwatch.Stop();

            Console.WriteLine($"Moved mouse to {x},{y}. Timing: {GeneralPurposeStopwatch.ElapsedMicrosecond()} us.");

            MouseThrottleStopwatch.Restart();
        }

        private static void MousePressReleaseHelper(MouseEventArgs e, bool isPress)
        {
            SerialSymbols.MouseButton button;
            switch (e.Button)
            {
                case MouseButtons.Right:
                    button = SerialSymbols.MouseButton.Right;
                    break;
                case MouseButtons.Left:
                    button = SerialSymbols.MouseButton.Left;
                    break;
                case MouseButtons.Middle:
                    button = SerialSymbols.MouseButton.Middle;
                    break;
                default:
                    return;
            }

            string buttonName = Enum.GetName(button);
            GeneralPurposeStopwatch.Restart();
            try
            {
                (isPress ? _keyboardMouse.MousePressButton(button) : _keyboardMouse.MouseReleaseButton(button)).GetAwaiter().GetResult();
            }
            catch (SerialDeviceException ex)
            {
                Console.WriteLine($"Serial device overload:{ex.Message}, input: {buttonName}.");
                return;
            }
            GeneralPurposeStopwatch.Stop();
            Console.Write(isPress ? "Press" : "Release");
            Console.WriteLine($" mouse button {buttonName}. Timing: {GeneralPurposeStopwatch.ElapsedMicrosecond()} us.");
        }

        private static void KeyboardPressReleaseHelper(KeyEventArgs e, bool isPress)
        {
            uint ps2ScanCode = MapVirtualKeyA((uint)e.KeyValue, 0);
            byte hidScanCode = HidHelper.GetHidUsageFromPs2Set1(ps2ScanCode);
            if (isPress && _keyboardMouse.KeyboardIsPressed(hidScanCode))
            {
                return;
            }
            if (hidScanCode == 0)
            {
                Console.WriteLine($"Cannot find corresponding HID code of " +
                                  $"KeyValue=0x{e.KeyValue:X2}, PS/2={ps2ScanCode:X2}!");
                return;
            }
            GeneralPurposeStopwatch.Restart();
            try
            {
                (isPress ? _keyboardMouse.KeyboardPress(hidScanCode) : _keyboardMouse.KeyboardRelease(hidScanCode)).GetAwaiter().GetResult();
            }
            catch (SerialDeviceException ex)
            {
                Console.WriteLine($"Serial device overload:{ex.Message}, input: 0x{hidScanCode:X2}.");
                return;
            }
            GeneralPurposeStopwatch.Stop();
            Console.Write(isPress ? "Press" : "Release");
            Console.WriteLine($" keyboard HID=0x{hidScanCode:X2}, PS/2=0x{ps2ScanCode:X2}, Win32VK=0x{e.KeyValue:X2}. Timing: {GeneralPurposeStopwatch.ElapsedMicrosecond()} us.");
        }

        private static void MouseScrollEventHandler(object sender, MouseEventArgs e)
        {
            sbyte value = e.Delta > 0 ? (sbyte)1 : (sbyte)-1;
            GeneralPurposeStopwatch.Restart();
            try
            {
                _keyboardMouse.MouseScroll(value).GetAwaiter().GetResult(); ;
            }
            catch (SerialDeviceException ex)
            {
                Console.WriteLine($"Serial device overload:{ex.Message}, input: {value}.");
                return;
            }
            GeneralPurposeStopwatch.Stop();
            Console.WriteLine($"Mouse scroll {value}. Timing: {GeneralPurposeStopwatch.ElapsedMicrosecond()} us.");
        }

        [DllImport("kernel32")]
        static extern bool AllocConsole();

        /// <see cref="https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-mapvirtualkeya"/>
        [DllImport("user32")]
        static extern uint MapVirtualKeyA(uint uCode, uint uMapType);

        /// <summary>
        /// Main logic here
        /// </summary>
        private static int RunAndReturn(Options options)
        {
            AllocConsole();
            ISerialAdaptor serial;
            try
            {
                serial = new DotNetSerialAdaptor(options.ComPort, options.HardwareFlowControl);
            }
            catch (Exception)
            {
                Console.Error.WriteLine($"Cannot open port {options.ComPort}!");
                return -1;
            }

            _keyboardMouse = new KeyboardMouse(serial);
            _keyboardMouse.SetMouseResolution(options.Width, options.Height);

            Program._options = options;
            _form = CreateForm(options.Width, options.Height);

            _form.FormClosed += (s, e) =>
            {
                _keyboardMouse.Dispose();
                System.Environment.Exit(0);
            };

            Console.CancelKeyPress += (s, e) =>
            {
                _keyboardMouse.Dispose();
                _form.Dispose();
                System.Environment.Exit(0);
            };

            _form.MouseMove += MouseMoveEventHandler;
            _form.MouseWheel += MouseScrollEventHandler;
            _form.MouseDown += (s, e) => { MousePressReleaseHelper(e, true); };
            _form.MouseUp += (s, e) => { MousePressReleaseHelper(e, false); };
            _form.KeyDown += (s, e) => { KeyboardPressReleaseHelper(e, true); };
            _form.PreviewKeyDown += (s, e) => { e.IsInputKey = true; };
            _form.KeyUp += (s, e) => { KeyboardPressReleaseHelper(e, false); };

            MouseThrottleStopwatch.Start();
            _form.ShowDialog();

            return 0;
        }
    }

    public static class StopwatchExtension
    {
        public static long ElapsedMicrosecond(this Stopwatch stopwatch)
        {
            return (long)(1000000.0 * (stopwatch.ElapsedTicks / (double)Stopwatch.Frequency));
        }
    }
}
