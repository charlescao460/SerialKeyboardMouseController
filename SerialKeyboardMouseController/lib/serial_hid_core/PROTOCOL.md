# serial_hid_core Protocol Specification

## 1. Scope
This document defines the on-wire serial protocol implemented by `serial_hid_core`.
It covers frame layout, checksum rules, request/reply types, payload constraints, and
error behavior.

## 2. Conventions
- Byte order:
  - All multi-byte integer fields are little-endian.
- Integer encodings:
  - `int8` and `int16` use two's complement representation.
- Constants:
  - `START` byte: `0xAB`
  - `FrameId` size: 2 bytes
  - CRC size: 1 byte
  - CRC polynomial: `0x07`, init `0x00`, no reflection, xorout `0x00`

## 3. Common Frame Structure

### 3.1 Request Frame
`0xAB <Length> <FrameId:2> <ReqType:1> <ReqPayload...> <CRC8:1>`

### 3.2 Reply Frame
`0xAB <Length> <FrameId:2> <ReplyType:1> <ReplyPayload...> <CRC8:1>`

### 3.3 Length Rules
- `Length` counts bytes from `FrameId` through `CRC8`.
- Formula:
  - `Length = 2 (FrameId) + 1 (Type) + PayloadLen + 1 (CRC8)`
- Allowed range:
  - `Length >= 4`
  - `Length <= 16`

### 3.4 CRC-8 Rules
- CRC input:
  - `FrameId + Type + Payload`
- CRC excludes:
  - `START` byte
  - `Length` byte

## 4. Request Types

### 4.0 Request Body Summary
Body means bytes covered by `Length`: `<FrameId:2> <ReqType:1> <ReqPayload...> <CRC8:1>`.

| Request Type | ReqType | Payload Bytes | Length Value | Body Format |
|---|---:|---:|---:|---|
| `SHD_FRAME_REL_MOUSE_MOVE` | `0xA0` | 4 | 8 | `<FrameId:2> 0xA0 <dx:int16> <dy:int16> <CRC8>` |
| `SHD_FRAME_MOUSE_MOVE_ABS` | `0xAA` | 4 | 8 | `<FrameId:2> 0xAA <x:uint16> <y:uint16> <CRC8>` |
| `SHD_FRAME_MOUSE_SCROLL` | `0xAB` | 1 | 5 | `<FrameId:2> 0xAB <steps:int8> <CRC8>` |
| `SHD_FRAME_MOUSE_PRESS` | `0xAC` | 1 | 5 | `<FrameId:2> 0xAC <buttons:uint8> <CRC8>` |
| `SHD_FRAME_MOUSE_RELEASE` | `0xAD` | 1 | 5 | `<FrameId:2> 0xAD <buttons:uint8> <CRC8>` |
| `SHD_FRAME_MOUSE_RESOLUTION` | `0xAE` | 4 | 8 | `<FrameId:2> 0xAE <width:uint16> <height:uint16> <CRC8>` |
| `SHD_FRAME_KEY_PRESS` | `0xBB` | 1 | 5 | `<FrameId:2> 0xBB <scan_code:uint8> <CRC8>` |
| `SHD_FRAME_KEY_RELEASE` | `0xBC` | 1 | 5 | `<FrameId:2> 0xBC <scan_code:uint8> <CRC8>` |
| `SHD_FRAME_QUERY_KEYBOARD_LOCK` | `0xC0` | 0 | 4 | `<FrameId:2> 0xC0 <CRC8>` |
| `SHD_FRAME_QUERY_HOST_STATUS` | `0xC1` | 0 | 4 | `<FrameId:2> 0xC1 <CRC8>` |
| `SHD_FRAME_RESET` | `0xF0` | 1 | 5 | `<FrameId:2> 0xF0 <mode:uint8> <CRC8>` |

### 4.1 `SHD_FRAME_REL_MOUSE_MOVE` (`0xA0`)
- Payload format:
  - `<dx:int16> <dy:int16>`
- Payload length: 4
- Allowed ranges:
  - Parsed as signed 16-bit, then validated to `[-128, 127]` for each axis
