#ifndef SERIAL_SYMBOLS_H_
#define SERIAL_SYMBOLS_H_

#include <stdint.h>

constexpr unsigned long BAUD_RATE = 500000u;

/*
 * Frame format:
 * 0xAB <Length> <Data...> <Checksum>
 *
 * Length = Data + Checksum
 * Checksum is XOR checksum of all data bytes
 *
 * Data format:
 * Mouse move:
 * <Type> <2-byte x> <2-byte y>
 *
 * Mouse Scroll
 * <Type> <Steps>
 *
 * Mouse / Keyboard button:
 * <Type> <Key>
 *
 */

constexpr uint8_t FRAME_START = 0xABu;
constexpr uint8_t MAX_DATA_LENGTH = 6; // Data(max 5-byte) + Checksum(1-byte)
constexpr uint8_t MAX_FRAME_LENGTH = MAX_DATA_LENGTH + 2; // Prefix 0xAB <Length> are 2 bytes

enum FrameType
{
    FRAME_TYPE_REL_MOUSE_MOVE = 0xA0u,
    FRAME_TYPE_MOUSE_MOVE = 0xAAu,
    FRAME_TYPE_MOUSE_SCROLL = 0xABu,
    FRAME_TYPE_MOUSE_PRESS = 0xACu,
    FRAME_TYPE_MOUSE_RELEASE = 0xADu,
    FRAME_TYPE_MOUSE_RESOLUTION = 0xAEu,

    FRAME_TYPE_KEY_PRESS = 0xBBu,
    FRAME_TYPE_KEY_RELEASE = 0xBC,

    FRAME_TYPE_UNKNOWN = 0xFF
};

constexpr uint8_t RELEASE_ALL_KEYS = 0x00u;


#endif


