#ifndef ABSMOUSE_h
#define ABSMOUSE_h

#include "HID.h"

#if !defined(_USING_HID)

#warning "AbsMouse not compatible with this device and/or firmware"

#else

class AbsMouse_
{
public:
    AbsMouse_();

    void init(uint16_t width = 32767u, uint16_t height = 32767u, bool autoReport = true);
    void report();

    void move(uint16_t x, uint16_t y);
    void moveRelative(int8_t dx, int8_t dy);
    void scroll(int8_t wheel);
    void press(uint8_t buttons);
    void release(uint8_t buttons);

private:
    void send_absolute_report(uint16_t x, uint16_t y, int8_t wheel);
    void send_relative_report(int8_t dx, int8_t dy, int8_t wheel);

    uint8_t buttons_;
    uint16_t width_;
    uint16_t height_;
    bool auto_report_;
};

extern AbsMouse_ AbsMouse;

#endif
#endif
