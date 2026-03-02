#ifndef SERIAL_HID_CPP_HPP_
#define SERIAL_HID_CPP_HPP_

#include <stddef.h>
#include <stdint.h>

extern "C" {
#include "serial_hid_core.h"
}

namespace shd {
namespace cpp {

/** @brief C++ serial adapter interface. */
class SerialIo
{
public:
    virtual ~SerialIo() {}
    virtual int available() = 0;
    virtual int read_byte() = 0;
    virtual size_t read_bytes(uint8_t* dst, size_t len) = 0;
    virtual size_t write_bytes(const uint8_t* src, size_t len) = 0;
};

/** @brief C++ keyboard adapter interface. */
class Keyboard
{
public:
    virtual ~Keyboard() {}
    virtual void press_scan_code(uint8_t key) = 0;
    virtual void release_scan_code(uint8_t key) = 0;
    virtual void release_all() = 0;
};

/** @brief C++ relative mouse adapter interface. */
class RelMouse
{
public:
    virtual ~RelMouse() {}
    virtual void move(int8_t dx, int8_t dy) = 0;
    virtual void scroll(int8_t step) = 0;
    virtual void press(uint8_t buttons) = 0;
    virtual void release(uint8_t buttons) = 0;
};

/** @brief C++ absolute mouse adapter interface. */
class AbsMouse
{
public:
    virtual ~AbsMouse() {}
    virtual void move(uint16_t x, uint16_t y) = 0;
    virtual void change_resolution(uint16_t width, uint16_t height) = 0;
};

/** @brief C++ checksum adapter interface. */
class Checksum
{
public:
    virtual ~Checksum() {}
    virtual size_t compute(const uint8_t* data,
                                size_t len,
                                uint8_t* out,
                                size_t out_cap) = 0;
    virtual bool checksum_ok(const uint8_t* data,
                             size_t len,
                             const uint8_t* expected,
                             size_t expected_len) = 0;
};

/** @brief C++ monotonic clock adapter interface. */
class Clock
{
public:
    virtual ~Clock() {}
    virtual uint32_t now_ms() = 0;
};

/** @brief C++ logger adapter interface. */
class Logger
{
public:
    virtual ~Logger() {}
    virtual void log(const char* msg) = 0;
};

/** @brief Creates C callbacks from a C++ serial implementation. */
shd_serial_io_t make_c_serial(SerialIo& serial);
/** @brief Creates C callbacks from a C++ keyboard implementation. */
shd_keyboard_t make_c_keyboard(Keyboard& keyboard);
/** @brief Creates C callbacks from a C++ relative mouse implementation. */
shd_rel_mouse_t make_c_rel_mouse(RelMouse& mouse);
/** @brief Creates C callbacks from a C++ absolute mouse implementation. */
shd_abs_mouse_t make_c_abs_mouse(AbsMouse& mouse);
/** @brief Creates C callbacks from a C++ checksum implementation. */
shd_checksum_t make_c_checksum(Checksum& checksum);
/** @brief Creates C callbacks from a C++ clock implementation. */
shd_clock_t make_c_clock(Clock& clock);
/** @brief Creates C callbacks from a C++ logger implementation. */
shd_logger_t make_c_logger(Logger& logger);

/** @brief Thin C++ wrapper around the C core. */
class Core
{
public:
    Core(const shd_core_deps_t& deps);
    Core(SerialIo& serial,
         Keyboard& keyboard,
         RelMouse& rel_mouse,
         AbsMouse& abs_mouse,
         Checksum& checksum,
         Clock& clock,
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