- Effect:
  - Calls relative mouse move with `int8 dx, int8 dy`
- Success reply:
  - `SHD_REPLY_OP_OK`
- Execution error replies:
  - `SHD_REPLY_OP_ERROR` with status code
  - Typical status: `SHD_STATUS_INVALID_PAYLOAD`, `SHD_STATUS_OUT_OF_RANGE`

### 4.2 `SHD_FRAME_MOUSE_MOVE_ABS` (`0xAA`)
- Payload format:
  - `<x:uint16> <y:uint16>`
- Payload length: 4
- Allowed ranges:
  - `x` in `[1, current_resolution_width]`
  - `y` in `[1, current_resolution_height]`
- Effect:
  - Calls absolute mouse move
- Success reply:
  - `SHD_REPLY_OP_OK`
- Execution error replies:
  - `SHD_REPLY_OP_ERROR` with status code
  - Typical status: `SHD_STATUS_INVALID_PAYLOAD`, `SHD_STATUS_OUT_OF_RANGE`

### 4.3 `SHD_FRAME_MOUSE_SCROLL` (`0xAB`)
- Payload format:
  - `<steps:int8>`
- Payload length: 1
- Allowed ranges:
  - Full `int8` range `[-128, 127]`
- Effect:
  - Calls relative mouse scroll
- Success reply:
  - `SHD_REPLY_OP_OK`
- Execution error replies:
  - `SHD_REPLY_OP_ERROR` with status code
  - Typical status: `SHD_STATUS_INVALID_PAYLOAD`

### 4.4 `SHD_FRAME_MOUSE_PRESS` (`0xAC`)
- Payload format:
  - `<buttons:uint8>`
- Payload length: 1
- Accepted range:
  - `0x00` to `0xFF` (passed to backend as-is)
- Defined button bits:
  - `0x01` left, `0x02` right, `0x04` middle
- Effect:
  - Calls relative mouse press
- Success reply:
  - `SHD_REPLY_OP_OK`
- Execution error replies:
  - `SHD_REPLY_OP_ERROR` with status code
  - Typical status: `SHD_STATUS_INVALID_PAYLOAD`

### 4.5 `SHD_FRAME_MOUSE_RELEASE` (`0xAD`)
- Payload format:
  - `<buttons:uint8>`
- Payload length: 1
- Accepted range:
  - `0x00` to `0xFF`
- Defined button bits:
  - `0x01` left, `0x02` right, `0x04` middle
  - Special value `0x00` means release all supported mouse buttons
- Effect:
  - Calls relative mouse release
- Success reply:
  - `SHD_REPLY_OP_OK`
- Execution error replies:
  - `SHD_REPLY_OP_ERROR` with status code
  - Typical status: `SHD_STATUS_INVALID_PAYLOAD`

### 4.6 `SHD_FRAME_MOUSE_RESOLUTION` (`0xAE`)
- Payload format:
  - `<width:uint16> <height:uint16>`
- Payload length: 4
- Allowed ranges:
  - `width` in `[1, 32767]`
  - `height` in `[1, 32767]`
- Effect:
  - Updates core resolution state and calls absolute mouse resolution change
- Success reply:
  - `SHD_REPLY_OP_OK`
- Execution error replies:
  - `SHD_REPLY_OP_ERROR` with status code
  - Typical status: `SHD_STATUS_INVALID_PAYLOAD`, `SHD_STATUS_OUT_OF_RANGE`

### 4.7 `SHD_FRAME_KEY_PRESS` (`0xBB`)
- Payload format:
  - `<scan_code:uint8>`
- Payload length: 1
- Allowed ranges:
  - `0x00` to `0xFF`
- Effect:
  - Calls keyboard press scan code
- Success reply:
  - `SHD_REPLY_OP_OK`
- Execution error replies:
  - `SHD_REPLY_OP_ERROR` with status code
  - Typical status: `SHD_STATUS_INVALID_PAYLOAD`

### 4.8 `SHD_FRAME_KEY_RELEASE` (`0xBC`)
- Payload format:
  - `<scan_code:uint8>`
