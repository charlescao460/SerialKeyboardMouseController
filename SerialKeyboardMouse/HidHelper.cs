/*
    Modified from Microsoft: https://github.com/microsoft/BluetoothLEExplorer/blob/master/BluetoothLEExplorer/BluetoothLEExplorer/Models/HidHelper.cs
    Original License:

    MIT License

    Bluetooth LE Explorer 
    Copyright (c) Microsoft Corporation. All rights reserved.

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in all
    copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE
*/

using System;
using static SerialKeyboardMouse.HidHelper;
using System.Collections.Generic;

namespace SerialKeyboardMouse
{

    public class HidHelper
    {
        private static readonly Dictionary<int, HidKeyboardUsage> WinFormsToHidMap = new()
        {
            // Alphanumeric keys
            { 65, HidKeyboardUsage.A },
            { 66, HidKeyboardUsage.B },
            { 67, HidKeyboardUsage.C },
            { 68, HidKeyboardUsage.D },
            { 69, HidKeyboardUsage.E },
            { 70, HidKeyboardUsage.F },
            { 71, HidKeyboardUsage.G },
            { 72, HidKeyboardUsage.H },
            { 73, HidKeyboardUsage.I },
            { 74, HidKeyboardUsage.J },
            { 75, HidKeyboardUsage.K },
            { 76, HidKeyboardUsage.L },
            { 77, HidKeyboardUsage.M },
            { 78, HidKeyboardUsage.N },
            { 79, HidKeyboardUsage.O },
            { 80, HidKeyboardUsage.P },
            { 81, HidKeyboardUsage.Q },
            { 82, HidKeyboardUsage.R },
            { 83, HidKeyboardUsage.S },
            { 84, HidKeyboardUsage.T },
            { 85, HidKeyboardUsage.U },
            { 86, HidKeyboardUsage.V },
            { 87, HidKeyboardUsage.W },
            { 88, HidKeyboardUsage.X },
            { 89, HidKeyboardUsage.Y },
            { 90, HidKeyboardUsage.Z },
            { 48, HidKeyboardUsage.Num0 },
            { 49, HidKeyboardUsage.Num1 },
            { 50, HidKeyboardUsage.Num2 },
            { 51, HidKeyboardUsage.Num3 },
            { 52, HidKeyboardUsage.Num4 },
            { 53, HidKeyboardUsage.Num5 },
            { 54, HidKeyboardUsage.Num6 },
            { 55, HidKeyboardUsage.Num7 },
            { 56, HidKeyboardUsage.Num8 },
            { 57, HidKeyboardUsage.Num9 },

            // Punctuation keys
            { 186, HidKeyboardUsage.Semicolon },
            { 187, HidKeyboardUsage.Equals },
            { 188, HidKeyboardUsage.Comma },
            { 189, HidKeyboardUsage.Minus },
            { 190, HidKeyboardUsage.Period },
            { 191, HidKeyboardUsage.Slash },
            { 192, HidKeyboardUsage.GraveAccent },
            { 219, HidKeyboardUsage.LeftBracket },
            { 220, HidKeyboardUsage.Backslash },
            { 221, HidKeyboardUsage.RightBracket },
            { 222, HidKeyboardUsage.Quote },

            // Function keys
            { 112, HidKeyboardUsage.F1 },
            { 113, HidKeyboardUsage.F2 },
            { 114, HidKeyboardUsage.F3 },
            { 115, HidKeyboardUsage.F4 },
            { 116, HidKeyboardUsage.F5 },
            { 117, HidKeyboardUsage.F6 },
            { 118, HidKeyboardUsage.F7 },
            { 119, HidKeyboardUsage.F8 },
            { 120, HidKeyboardUsage.F9 },
            { 121, HidKeyboardUsage.F10 },
            { 122, HidKeyboardUsage.F11 },
            { 123, HidKeyboardUsage.F12 },

            // Special keys
            { 8, HidKeyboardUsage.Backspace },
            { 9, HidKeyboardUsage.Tab },
            { 13, HidKeyboardUsage.Enter },
            { 16, HidKeyboardUsage.LeftShift },
            { 17, HidKeyboardUsage.LeftControl },
            { 18, HidKeyboardUsage.LeftAlt },
            { 19, HidKeyboardUsage.Pause },
            { 20, HidKeyboardUsage.CapsLock },
            { 27, HidKeyboardUsage.Escape },
            { 32, HidKeyboardUsage.Space },
            { 33, HidKeyboardUsage.PageUp },
            { 34, HidKeyboardUsage.PageDown },
            { 35, HidKeyboardUsage.End },
            { 36, HidKeyboardUsage.Home },
            { 37, HidKeyboardUsage.LeftArrow },
            { 38, HidKeyboardUsage.UpArrow },
            { 39, HidKeyboardUsage.RightArrow },
            { 40, HidKeyboardUsage.DownArrow },
            { 45, HidKeyboardUsage.Insert },
            { 46, HidKeyboardUsage.DeleteForward },

            // Numpad keys
            { 96, HidKeyboardUsage.Keypad0 },
            { 97, HidKeyboardUsage.Keypad1 },
            { 98, HidKeyboardUsage.Keypad2 },
            { 99, HidKeyboardUsage.Keypad3 },
            { 100, HidKeyboardUsage.Keypad4 },
            { 101, HidKeyboardUsage.Keypad5 },
            { 102, HidKeyboardUsage.Keypad6 },
            { 103, HidKeyboardUsage.Keypad7 },
            { 104, HidKeyboardUsage.Keypad8 },
            { 105, HidKeyboardUsage.Keypad9 },
            { 106, HidKeyboardUsage.KeypadMultiply },
            { 107, HidKeyboardUsage.KeypadPlus },
            { 109, HidKeyboardUsage.KeypadMinus },
            { 110, HidKeyboardUsage.KeypadPeriod },
            { 111, HidKeyboardUsage.KeypadDivide },
            { 144, HidKeyboardUsage.NumLock },
            { 145, HidKeyboardUsage.ScrollLock },

            // Modifier keys
            { 160, HidKeyboardUsage.LeftShift },
            { 161, HidKeyboardUsage.RightShift },
            { 162, HidKeyboardUsage.LeftControl },
            { 163, HidKeyboardUsage.RightControl },
            { 164, HidKeyboardUsage.LeftAlt },
            { 165, HidKeyboardUsage.RightAlt },
        };

