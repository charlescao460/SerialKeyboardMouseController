#ifndef SERIAL_HID_INTERFACES_H_
#define SERIAL_HID_INTERFACES_H_

#include "serial_hid_types.h"

#ifdef __cplusplus
extern "C" {
#endif

/**
 * @file serial_hid_interfaces.h
 * @brief C ABI interfaces injected into the serial HID core.
 */

/** @brief Serial I/O callbacks used by the core parser. */
typedef struct shd_serial_io {
    /** @brief User context passed to callbacks. */
    void* ctx;
    /** @brief Returns number of bytes ready to read, or 0 if none. */
    int (*available)(void* ctx);
    /** @brief Reads one byte, or returns -1 if no data is available. */
    int (*read_byte)(void* ctx);
    /**
     * @brief Reads up to @p len bytes into @p dst.
     * @return Number of bytes copied to @p dst.
     */
    size_t (*read_bytes)(void* ctx, uint8_t* dst, size_t len);
    /**
     * @brief Writes @p len bytes from @p src.
     * @return Number of bytes written.
     */
    size_t (*write_bytes)(void* ctx, const uint8_t* src, size_t len);
} shd_serial_io_t;

/** @brief Keyboard callbacks for scan-code based key control. */
typedef struct shd_keyboard {
    void* ctx;
    void (*press_scan_code)(void* ctx, uint8_t key);
    void (*release_scan_code)(void* ctx, uint8_t key);
    void (*release_all)(void* ctx);
} shd_keyboard_t;

/** @brief Relative mouse callbacks for move/scroll/button actions. */
typedef struct shd_rel_mouse {
    void* ctx;
    void (*move)(void* ctx, int8_t dx, int8_t dy);
    void (*scroll)(void* ctx, int8_t step);
    void (*press)(void* ctx, uint8_t buttons);
    void (*release)(void* ctx, uint8_t buttons);
} shd_rel_mouse_t;

/** @brief Absolute mouse callbacks for coordinate move/resolution changes. */
typedef struct shd_abs_mouse {
    void* ctx;
    void (*move)(void* ctx, uint16_t x, uint16_t y);
    void (*change_resolution)(void* ctx, uint16_t width, uint16_t height);
} shd_abs_mouse_t;

/** @brief Checksum callbacks used for payload integrity verification. */
typedef struct shd_checksum {
    void* ctx;
    /**
     * @brief Computes checksum bytes for @p data.
     * @return Number of checksum bytes written to @p out.
     */
    size_t (*compute)(void* ctx, const uint8_t* data, size_t len, uint8_t* out, size_t out_cap);
    /**
     * @brief Verifies checksum bytes attached to @p data.
     * @return true when checksum matches.
     */
    bool (*checksum_ok)(void* ctx,
                        const uint8_t* data,
                        size_t len,
                        const uint8_t* expected,
                        size_t expected_len);
} shd_checksum_t;

/** @brief Monotonic millisecond clock used for timeout handling. */
typedef struct shd_clock {
    void* ctx;
    uint32_t (*now_ms)(void* ctx);
} shd_clock_t;

/** @brief Optional logger callback used by the core for diagnostics. */
typedef struct shd_logger {
    void* ctx;
    void (*log)(void* ctx, const char* msg);
} shd_logger_t;

/** @brief Aggregated runtime dependencies required by the core. */
typedef struct shd_core_deps {
    shd_serial_io_t serial;
    shd_keyboard_t keyboard;
    shd_rel_mouse_t rel_mouse;
    shd_abs_mouse_t abs_mouse;
    shd_checksum_t checksum;
    shd_clock_t clock;
    shd_logger_t logger;
} shd_core_deps_t;

#ifdef __cplusplus
}
#endif

#endif /* SERIAL_HID_INTERFACES_H_ */
