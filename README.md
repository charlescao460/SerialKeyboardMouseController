# SerialKeyboardMouseController
A software-controlled hardware USB HID keyboard &amp; mouse for everyone

## Purpose
This is an Arduino project with a .NET Core library allowed you to control a **real** hardware mouse &amp; keyboard without being detected by any anti-cheat or protection software. Since this system is using a real USB HID device, it’s very hard to distinguish it from normal mouses and keyboards.

![](https://github.com/charlescao460/SerialKeyboardMouseController/blob/main/Pictures/TypicalApplication.png)

## Build Hardware
**Materials:**

* An Arduino device with [`HID.h`](https://www.arduino.cc/en/Reference/HID) implemented. ATmega32U4 boards like Micro and Leonardo, or SAMD board like MKR ZERO would work.
* An UART-To-USB bridge, like CP2102N, FT232R, CH340, etc..

**Step:**
1. Make sure your Arduino is tolerant with your UART-USB bridge logic level (5V or 3.3V). :warning:
2. Connect Arduino **GND** -> UART-USB bridge **GND** (VCC is not necessarily connected)
3. Connect Arduino **TX** -> UART-USB bridge **RX**
4. Connect Arduino **RX** -> UART-USB bridge **TX**


## Notes
Some protection software will check USB VID and PID, to avoid being detected, consider changing them in Arduino’s [bootloader](https://github.com/arduino/ArduinoCore-avr/tree/master/bootloaders). Most operation systems will have a general driver for HID devices, so changing VID & PID won’t involve driver issue.

Also, be aware of [Keystroke dynamics](https://en.wikipedia.org/wiki/Keystroke_dynamics). Researchers have proven that each individual has a unique pattern of typing. So,  theoretically a machine learning pattern-recognition algorithm can detect suspicious keyboard operation. Try to add some random delays between each HID report. If you send commands too fast, it will definitely trigger the anti-bot protection. 

## License
**GNU Lesser General Public License** (LGPL)

Because [Keyboard.cpp](https://github.com/charlescao460/SerialKeyboardMouseController/blob/main/SerialKeyboardMouseController/Keyboard.cpp) and [Keyboard.h](https://github.com/charlescao460/SerialKeyboardMouseController/blob/main/SerialKeyboardMouseController/Keyboard.h) are based on Arduino's [Keyboard](https://github.com/arduino-libraries/Keyboard) library. If you can get rid of it by writing your own library, or if you don't need Arduino sketch, feel free to use this MIT Licesne:
```text
MIT License

Copyright (c) 2021 Charles Cao

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
SOFTWARE.
```
