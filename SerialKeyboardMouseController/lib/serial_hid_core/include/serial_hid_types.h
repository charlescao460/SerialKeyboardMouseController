#ifndef SERIAL_HID_TYPES_H_
#define SERIAL_HID_TYPES_H_

#include <stdbool.h>
#include <stddef.h>
#include <stdint.h>

#ifdef __cplusplus
extern "C" {
#endif

/**
 * @file serial_hid_types.h
 * @brief Fixed protocol constants and public value types for the serial HID core.
 */

/** @brief UART frame start byte. */
#define SHD_FRAME_START ((uint8_t)0xABu)
/** @brief Request/reply frame ID size in bytes. */
#define SHD_FRAME_ID_SIZE ((size_t)2u)
/** @brief CRC-8 field size in bytes. */
#define SHD_CRC8_SIZE ((size_t)1u)
/** @brief Special key code that indicates releasing all held keys/buttons. */
#define SHD_RELEASE_ALL_KEYS ((uint8_t)0x00u)

/**
 * @brief Max payload section length in frame data bytes.
 *
 * Data section = FrameId + Type + Payload + CRC8.
 */
#define SHD_MAX_DATA_LENGTH ((size_t)16u)
/** @brief Max complete frame length: start + length + data section. */
#define SHD_MAX_FRAME_LENGTH ((size_t)(SHD_MAX_DATA_LENGTH + 2u))
/** @brief Minimum request/reply data section length. */
#define SHD_MIN_DATA_LENGTH ((size_t)(SHD_FRAME_ID_SIZE + 1u + SHD_CRC8_SIZE))

/** @brief Absolute mouse coordinate upper bound on each axis. */
#define SHD_MAX_RESOLUTION_WIDTH ((uint16_t)32767u)
/** @brief Absolute mouse coordinate upper bound on each axis. */
#define SHD_MAX_RESOLUTION_HEIGHT ((uint16_t)32767u)

/** @brief Request command type IDs from host to device. */
typedef enum shd_frame_type {
    SHD_FRAME_REL_MOUSE_MOVE = 0xA0u,
    SHD_FRAME_MOUSE_MOVE_ABS = 0xAAu,
    SHD_FRAME_MOUSE_SCROLL = 0xABu,
    SHD_FRAME_MOUSE_PRESS = 0xACu,
    SHD_FRAME_MOUSE_RELEASE = 0xADu,
    SHD_FRAME_MOUSE_RESOLUTION = 0xAEu,

    SHD_FRAME_KEY_PRESS = 0xBBu,
    SHD_FRAME_KEY_RELEASE = 0xBCu,

    SHD_FRAME_QUERY_KEYBOARD_LOCK = 0xC0u,
    SHD_FRAME_QUERY_HOST_STATUS = 0xC1u,
    SHD_FRAME_RESET = 0xF0u
} shd_frame_type_t;

/** @brief Reset request payload values for SHD_FRAME_RESET. */
typedef enum shd_reset_mode {
    SHD_RESET_NORMAL = 0x00u,
    SHD_RESET_BOOTLOADER = 0x01u
} shd_reset_mode_t;

/** @brief Reply frame type IDs from device to host. */
typedef enum shd_reply_type {
    SHD_REPLY_OP_OK = 0x01u, // HID report succeed
    SHD_REPLY_OP_ERROR = 0x02u, // HID report error, including timeout in HID report or USB-related.
    SHD_REPLY_INVALID = 0x03u, // Invalid frame
    SHD_REPLY_TIMEOUT = 0x04u, // Timeout on serial read, NOT the timeout related to HID. 

    SHD_REPLY_KEYBOARD_LOCK = 0x20u,
    SHD_REPLY_HOST_STATUS = 0x21u
} shd_reply_type_t;

/** @brief Keyboard lock state payload flags for SHD_REPLY_KEYBOARD_LOCK. */
typedef enum shd_keyboard_lock_flag {
    SHD_KEYBOARD_LOCK_NUM = 0x01u,
    SHD_KEYBOARD_LOCK_CAPS = 0x02u,
    SHD_KEYBOARD_LOCK_SCROLL = 0x04u
} shd_keyboard_lock_flag_t;

/** @brief Host status payload flags for SHD_REPLY_HOST_STATUS. */
typedef enum shd_host_status_flag {
    SHD_HOST_STATUS_CONFIGURED = 0x01u,
    SHD_HOST_STATUS_SUSPENDED = 0x02u,
    SHD_HOST_STATUS_SOF_ACTIVE = 0x04u
} shd_host_status_flag_t;

/** @brief Mouse button bit flags used by press/release commands. */
typedef enum shd_mouse_button {
    SHD_MOUSE_BTN_LEFT = 0x01u,
    SHD_MOUSE_BTN_RIGHT = 0x02u,
    SHD_MOUSE_BTN_MIDDLE = 0x04u
} shd_mouse_button_t;

/** @brief Result codes returned by the core tick function. */
typedef enum shd_status {
    SHD_STATUS_OK = 0,
    SHD_STATUS_NO_DATA,
    SHD_STATUS_INVALID_START,
    SHD_STATUS_INVALID_LENGTH,
    SHD_STATUS_TIMEOUT,
    SHD_STATUS_CRC_MISMATCH,
    SHD_STATUS_UNSUPPORTED_FRAME,
    SHD_STATUS_INVALID_PAYLOAD,
    SHD_STATUS_OUT_OF_RANGE,
    SHD_STATUS_IO_ERROR,
    SHD_STATUS_BAD_DEPENDENCY
} shd_status_t;

#ifdef __cplusplus
}
#endif

#endif /* SERIAL_HID_TYPES_H_ */
