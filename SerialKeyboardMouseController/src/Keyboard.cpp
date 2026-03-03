#include "Keyboard.h"

#if defined(_USING_HID)

static const uint8_t kKeyboardReportId = 2u;
static const uint8_t kKeyboardNkroBytes = 28u;
static const uint8_t kKeyboardDescriptor[] PROGMEM = {
    // Top-level keyboard application collection.
    0x05, 0x01,
    0x09, 0x06,
    0xA1, 0x01,
    0x85, kKeyboardReportId,

    // 8 modifier bits (E0..E7).
    0x05, 0x07,
    0x19, 0xE0,
    0x29, 0xE7,
    0x15, 0x00,
    0x25, 0x01,
    0x75, 0x01,
    0x95, 0x08,
    0x81, 0x02,

    // NKRO bitmap for usages 0x00..0xDF (224 keys -> 28 bytes).
    0x05, 0x07,
    0x19, 0x00,
    0x29, 0xDF,
    0x15, 0x00,
    0x25, 0x01,
    0x75, 0x01,
    0x95, 0xE0,
    0x81, 0x02,

    // LED output report (Num/Caps/Scroll/Compose/Kana + padding).
    0x05, 0x08,
    0x19, 0x01,
    0x29, 0x05,
    0x95, 0x05,
    0x75, 0x01,
    0x91, 0x02,
    0x95, 0x01,
    0x75, 0x03,
    0x91, 0x03,

    0xC0};

static Keyboard_* g_keyboard_instance = NULL;

static void set_bitmap_bit(uint8_t* bitmap, uint8_t usage)
{
    bitmap[(uint8_t)(usage >> 3u)] |= (uint8_t)(1u << (usage & 0x07u));
}

static void clear_bitmap_bit(uint8_t* bitmap, uint8_t usage)
{
    bitmap[(uint8_t)(usage >> 3u)] &= (uint8_t)(~(uint8_t)(1u << (usage & 0x07u)));
}

static bool get_bitmap_bit(const uint8_t* bitmap, uint8_t usage)
{
    return (bitmap[(uint8_t)(usage >> 3u)] & (uint8_t)(1u << (usage & 0x07u))) != 0u;
}

Keyboard_::Keyboard_()
    : modifiers_(0u)
    , nkro_bitmap_()
    , lock_leds_(0u)
{
    static HIDSubDescriptor descriptor(kKeyboardDescriptor, sizeof(kKeyboardDescriptor));
    HID().AppendDescriptor(&descriptor);
    g_keyboard_instance = this;
    HID().setKeyboardOutputHandler(&Keyboard_::on_keyboard_led_report);
}

void Keyboard_::begin()
{
}

void Keyboard_::end()
{
}

void Keyboard_::on_keyboard_led_report(uint8_t leds)
{
    if (g_keyboard_instance != NULL)
    {
        g_keyboard_instance->lock_leds_ = leds;
    }
}

void Keyboard_::build_boot_keys(uint8_t out_keys[6]) const
{
    uint8_t count = 0u;
    uint16_t usage = 0u;

    for (usage = 0u; usage <= 0xDFu && count < 6u; ++usage)
    {
        if (get_bitmap_bit(nkro_bitmap_, (uint8_t)usage) && usage != 0u)
        {
            out_keys[count++] = (uint8_t)usage;
        }
    }

    while (count < 6u)
    {
        out_keys[count++] = 0u;
    }
}

void Keyboard_::send_current_report()
{
    if (HID().keyboardProtocol() == HID_BOOT_PROTOCOL)
    {
        uint8_t report[8];
        report[0] = modifiers_;
        report[1] = 0u;
        build_boot_keys(report + 2u);
        HID().SendBootKeyboardReport(report, sizeof(report));
    }
    else
    {
        uint8_t payload[1u + kKeyboardNkroBytes];
        payload[0] = modifiers_;
        memcpy(payload + 1u, nkro_bitmap_, kKeyboardNkroBytes);
        HID().SendReport(kKeyboardReportId, payload, sizeof(payload));
    }
}