        private static readonly Dictionary<uint, HidKeyboardUsage> Ps2Set1ToHidMap = new()
        {
            // Alphanumeric Keys
            { 0x1E, HidKeyboardUsage.A },
            { 0x30, HidKeyboardUsage.B },
            { 0x2E, HidKeyboardUsage.C },
            { 0x20, HidKeyboardUsage.D },
            { 0x12, HidKeyboardUsage.E },
            { 0x21, HidKeyboardUsage.F },
            { 0x22, HidKeyboardUsage.G },
            { 0x23, HidKeyboardUsage.H },
            { 0x17, HidKeyboardUsage.I },
            { 0x24, HidKeyboardUsage.J },
            { 0x25, HidKeyboardUsage.K },
            { 0x26, HidKeyboardUsage.L },
            { 0x32, HidKeyboardUsage.M },
            { 0x31, HidKeyboardUsage.N },
            { 0x18, HidKeyboardUsage.O },
            { 0x19, HidKeyboardUsage.P },
            { 0x10, HidKeyboardUsage.Q },
            { 0x13, HidKeyboardUsage.R },
            { 0x1F, HidKeyboardUsage.S },
            { 0x14, HidKeyboardUsage.T },
            { 0x16, HidKeyboardUsage.U },
            { 0x2F, HidKeyboardUsage.V },
            { 0x11, HidKeyboardUsage.W },
            { 0x2D, HidKeyboardUsage.X },
            { 0x15, HidKeyboardUsage.Y },
            { 0x2C, HidKeyboardUsage.Z },

            // Numeric Keys
            { 0x0B, HidKeyboardUsage.Num0 },
            { 0x02, HidKeyboardUsage.Num1 },
            { 0x03, HidKeyboardUsage.Num2 },
            { 0x04, HidKeyboardUsage.Num3 },
            { 0x05, HidKeyboardUsage.Num4 },
            { 0x06, HidKeyboardUsage.Num5 },
            { 0x07, HidKeyboardUsage.Num6 },
            { 0x08, HidKeyboardUsage.Num7 },
            { 0x09, HidKeyboardUsage.Num8 },
            { 0x0A, HidKeyboardUsage.Num9 },

            // Punctuation Keys
            { 0x29, HidKeyboardUsage.GraveAccent }, // ` and ~
            { 0x0C, HidKeyboardUsage.Minus }, // - and _
            { 0x0D, HidKeyboardUsage.Equals }, // = and +
            { 0x1A, HidKeyboardUsage.LeftBracket }, // [ and {
            { 0x1B, HidKeyboardUsage.RightBracket }, // ] and }
            { 0x2B, HidKeyboardUsage.Backslash }, // \ and |
            { 0x27, HidKeyboardUsage.Semicolon }, // ; and :
            { 0x28, HidKeyboardUsage.Quote }, // ' and "
            { 0x33, HidKeyboardUsage.Comma }, // , and <
            { 0x34, HidKeyboardUsage.Period }, // . and >
            { 0x35, HidKeyboardUsage.Slash }, // / and ?

            // Function Keys
            { 0x3B, HidKeyboardUsage.F1 },
            { 0x3C, HidKeyboardUsage.F2 },
            { 0x3D, HidKeyboardUsage.F3 },
            { 0x3E, HidKeyboardUsage.F4 },
            { 0x3F, HidKeyboardUsage.F5 },
            { 0x40, HidKeyboardUsage.F6 },
            { 0x41, HidKeyboardUsage.F7 },
            { 0x42, HidKeyboardUsage.F8 },
            { 0x43, HidKeyboardUsage.F9 },
            { 0x44, HidKeyboardUsage.F10 },
            { 0x57, HidKeyboardUsage.F11 },
            { 0x58, HidKeyboardUsage.F12 },

            // Control Keys
            { 0x1C, HidKeyboardUsage.Enter },
            { 0x1D, HidKeyboardUsage.LeftControl },
            { 0x2A, HidKeyboardUsage.LeftShift },
            { 0xE01D, HidKeyboardUsage.RightControl },
            { 0x36, HidKeyboardUsage.RightShift },
            { 0x38, HidKeyboardUsage.LeftAlt },
            { 0xE038, HidKeyboardUsage.RightAlt },
            { 0x39, HidKeyboardUsage.Space },
            { 0x0E, HidKeyboardUsage.Backspace },
            { 0x0F, HidKeyboardUsage.Tab },
            { 0x3A, HidKeyboardUsage.CapsLock },
            { 0x45, HidKeyboardUsage.NumLock },
            { 0x46, HidKeyboardUsage.ScrollLock },
            { 0x01, HidKeyboardUsage.Escape },
            { 0x54, HidKeyboardUsage.PrintScreen },

            // Numpad Keys
            { 0x47, HidKeyboardUsage.Keypad7 },
            { 0x48, HidKeyboardUsage.Keypad8 },
            { 0x49, HidKeyboardUsage.Keypad9 },
            { 0x4B, HidKeyboardUsage.Keypad4 },
            { 0x4C, HidKeyboardUsage.Keypad5 },
            { 0x4D, HidKeyboardUsage.Keypad6 },
            { 0x4F, HidKeyboardUsage.Keypad1 },
            { 0x50, HidKeyboardUsage.Keypad2 },
            { 0x51, HidKeyboardUsage.Keypad3 },
            { 0x52, HidKeyboardUsage.Keypad0 },
            { 0x53, HidKeyboardUsage.KeypadPeriod },
            { 0xE035, HidKeyboardUsage.KeypadDivide }, // Keypad Divide
            { 0x37, HidKeyboardUsage.KeypadMultiply }, // Keypad Multiply
            { 0x4A, HidKeyboardUsage.KeypadMinus }, // Keypad Minus
            { 0x4E, HidKeyboardUsage.KeypadPlus }, // Keypad Plus

            // Navigation keys and others
            { 0xE01C, HidKeyboardUsage.KeypadEnter }, // Keypad Enter
            { 0xE048, HidKeyboardUsage.UpArrow }, // Up Arrow
            { 0xE050, HidKeyboardUsage.DownArrow }, // Down Arrow
            { 0xE04B, HidKeyboardUsage.LeftArrow }, // Left Arrow
            { 0xE04D, HidKeyboardUsage.RightArrow }, // Right Arrow
            { 0xE049, HidKeyboardUsage.PageUp }, // Page Up
            { 0xE051, HidKeyboardUsage.PageDown }, // Page Down
            { 0xE04F, HidKeyboardUsage.End }, // End
            { 0xE052, HidKeyboardUsage.Insert }, // Insert
            { 0xE053, HidKeyboardUsage.DeleteForward }, // Delete
            { 0xE047, HidKeyboardUsage.Home }, // Home
            { 0xE05B, HidKeyboardUsage.LeftWindows },
            { 0xE05C, HidKeyboardUsage.RightWindows },
            { 0xE05D, HidKeyboardUsage.Application },
        };

        /// <summary>
        /// Return HID usage id from System.Windows.Forms.Keys
        /// </summary>
        /// <param name="winFormKeys">System.Windows.Forms.Keys Enum</param>
        /// <returns>HID usage id</returns>
        public static HidKeyboardUsage GetHidUsageFromWinforms(int winFormKeys)
        {
            return WinFormsToHidMap.GetValueOrDefault(winFormKeys, HidKeyboardUsage.Undefined);
        }

        /// <summary>
        /// Return HID usage id from PS/2 Set 1 scan code
        /// </summary>
        /// <param name="ps2Set1ScanCode">PS/2 Set 1 scan code</param>
        /// <returns>HID usage id</returns>
        public static HidKeyboardUsage GetHidUsageFromPs2Set1(uint ps2Set1ScanCode)
        {
            return Ps2Set1ToHidMap.GetValueOrDefault(ps2Set1ScanCode, HidKeyboardUsage.Undefined);
        }
    }
}
