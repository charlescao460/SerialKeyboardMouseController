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


#ifndef ABSMOUSE_h
#define ABSMOUSE_h

#include "HID.h"

#if !defined(_USING_HID)

#warning "AbsMouse not compatible with this device and/or firmware"

#else

#define MOUSE_LEFT 0x01
#define MOUSE_RIGHT 0x02
#define MOUSE_MIDDLE 0x04

class AbsMouse_
{
private:
    uint8_t _buttons;
    int8_t _scroll;
    uint16_t _x;
    uint16_t _y;
    uint32_t _width;
    uint32_t _height;
    bool _autoReport;

public:
    AbsMouse_(void);
    void init(uint16_t width = 32767, uint16_t height = 32767, bool autoReport = true);
    void report(void) const;
    void move(uint16_t x, uint16_t y);
    void scroll(int8_t wheel);
    void press(uint8_t b = MOUSE_LEFT);
    void release(uint8_t b = MOUSE_LEFT);
};
extern AbsMouse_ AbsMouse;

#endif
#endif