bool Keyboard_::decode_ascii(uint8_t ascii, uint8_t* scan_code, bool* with_shift) const
{
    *with_shift = false;

    if (ascii >= 'a' && ascii <= 'z')
    {
        *scan_code = (uint8_t)(0x04u + (ascii - 'a'));
        return true;
    }
    if (ascii >= 'A' && ascii <= 'Z')
    {
        *scan_code = (uint8_t)(0x04u + (ascii - 'A'));
        *with_shift = true;
        return true;
    }
    if (ascii >= '1' && ascii <= '9')
    {
        *scan_code = (uint8_t)(0x1Eu + (ascii - '1'));
        return true;
    }

    switch (ascii)
    {
    case '0': *scan_code = 0x27u; return true;
    case ' ': *scan_code = 0x2Cu; return true;
    case '-': *scan_code = 0x2Du; return true;
    case '=': *scan_code = 0x2Eu; return true;
    case '[': *scan_code = 0x2Fu; return true;
    case ']': *scan_code = 0x30u; return true;
    case '\\': *scan_code = 0x31u; return true;
    case ';': *scan_code = 0x33u; return true;
    case '\'': *scan_code = 0x34u; return true;
    case '`': *scan_code = 0x35u; return true;
    case ',': *scan_code = 0x36u; return true;
    case '.': *scan_code = 0x37u; return true;
    case '/': *scan_code = 0x38u; return true;
    case '\n': *scan_code = 0x28u; return true;
    case '\t': *scan_code = 0x2Bu; return true;
    case '!': *scan_code = 0x1Eu; *with_shift = true; return true;
    case '@': *scan_code = 0x1Fu; *with_shift = true; return true;
    case '#': *scan_code = 0x20u; *with_shift = true; return true;
    case '$': *scan_code = 0x21u; *with_shift = true; return true;
    case '%': *scan_code = 0x22u; *with_shift = true; return true;
    case '^': *scan_code = 0x23u; *with_shift = true; return true;
    case '&': *scan_code = 0x24u; *with_shift = true; return true;
    case '*': *scan_code = 0x25u; *with_shift = true; return true;
    case '(': *scan_code = 0x26u; *with_shift = true; return true;
    case ')': *scan_code = 0x27u; *with_shift = true; return true;
    case '_': *scan_code = 0x2Du; *with_shift = true; return true;
    case '+': *scan_code = 0x2Eu; *with_shift = true; return true;
    case '{': *scan_code = 0x2Fu; *with_shift = true; return true;
    case '}': *scan_code = 0x30u; *with_shift = true; return true;
    case '|': *scan_code = 0x31u; *with_shift = true; return true;
    case ':': *scan_code = 0x33u; *with_shift = true; return true;
    case '"': *scan_code = 0x34u; *with_shift = true; return true;
    case '~': *scan_code = 0x35u; *with_shift = true; return true;
    case '<': *scan_code = 0x36u; *with_shift = true; return true;
    case '>': *scan_code = 0x37u; *with_shift = true; return true;
    case '?': *scan_code = 0x38u; *with_shift = true; return true;
    default:
        break;
    }

    return false;
}

size_t Keyboard_::press_scan_code(uint8_t k)
{
    if (k >= 0xE0u && k <= 0xE7u)
    {
        modifiers_ |= (uint8_t)(1u << (k - 0xE0u));
    }
    else if (k <= 0xDFu)
    {
        set_bitmap_bit(nkro_bitmap_, k);
    }

    send_current_report();
    return 1u;
}

size_t Keyboard_::release_scan_code(uint8_t k)
{
    if (k >= 0xE0u && k <= 0xE7u)
    {
        modifiers_ &= (uint8_t)(~(uint8_t)(1u << (k - 0xE0u)));
    }
    else if (k <= 0xDFu)
    {
        clear_bitmap_bit(nkro_bitmap_, k);
    }

    send_current_report();
    return 1u;
}

size_t Keyboard_::press(uint8_t k)
{
    uint8_t code = 0u;
    bool shift = false;

    if (k >= 128u && k <= 135u)
    {
        return press_scan_code((uint8_t)(0xE0u + (k - 128u)));
    }
    if (!decode_ascii(k, &code, &shift))
    {
        setWriteError();
        return 0u;
    }

    if (shift)
    {
        modifiers_ |= 0x02u;
    }
    return press_scan_code(code);
}

size_t Keyboard_::release(uint8_t k)
{
    uint8_t code = 0u;
    bool shift = false;

    if (k >= 128u && k <= 135u)
    {
        return release_scan_code((uint8_t)(0xE0u + (k - 128u)));
    }
    if (!decode_ascii(k, &code, &shift))
    {
        return 0u;
    }

    if (shift)
    {
        modifiers_ &= (uint8_t)~0x02u;
    }
    return release_scan_code(code);
}

void Keyboard_::releaseAll()
{
    modifiers_ = 0u;
    memset(nkro_bitmap_, 0, sizeof(nkro_bitmap_));
    send_current_report();
}

size_t Keyboard_::write(uint8_t k)
{
    size_t result = press(k);
    (void)release(k);
    return result;
}

size_t Keyboard_::write(const uint8_t* buffer, size_t size)
{
    size_t written = 0u;
    while (size > 0u)
    {
        if (write(*buffer) == 0u)
        {
            break;
        }
        ++written;
        ++buffer;
        --size;
    }
    return written;
}

uint8_t Keyboard_::lock_status() const
{
    uint8_t status = 0u;
    if ((lock_leds_ & 0x01u) != 0u)
    {
        status |= SHD_KEYBOARD_LOCK_NUM;
    }
    if ((lock_leds_ & 0x02u) != 0u)
    {
        status |= SHD_KEYBOARD_LOCK_CAPS;
    }
    if ((lock_leds_ & 0x04u) != 0u)
    {
        status |= SHD_KEYBOARD_LOCK_SCROLL;
    }
    return status;
}

Keyboard_ Keyboard;

#endif
