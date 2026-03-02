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
/** @brief Frame length upper bound: data bytes + checksum byte. */
#define SHD_MAX_DATA_LENGTH ((size_t)6u)
/** @brief Max complete frame length: start + length + data/checksum. */
#define SHD_MAX_FRAME_LENGTH ((size_t)(SHD_MAX_DATA_LENGTH + 2u))
/** @brief Special key code that indicates releasing all held keys/buttons. */
#define SHD_RELEASE_ALL_KEYS ((uint8_t)0x00u)

/** @brief Absolute mouse coordinate upper bound on each axis. */
#define SHD_MAX_RESOLUTION_WIDTH ((uint16_t)32767u)
/** @brief Absolute mouse coordinate upper bound on each axis. */
#define SHD_MAX_RESOLUTION_HEIGHT ((uint16_t)32767u)

/** @brief Protocol command type IDs. */
typedef enum shd_frame_type {
    SHD_FRAME_REL_MOUSE_MOVE = 0xA0u,
    SHD_FRAME_MOUSE_MOVE_ABS = 0xAAu,
    SHD_FRAME_MOUSE_SCROLL = 0xABu,
    SHD_FRAME_MOUSE_PRESS = 0xACu,
    SHD_FRAME_MOUSE_RELEASE = 0xADu,
    SHD_FRAME_MOUSE_RESOLUTION = 0xAEu,
    SHD_FRAME_KEY_PRESS = 0xBBu,
    SHD_FRAME_KEY_RELEASE = 0xBCu
} shd_frame_type_t;

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
    SHD_STATUS_CHECKSUM_MISMATCH,
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
