#include "AbsMouse.h"

#if defined(_USING_HID)

static const uint8_t kAbsMouseReportId = 1u;
static const uint8_t kRelMouseReportId = 3u;

static const uint8_t kAbsMouseDescriptor[] PROGMEM = {
    // Relative mouse application report.
    0x05, 0x01,
    0x09, 0x02,
    0xA1, 0x01,
    0x09, 0x01,
    0xA1, 0x00,
    0x85, kRelMouseReportId,
    0x05, 0x09,
    0x19, 0x01,
    0x29, 0x03,
    0x15, 0x00,
    0x25, 0x01,
    0x75, 0x01,
    0x95, 0x03,
    0x81, 0x02,
    0x75, 0x05,
    0x95, 0x01,
    0x81, 0x03,
    0x05, 0x01,
    0x09, 0x30,
    0x09, 0x31,
    0x09, 0x38,
    0x15, 0x81,
    0x25, 0x7F,
    0x75, 0x08,
    0x95, 0x03,
    0x81, 0x06,
    0xC0,
    0xC0,

    // Absolute mouse application report.
    0x05, 0x01,
    0x09, 0x02,
    0xA1, 0x01,
    0x09, 0x01,
    0xA1, 0x00,
    0x85, kAbsMouseReportId,
    0x05, 0x09,
    0x19, 0x01,
    0x29, 0x03,
    0x15, 0x00,
    0x25, 0x01,
    0x75, 0x01,
    0x95, 0x03,
    0x81, 0x02,
    0x75, 0x05,
    0x95, 0x01,
    0x81, 0x03,
    0x05, 0x01,
    0x09, 0x30,
    0x09, 0x31,
    0x16, 0x00, 0x00,
    0x26, 0xFF, 0x7F,
    0x75, 0x10,
    0x95, 0x02,
    0x81, 0x02,
    0x09, 0x38,
    0x15, 0x81,
    0x25, 0x7F,
    0x75, 0x08,
    0x95, 0x01,
    0x81, 0x06,
    0xC0,
    0xC0};

static int8_t clamp_rel(int value)
{
    if (value > 127)
    {
        return 127;
    }
    if (value < -127)
    {
        return -127;
    }
    return (int8_t)value;
}

AbsMouse_::AbsMouse_()
    : buttons_(0u)
    , width_(32767u)
    , height_(32767u)
    , auto_report_(false)
{
    static HIDSubDescriptor descriptor(kAbsMouseDescriptor, sizeof(kAbsMouseDescriptor));
    HID().AppendDescriptor(&descriptor);
}

void AbsMouse_::init(uint16_t width, uint16_t height, bool autoReport)
{
    width_ = width;
    height_ = height;
    auto_report_ = autoReport;
}

void AbsMouse_::report()
{
    // Emit a neutral relative report to refresh button state.
    send_relative_report(0, 0, 0);
}

void AbsMouse_::send_absolute_report(uint16_t x, uint16_t y, int8_t wheel)
{
    uint8_t payload[6];
    payload[0] = buttons_;
    payload[1] = (uint8_t)(x & 0xFFu);
    payload[2] = (uint8_t)((x >> 8u) & 0xFFu);
    payload[3] = (uint8_t)(y & 0xFFu);
    payload[4] = (uint8_t)((y >> 8u) & 0xFFu);
    payload[5] = (uint8_t)wheel;
    HID().SendReport(kAbsMouseReportId, payload, sizeof(payload));
}

void AbsMouse_::send_relative_report(int8_t dx, int8_t dy, int8_t wheel)
{
    uint8_t payload[4];
    payload[0] = buttons_;
    payload[1] = (uint8_t)dx;
    payload[2] = (uint8_t)dy;
    payload[3] = (uint8_t)wheel;
    HID().SendReport(kRelMouseReportId, payload, sizeof(payload));
}

void AbsMouse_::move(uint16_t x, uint16_t y)
{
    uint16_t abs_x = 0u;
    uint16_t abs_y = 0u;

    if (width_ != 32767u || height_ != 32767u)
    {
        abs_x = (uint16_t)((32767ul * (uint32_t)x) / width_);
        abs_y = (uint16_t)((32767ul * (uint32_t)y) / height_);
    }
    else
    {
        abs_x = x;
        abs_y = y;
    }

    if (auto_report_)
    {
        send_absolute_report(abs_x, abs_y, 0);
    }
}

void AbsMouse_::moveRelative(int8_t dx, int8_t dy)
{
    if (auto_report_)
    {
        // Relative path reports zero absolute coordinates by not sending absolute report data.
        send_relative_report(clamp_rel(dx), clamp_rel(dy), 0);
    }
}

void AbsMouse_::scroll(int8_t wheel)
{
    if (auto_report_)
    {
        send_relative_report(0, 0, clamp_rel(wheel));
    }
}

void AbsMouse_::press(uint8_t buttons)
{
    buttons_ |= buttons;

    if (auto_report_)
    {
        report();
    }
}

void AbsMouse_::release(uint8_t buttons)
{
    buttons_ = (uint8_t)(buttons_ & (uint8_t)(~buttons));

    if (auto_report_)
    {
        report();
    }
}

AbsMouse_ AbsMouse;

#endif
