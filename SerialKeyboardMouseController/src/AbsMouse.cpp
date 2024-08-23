/*

Modified based on: https://github.com/jonathanedgecombe/absmouse

Original LICENSE:

Copyright (c) 2017 Jonathan Edgecombe <jonathanedgecombe@gmail.com>

Permission to use, copy, modify, and distribute this software for any
purpose with or without fee is hereby granted, provided that the above
copyright notice and this permission notice appear in all copies.

THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES
WITH REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF
MERCHANTABILITY AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR
ANY SPECIAL, DIRECT, INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES
WHATSOEVER RESULTING FROM LOSS OF USE, DATA OR PROFITS, WHETHER IN AN
ACTION OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS ACTION, ARISING OUT OF
OR IN CONNECTION WITH THE USE OR PERFORMANCE OF THIS SOFTWARE.

*/

#include "AbsMouse.h"
#include "debug_print.h"

#if defined(_USING_HID)

#define MOUSE_LEFT 0x01
#define MOUSE_RIGHT 0x02
#define MOUSE_MIDDLE 0x04

static const uint8_t HID_REPORT_DESCRIPTOR[] PROGMEM = {
    0x05, 0x01,        // Usage Page (Generic Desktop Ctrls)
    0x09, 0x02,        // Usage (Mouse)
    0xA1, 0x01,        // Collection (Application)
    0x09, 0x01,        //   Usage (Pointer)
    0xA1, 0x00,        //   Collection (Physical)
    0x85, 0x01,        //     Report ID (1)
    0x05, 0x09,        //     Usage Page (Button)
    0x19, 0x01,        //     Usage Minimum (0x01)
    0x29, 0x03,        //     Usage Maximum (0x03)
    0x15, 0x00,        //     Logical Minimum (0)
    0x25, 0x01,        //     Logical Maximum (1)
    0x95, 0x03,        //     Report Count (3)
    0x75, 0x01,        //     Report Size (1)
    0x81, 0x02,        //     Input (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position)
    0x95, 0x01,        //     Report Count (1)
    0x75, 0x05,        //     Report Size (5)
    0x81, 0x03,        //     Input (Const,Var,Abs,No Wrap,Linear,Preferred State,No Null Position)
    0x05, 0x01,        //     Usage Page (Generic Desktop Ctrls)
    0x09, 0x30,        //     Usage (X)
    0x09, 0x31,        //     Usage (Y)
    0x16, 0x00, 0x00,  //     Logical Minimum (0)
    0x26, 0xFF, 0x7F,  //     Logical Maximum (32767)
    0x36, 0x00, 0x00,  //     Physical Minimum (0)
    0x46, 0xFF, 0x7F,  //     Physical Maximum (32767)
    0x75, 0x10,        //     Report Size (16)
    0x95, 0x02,        //     Report Count (2)
    0x81, 0x02,        //     Input (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position)
    0x09, 0x38,        //     Usage (Wheel)
    0x15, 0x81,        //     Logical Minimum (-127)
    0x25, 0x7F,        //     Logical Maximum (127)
    0x35, 0x81,        //     Physical Minimum (-127)
    0x45, 0x7F,        //     Physical Maximum (127)
    0x75, 0x08,        //     Report Size (8)
    0x95, 0x01,        //     Report Count (1)
    0x81, 0x06,        //     Input (Data,Var,Rel)
    0xC0,              //   End Collection (Physical)
    0xC0               // End Collection (Application)
};

AbsMouse_::AbsMouse_(void) : _buttons(0), _scroll(0), _x(0), _y(0), _width(1920), _height(1080), _autoReport(false)
{
    static HIDSubDescriptor descriptorNode(HID_REPORT_DESCRIPTOR, sizeof(HID_REPORT_DESCRIPTOR));
    HID().AppendDescriptor(&descriptorNode);
}

void AbsMouse_::init(uint16_t width, uint16_t height, bool autoReport)
{
    _width = width;
    _height = height;
    _autoReport = autoReport;
}

void AbsMouse_::report(void)
{
    uint8_t buffer[6];
    buffer[0] = _buttons;
    buffer[1] = _x & 0xFF;
    buffer[2] = (_x >> 8) & 0xFF;
    buffer[3] = _y & 0xFF;
    buffer[4] = (_y >> 8) & 0xFF;
    buffer[5] = _scroll;
    HID().SendReport(1, buffer, 6);
    _scroll = 0;
}

void AbsMouse_::move(uint16_t x, uint16_t y)
{
    if(_width != 32767 || _height != 32767)
    {
        _x = (uint16_t)((32767l * ((uint32_t)x)) / _width);
        _y = (uint16_t)((32767l * ((uint32_t)y)) / _height);
    } else
    {
        _x = x;
        _y = y;
    }

    if (_autoReport)
    {
        report();
    }
}

void AbsMouse_::scroll(int8_t wheel)
{
    _scroll = wheel;
    if (_autoReport)
    {
        report();
    }
}

void AbsMouse_::press(uint8_t button)
{
    _buttons |= button;

    if (_autoReport)
    {
        report();
    }
}

void AbsMouse_::release(uint8_t button)
{
    _buttons &= ~button;

    if (_autoReport)
    {
        report();
    }
}

AbsMouse_ AbsMouse;

#endif
