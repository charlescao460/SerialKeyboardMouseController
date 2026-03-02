#ifndef SERIAL_HID_CORE_H_
#define SERIAL_HID_CORE_H_

#include "serial_hid_interfaces.h"

#ifdef __cplusplus
extern "C" {
#endif

/**
 * @file serial_hid_core.h
 * @brief C core that parses fixed request frames and sends typed reply frames.
 *
 * Request frame format:
 *   0xAB <Length> <FrameId:2 LE> <ReqType:1> <ReqPayload...> <CRC8:1>
 *
 * Reply frame format:
 *   0xAB <Length> <FrameId:2 LE> <ReplyType:1> <ReplyPayload...> <CRC8:1>
 *
 * Length:
 *   Number of bytes from FrameId through CRC8.
 *
 * CRC-8 scope:
 *   CRC8 is computed over FrameId + Type + Payload.
 *   (Start byte and Length byte are excluded.)
 *
 * Request payload format by type:
 *   Rel mouse move:    <Type> <2-byte dx LE> <2-byte dy LE>
 *   Abs mouse move:    <Type> <2-byte x LE>  <2-byte y LE>
 *   Mouse scroll:      <Type> <Steps>
 *   Mouse press:       <Type> <Buttons>
 *   Mouse release:     <Type> <Buttons>
 *   Mouse resolution:  <Type> <2-byte width LE> <2-byte height LE>
 *   Key press:         <Type> <ScanCode>
 *   Key release:       <Type> <ScanCode>
 *
 * Reply behavior:
 *   Existing keyboard/mouse requests return SHD_REPLY_OP_OK with the same FrameId.
 *   Query reply types are reserved for future use.
 *
 * Example request (mouse left press, FrameId=0x1234):
 *   AB 05 34 12 AC 01 66
 *
 * Example reply (operation succeed, FrameId=0x1234):
 *   AB 04 34 12 01 30
 */

/**
 * @brief Core state object.
 *
 * The structure is intentionally public so embedded users can allocate it
 * statically without dynamic memory.
 */
typedef struct shd_core {
    shd_core_deps_t deps;
    uint32_t timeout_ms;
    uint16_t current_resolution_width;
    uint16_t current_resolution_height;
    uint8_t frame_buffer[SHD_MAX_FRAME_LENGTH];
} shd_core_t;

/**
 * @brief Initializes the core with fixed protocol parameters and dependencies.
 * @param core Core instance to initialize.
 * @param deps Runtime interfaces required for I/O and HID actions.
 */
void shd_core_init(shd_core_t* core, const shd_core_deps_t* deps);

/**
 * @brief Sets frame read timeout used by @ref shd_core_tick.
 * @param core Core instance.
 * @param timeout_ms Timeout in milliseconds.
 */
void shd_core_set_timeout_ms(shd_core_t* core, uint32_t timeout_ms);

/**
 * @brief Overrides current absolute mouse resolution and syncs target interface.
 */
void shd_core_set_resolution(shd_core_t* core, uint16_t width, uint16_t height);

/** @brief Returns current absolute mouse width. */
uint16_t shd_core_resolution_width(const shd_core_t* core);
/** @brief Returns current absolute mouse height. */
uint16_t shd_core_resolution_height(const shd_core_t* core);

/**
 * @brief Processes at most one request frame and emits a typed reply frame.
 */
shd_status_t shd_core_tick(shd_core_t* core);

#ifdef __cplusplus
}
#endif

#endif /* SERIAL_HID_CORE_H_ */
