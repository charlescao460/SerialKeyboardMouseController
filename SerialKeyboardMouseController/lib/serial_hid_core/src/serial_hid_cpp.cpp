#include "serial_hid_cpp.hpp"

namespace shd {
namespace cpp {

namespace {

int serial_available(void* ctx)
{
    return static_cast<SerialIo*>(ctx)->available();
}

int serial_read_byte(void* ctx)
{
    return static_cast<SerialIo*>(ctx)->read_byte();
}

size_t serial_read_bytes(void* ctx, uint8_t* dst, size_t len)
{
    return static_cast<SerialIo*>(ctx)->read_bytes(dst, len);
}

size_t serial_write_bytes(void* ctx, const uint8_t* src, size_t len)
{
    return static_cast<SerialIo*>(ctx)->write_bytes(src, len);
}

void keyboard_press(void* ctx, uint8_t key)
{
    static_cast<Keyboard*>(ctx)->press_scan_code(key);
}

void keyboard_release(void* ctx, uint8_t key)
{
    static_cast<Keyboard*>(ctx)->release_scan_code(key);
}

void keyboard_release_all(void* ctx)
{
    static_cast<Keyboard*>(ctx)->release_all();
}

void rel_mouse_move(void* ctx, int8_t dx, int8_t dy)
{
    static_cast<RelMouse*>(ctx)->move(dx, dy);
}

void rel_mouse_scroll(void* ctx, int8_t step)
{
    static_cast<RelMouse*>(ctx)->scroll(step);
}

void rel_mouse_press(void* ctx, uint8_t buttons)
{
    static_cast<RelMouse*>(ctx)->press(buttons);
}

void rel_mouse_release(void* ctx, uint8_t buttons)
{
    static_cast<RelMouse*>(ctx)->release(buttons);
}

void abs_mouse_move(void* ctx, uint16_t x, uint16_t y)
{
    static_cast<AbsMouse*>(ctx)->move(x, y);
}

void abs_mouse_change_resolution(void* ctx, uint16_t width, uint16_t height)
{
    static_cast<AbsMouse*>(ctx)->change_resolution(width, height);
}

size_t checksum_compute(void* ctx,
                        const uint8_t* data,
                        size_t len,
                        uint8_t* out,
                        size_t out_cap)
{
    return static_cast<Checksum*>(ctx)->compute(data, len, out, out_cap);
}

bool checksum_ok(void* ctx,
                 const uint8_t* data,
                 size_t len,
                 const uint8_t* expected,
                 size_t expected_len)
{
    return static_cast<Checksum*>(ctx)->checksum_ok(data, len, expected, expected_len);
}

uint32_t clock_now_ms(void* ctx)
{
    return static_cast<Clock*>(ctx)->now_ms();
}

void logger_log(void* ctx, const char* msg)
{
    static_cast<Logger*>(ctx)->log(msg);
}

} // namespace

shd_serial_io_t make_c_serial(SerialIo& serial)
{
    shd_serial_io_t out;
    out.ctx = &serial;
    out.available = &serial_available;
    out.read_byte = &serial_read_byte;
    out.read_bytes = &serial_read_bytes;
    out.write_bytes = &serial_write_bytes;
    return out;
}

shd_keyboard_t make_c_keyboard(Keyboard& keyboard)
{
    shd_keyboard_t out;
    out.ctx = &keyboard;
    out.press_scan_code = &keyboard_press;
    out.release_scan_code = &keyboard_release;
    out.release_all = &keyboard_release_all;
    return out;
}

shd_rel_mouse_t make_c_rel_mouse(RelMouse& mouse)
{
    shd_rel_mouse_t out;
    out.ctx = &mouse;
    out.move = &rel_mouse_move;
    out.scroll = &rel_mouse_scroll;
    out.press = &rel_mouse_press;
    out.release = &rel_mouse_release;
    return out;
}

shd_abs_mouse_t make_c_abs_mouse(AbsMouse& mouse)
{
    shd_abs_mouse_t out;
    out.ctx = &mouse;
    out.move = &abs_mouse_move;
    out.change_resolution = &abs_mouse_change_resolution;
    return out;
}

shd_checksum_t make_c_checksum(Checksum& checksum)
{
    shd_checksum_t out;
    out.ctx = &checksum;
    out.compute = &checksum_compute;
    out.checksum_ok = &checksum_ok;
    return out;
}

shd_clock_t make_c_clock(Clock& clock)
{
    shd_clock_t out;
    out.ctx = &clock;
    out.now_ms = &clock_now_ms;
    return out;
}

shd_logger_t make_c_logger(Logger& logger)
{
    shd_logger_t out;
    out.ctx = &logger;
    out.log = &logger_log;
    return out;
}

Core::Core(const shd_core_deps_t& deps)
    : core_(), deps_(deps)
{
    shd_core_init(&core_, &deps_);
}

Core::Core(SerialIo& serial,
           Keyboard& keyboard,
           RelMouse& rel_mouse,
           AbsMouse& abs_mouse,
           Checksum& checksum,
           Clock& clock,
           Logger* logger)
    : core_(), deps_()
{
    deps_.serial = make_c_serial(serial);
    deps_.keyboard = make_c_keyboard(keyboard);
    deps_.rel_mouse = make_c_rel_mouse(rel_mouse);
    deps_.abs_mouse = make_c_abs_mouse(abs_mouse);
    deps_.checksum = make_c_checksum(checksum);
    deps_.clock = make_c_clock(clock);
    if (logger != 0)
    {
        deps_.logger = make_c_logger(*logger);
    }
    shd_core_init(&core_, &deps_);
}

shd_status_t Core::tick()
{
    return shd_core_tick(&core_);
}

void Core::set_timeout_ms(uint32_t timeout_ms)
{
    shd_core_set_timeout_ms(&core_, timeout_ms);
}

void Core::set_resolution(uint16_t width, uint16_t height)
{
    shd_core_set_resolution(&core_, width, height);
}

uint16_t Core::resolution_width() const
{
    return shd_core_resolution_width(&core_);
}

uint16_t Core::resolution_height() const
{
    return shd_core_resolution_height(&core_);
}

shd_core_t* Core::c_handle()
{
    return &core_;
}

const shd_core_t* Core::c_handle() const
{
    return &core_;
}

} // namespace cpp
} // namespace shd
