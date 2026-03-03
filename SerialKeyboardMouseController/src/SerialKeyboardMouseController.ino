/*
 Name:      SerialKeyboardMouseController.ino
 Created:   2020/12/28 4:23:35
 Author:    CSR
*/

// Make sure to change SERIAL_RX_BUFFER_SIZE to 512 or higher in <HardwareSerial.h>
#include <Arduino.h>
#include "Keyboard.h"
#include "AbsMouse.h"
#include "debug_print.h"
#include "serial_hid_cpp.hpp"

/****************************** Settings ******************************/
constexpr unsigned long BAUD_RATE = 500000u;
constexpr unsigned int SERIAL_TIMEOUT =
    1000 / ((BAUD_RATE / 8) / SHD_MAX_FRAME_LENGTH) + 2;
HardwareSerial& ControlSerial = Serial1;

/****************************** Adapters ******************************/
class ArduinoSerialIo : public shd::cpp::SerialIo
{
public:
    explicit ArduinoSerialIo(HardwareSerial& serial)
        : serial_(serial)
    {
    }

    int available() override
    {
        return serial_.available();
    }

    int read_byte() override
    {
        return serial_.read();
    }

    size_t read_bytes(uint8_t* dst, size_t len) override
    {
        return serial_.readBytes(dst, len);
    }

    size_t write_bytes(const uint8_t* src, size_t len) override
    {
        return serial_.write(src, len);
    }

private:
    HardwareSerial& serial_;
};

class ArduinoKeyboard : public shd::cpp::Keyboard
{
public:
    void press_scan_code(uint8_t key) override
    {
        ::Keyboard.press_scan_code(key);
    }

    void release_scan_code(uint8_t key) override
    {
        ::Keyboard.release_scan_code(key);
    }

    void release_all() override
    {
        ::Keyboard.releaseAll();
    }

    uint8_t get_lock_state() override
    {
        return ::Keyboard.lock_status();
    }
};

class ArduinoRelMouse : public shd::cpp::RelMouse
{
public:
    void move(int8_t dx, int8_t dy) override
    {
        ::AbsMouse.moveRelative(dx, dy);
    }

    void scroll(int8_t step) override
    {
        ::AbsMouse.scroll(step);
    }

    void press(uint8_t buttons) override
    {
        ::AbsMouse.press(buttons);
    }

    void release(uint8_t buttons) override
    {
        ::AbsMouse.release(buttons);
    }
};

class ArduinoAbsMouse : public shd::cpp::AbsMouse
{
public:
    void move(uint16_t x, uint16_t y) override
    {
        ::AbsMouse.move(x, y);
    }

    void change_resolution(uint16_t width, uint16_t height) override
    {
        ::AbsMouse.init(width, height, true);
    }
};

class ArduinoClock : public shd::cpp::Clock
{
public:
    uint32_t now_ms() override
    {
        return millis();
    }
};

class ArduinoHost : public shd::cpp::Host
{
public:
    uint8_t get_status_flags() override
    {
        return HID().hostStatusFlags();
    }
};

/****************************** Globals *******************************/
static ArduinoSerialIo g_serial_io(ControlSerial);
static ArduinoKeyboard g_keyboard;
static ArduinoRelMouse g_rel_mouse;
static ArduinoAbsMouse g_abs_mouse;
static ArduinoHost g_host;
static ArduinoClock g_clock;

static shd::cpp::Core g_core(g_serial_io, g_keyboard, g_rel_mouse, g_abs_mouse, g_host, g_clock);

// the setup function runs once when you press reset or power the board
void setup()
{
#ifdef _DEBUG
    Serial.begin(115200);
#endif

    pinMode(LED_BUILTIN, OUTPUT);
    analogWrite(LED_BUILTIN, 16);

    ControlSerial.begin(BAUD_RATE);
    ControlSerial.setTimeout(SERIAL_TIMEOUT);

    ::Keyboard.begin();
    ::AbsMouse.init(SHD_MAX_RESOLUTION_WIDTH, SHD_MAX_RESOLUTION_HEIGHT, true);

    g_core.set_timeout_ms(SERIAL_TIMEOUT);
    g_core.set_resolution(SHD_MAX_RESOLUTION_WIDTH, SHD_MAX_RESOLUTION_HEIGHT);

    ControlSerial.println("ControlSerial Initialized!");
}

// the loop function runs over and over again until power down or reset
void loop()
{
#ifdef _DEBUG
    const shd_status_t status = g_core.tick();
    if (status != SHD_STATUS_OK && status != SHD_STATUS_NO_DATA)
    {
        debug_print("Core tick status: ");
        debug_println(static_cast<int>(status));
    }
#else
    (void)g_core.tick();
#endif
}
