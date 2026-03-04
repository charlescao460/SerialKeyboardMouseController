#ifndef SERIAL_HID_CPP_HPP_
#define SERIAL_HID_CPP_HPP_

#include <stddef.h>
#include <stdint.h>

extern "C" {
#include "serial_hid_core.h"
}

namespace shd {
namespace cpp {

class SerialIo
{
public:
    virtual ~SerialIo() {}
    virtual int available() = 0;
    virtual int read_byte() = 0;
    virtual size_t read_bytes(uint8_t* dst, size_t len) = 0;
    virtual size_t write_bytes(const uint8_t* src, size_t len) = 0;
};

class Keyboard
{
public:
    virtual ~Keyboard() {}
    virtual void press_scan_code(uint8_t key) = 0;
    virtual void release_scan_code(uint8_t key) = 0;
    virtual void release_all() = 0;
    virtual uint8_t get_lock_state() = 0;
};

class RelMouse
{
public:
    virtual ~RelMouse() {}
    virtual void move(int8_t dx, int8_t dy) = 0;
    virtual void scroll(int8_t step) = 0;
    virtual void press(uint8_t buttons) = 0;
    virtual void release(uint8_t buttons) = 0;
};

class AbsMouse
{
public:
    virtual ~AbsMouse() {}
    virtual void move(uint16_t x, uint16_t y) = 0;
    virtual void change_resolution(uint16_t width, uint16_t height) = 0;
};

/** @brief Optional CRC-8 override (for hardware CRC units). */
class Crc8
{
public:
    virtual ~Crc8() {}
    virtual uint8_t compute(const uint8_t* data, size_t len) = 0;
};

class Clock
{
public:
    virtual ~Clock() {}
    virtual uint32_t now_ms() = 0;
};

class Logger
{
public:
    virtual ~Logger() {}
    virtual void log(const char* msg) = 0;
};

class Host
{
public:
    virtual ~Host() {}
    virtual uint8_t get_status_flags() = 0;
};

class Reset
{
public:
    virtual ~Reset() {}
    virtual void reboot(uint8_t enter_bootloader) = 0;
};

shd_serial_io_t make_c_serial(SerialIo& serial);
shd_keyboard_t make_c_keyboard(Keyboard& keyboard);
shd_rel_mouse_t make_c_rel_mouse(RelMouse& mouse);
shd_abs_mouse_t make_c_abs_mouse(AbsMouse& mouse);
shd_crc8_t make_c_crc8(Crc8& crc8);
shd_clock_t make_c_clock(Clock& clock);
shd_logger_t make_c_logger(Logger& logger);
shd_host_t make_c_host(Host& host);
shd_reset_t make_c_reset(Reset& reset);

class Core
{
public:
    Core(const shd_core_deps_t& deps);
    Core(SerialIo& serial,
         Keyboard& keyboard,
         RelMouse& rel_mouse,
         AbsMouse& abs_mouse,
         Host& host,
         Reset& reset,
         Clock& clock,
         Crc8* crc8 = 0,
         Logger* logger = 0);

    shd_status_t tick();
    void set_timeout_ms(uint32_t timeout_ms);
    void set_resolution(uint16_t width, uint16_t height);
    uint16_t resolution_width() const;
    uint16_t resolution_height() const;

    shd_core_t* c_handle();
    const shd_core_t* c_handle() const;

private:
    shd_core_t core_;
    shd_core_deps_t deps_;
};

} // namespace cpp
} // namespace shd

#endif /* SERIAL_HID_CPP_HPP_ */