- Payload length: 1
- Allowed ranges:
  - `0x00` to `0xFF`
  - Special value `0x00` means release all held keys
- Effect:
  - Calls keyboard release scan code or release all
- Success reply:
  - `SHD_REPLY_OP_OK`
- Execution error replies:
  - `SHD_REPLY_OP_ERROR` with status code
  - Typical status: `SHD_STATUS_INVALID_PAYLOAD`

### 4.9 `SHD_FRAME_QUERY_KEYBOARD_LOCK` (`0xC0`)
- Payload format:
  - none
- Payload length: 0
- Effect:
  - Reads keyboard LED lock state
- Success reply:
  - `SHD_REPLY_KEYBOARD_LOCK`
  - Reply payload length: 1
  - Payload bit flags:
    - bit0: Num Lock
    - bit1: Caps Lock
    - bit2: Scroll Lock
- Execution error replies:
  - `SHD_REPLY_OP_ERROR` with status code
  - Typical status: `SHD_STATUS_INVALID_PAYLOAD`

### 4.10 `SHD_FRAME_QUERY_HOST_STATUS` (`0xC1`)
- Payload format:
  - none
- Payload length: 0
- Effect:
  - Reads host/device connection status flags
- Success reply:
  - `SHD_REPLY_HOST_STATUS`
  - Reply payload length: 1
  - Payload bit flags:
    - bit0: configured
    - bit1: suspended
    - bit2: SOF active
- Execution error replies:
  - `SHD_REPLY_OP_ERROR` with status code
  - Typical status: `SHD_STATUS_INVALID_PAYLOAD`

### 4.11 `SHD_FRAME_RESET` (`0xF0`)
- Payload format:
  - `<mode:uint8>`
- Payload length: 1
- Allowed ranges:
  - `0x00` means normal reset
  - nonzero means bootloader requested
- Effect:
  - Returns success reply, then reboots immediately after successful reply write
- Success reply:
  - `SHD_REPLY_OP_OK`
  - Reply payload length: 0
- Execution error replies:
  - `SHD_REPLY_OP_ERROR` with status code
  - Typical status: `SHD_STATUS_INVALID_PAYLOAD`

## 5. Reply Types and Payloads

### 5.1 `SHD_REPLY_OP_OK` (`0x01`)
- Meaning:
  - Request was valid and executed successfully
- Payload length:
  - 0

### 5.2 `SHD_REPLY_OP_ERROR` (`0x02`)
- Meaning:
  - Request frame was validly parsed, but execution or validation failed
- Payload length:
  - 1
- Payload format:
  - `<status:uint8>` where value is `shd_status_t`

### 5.3 `SHD_REPLY_INVALID` (`0x03`)
- Meaning:
  - Framing-level invalid input (for example invalid start, invalid length, CRC mismatch)
- Payload length:
  - 0
- FrameId behavior:
  - Uses parsed/best-effort FrameId when available, otherwise `0xFFFF`

### 5.4 `SHD_REPLY_TIMEOUT` (`0x04`)
- Meaning:
  - Timeout while reading an in-progress frame
- Payload length:
  - 0
- FrameId behavior:
  - Uses parsed/best-effort FrameId when available, otherwise `0xFFFF`

### 5.5 `SHD_REPLY_KEYBOARD_LOCK` (`0x20`)
- Meaning:
  - Reply for keyboard lock query
- Payload length:
  - 1
- Payload:
  - lock bitmask (`Num/Caps/Scroll`)

### 5.6 `SHD_REPLY_HOST_STATUS` (`0x21`)
- Meaning:
  - Reply for host status query
- Payload length:
  - 1
- Payload:
  - host status bitmask (`configured/suspended/SOF active`)

## 6. Global Error Behavior
- If frame parsing fails at transport/protocol level:
  - `SHD_REPLY_INVALID` or `SHD_REPLY_TIMEOUT` is sent.
- If request parsing succeeds but request is invalid for its type:
  - `SHD_REPLY_OP_ERROR` is sent with 1-byte `shd_status_t`.
- If a request succeeds:
  - Type-specific success reply is sent.
- Reset command special rule:
  - Reboot happens only after a successful reply write.
