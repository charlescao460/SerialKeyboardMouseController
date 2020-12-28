/*
 Name:		SerialKeyboardMouseController.ino
 Created:	2020/12/28 4:23:35
 Author:	CSR
*/

#include <Arduino.h>
#include <limits.h>
#include <Keyboard.h>
#include "serial_symbols.h"

#define _DEBUG
#include "debug_print.h"

/****************************** Settings ******************************/
constexpr unsigned long BAUD_RATE = 500000;
constexpr unsigned int SERIAL_TIMEOUT = 1000 / ((BAUD_RATE / 8) / MAX_FRAME_LENGTH) + 2;
constexpr unsigned int RECEIVE_DATA_BUFFER_SIZE = 128;
static_assert(RECEIVE_DATA_BUFFER_SIZE >= MAX_FRAME_LENGTH + 2, "Serial receiving buffer must larger than frame size!");
HardwareSerial& ControlSerial = Serial1;


/*************************** Implementation ***************************/
inline bool xor_checksum_check(const uint8_t* data, const uint8_t length, const uint8_t value)
{
    if (length == 0)
    {
        return true;
    }
    const uint8_t* ptr = data;
    uint8_t checksum = *(ptr++);
    for (uint8_t i = 1; i < length; ++i)
    {
        checksum ^= *ptr;
        ++ptr;
    }
    return checksum == value;
}

// the setup function runs once when you press reset or power the board
void setup()
{
#ifdef _DEBUG
    Serial.begin(115200);
#endif
    ControlSerial.begin(BAUD_RATE);
    ControlSerial.setTimeout(SERIAL_TIMEOUT);
    Keyboard.begin();
    ControlSerial.println("ControlSerial Initialized!");
}

// the loop function runs over and over again until power down or reset
void loop()
{
    static uint8_t data_buffer[RECEIVE_DATA_BUFFER_SIZE];
    static uint8_t* const ptr_data = data_buffer + 2; // reserve 2 bytes for loop-back frame

    if (ControlSerial.read() == FRAME_START)
    {
        // Read length
        uint8_t length = 0xFFu;
        ControlSerial.readBytes(&length, 1);
        if (length > MAX_DATA_LENGTH || length == 0)
        {
            debug_println("Incorrect data length!");
            return;
        }
        // Read data
        if (ControlSerial.readBytes(ptr_data, length) != length)
        {
            debug_println("Reading data timeout!");
            return;
        }
        // Integrity check
        if (!xor_checksum_check(ptr_data, length - 1, ptr_data[length - 1]))
        {
            debug_println("Corrupted data!");
            return;
        }

        // Construct reply of the same frame to indicate host that we've complete the frame
        data_buffer[0] = FRAME_START;
        data_buffer[1] = length;

        // Execute the frame
        const uint8_t type = ptr_data[0];
        switch (type)
        {
        case FRAME_TYPE_MOUSE_MOVE:
        {
            break;
        }
        case FRAME_TYPE_MOUSE_SCROLL:
        {
            break;
        }
        case FRAME_TYPE_MOUSE_PRESS:
        {
            break;
        }
        case FRAME_TYPE_MOUSE_RELEASE:
        {
            break;
        }
        case FRAME_TYPE_KEY_PRESS:
        {
            const uint8_t key = ptr_data[1];
            Keyboard.press(key);
            break;
        }
        case FRAME_TYPE_KEY_RELEASE:
        {
            const uint8_t key = ptr_data[1];
            if (key == RELEASE_ALL_KEYS)
            {
                Keyboard.releaseAll();
            }
            else
            {
                Keyboard.release(key);
            }
            break;
        }
        default:
        {
            return;
        }
        }
        // Send loop-back frame
        ControlSerial.write(data_buffer, length + 2);
    }
}
