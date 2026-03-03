#ifndef KEYBOARD_h
#define KEYBOARD_h

#include <Arduino.h>
#include "HID.h"

#define KEY_LEFT_CTRL 0x80
#define KEY_LEFT_SHIFT 0x81
#define KEY_LEFT_ALT 0x82
#define KEY_LEFT_GUI 0x83
#define KEY_RIGHT_CTRL 0x84
#define KEY_RIGHT_SHIFT 0x85
#define KEY_RIGHT_ALT 0x86
#define KEY_RIGHT_GUI 0x87

#define KEY_UP_ARROW 0xDA
#define KEY_DOWN_ARROW 0xD9
#define KEY_LEFT_ARROW 0xD8
#define KEY_RIGHT_ARROW 0xD7
#define KEY_BACKSPACE 0xB2
#define KEY_TAB 0xB3
#define KEY_RETURN 0xB0
#define KEY_ESC 0xB1
#define KEY_INSERT 0xD1
#define KEY_DELETE 0xD4
#define KEY_PAGE_UP 0xD3
#define KEY_PAGE_DOWN 0xD6
#define KEY_HOME 0xD2
#define KEY_END 0xD5

class Keyboard_ : public Print
{
public:
    Keyboard_();

    void begin();
    void end();

    size_t write(uint8_t k) override;
    size_t write(const uint8_t* buffer, size_t size) override;

    size_t press(uint8_t k);
    size_t press_scan_code(uint8_t k);
    size_t release(uint8_t k);
    size_t release_scan_code(uint8_t k);
    void releaseAll();

    uint8_t lock_status() const;

private:
    static void on_keyboard_led_report(uint8_t leds);

    void send_current_report();
    void build_boot_keys(uint8_t out_keys[6]) const;
    bool decode_ascii(uint8_t ascii, uint8_t* scan_code, bool* with_shift) const;

    uint8_t modifiers_;
    uint8_t nkro_bitmap_[28];
    uint8_t lock_leds_;
};

extern Keyboard_ Keyboard;

#endif
