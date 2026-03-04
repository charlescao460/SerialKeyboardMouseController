# serial_hid_core

`serial_hid_core` is a C-first embedded library for parsing fixed serial HID request frames and producing typed reply frames.

## Features
- Fixed request/reply protocol with 16-bit Frame ID
- CRC-8 integrity check (default pure C implementation, no lookup table)
- Optional hardware CRC-8 override callback
- No dynamic allocation in core
- C ABI for C-only toolchains
- Optional C++ wrapper layer
- Reset request support with post-reply reboot callback

## Frame Format
Request:
- `0xAB <Length> <FrameId:2 LE> <ReqType:1> <ReqPayload...> <CRC8:1>`

Reply:
- `0xAB <Length> <FrameId:2 LE> <ReplyType:1> <ReplyPayload...> <CRC8:1>`

Length rule:
- `Length` is byte count from `FrameId` through `CRC8`.

CRC-8 rule:
- CRC input is `FrameId + Type + Payload`.
- Start byte and length byte are excluded.
- Default CRC parameters: poly `0x07`, init `0x00`, no reflection, xorout `0x00`.

## Request and Reply Types
See:
- `include/serial_hid_types.h` (`shd_frame_type_t`, `shd_reply_type_t`)

Currently implemented requests:
- Relative mouse move
- Absolute mouse move
- Mouse scroll / press / release
- Mouse resolution
- Key press / release
- Keyboard lock query
- Host status query
- Reset request (`SHD_FRAME_RESET`, type `0xF0`)

Current reply behavior:
- Successful operation -> `SHD_REPLY_OP_OK` with same `FrameId`
- Keyboard lock query -> `SHD_REPLY_KEYBOARD_LOCK` + 1 byte lock bitmask
- Host status query -> `SHD_REPLY_HOST_STATUS` + 1 byte host-status bitmask
- Valid request with execution error -> `SHD_REPLY_OP_ERROR` + 1 byte `shd_status_t`
- Invalid/corrupted frames -> `SHD_REPLY_INVALID` (best-effort `FrameId`, or `0xFFFF`)
- Read timeout while receiving frame -> `SHD_REPLY_TIMEOUT` (best-effort `FrameId`, or `0xFFFF`)
- Valid reset request -> `SHD_REPLY_OP_OK` then reboot callback is invoked immediately
  after successful reply transmission

## C Integration
1. Include `serial_hid_core.h`
2. Provide `shd_core_deps_t` callbacks:
- Required: serial, keyboard, rel_mouse, abs_mouse, host, reset, clock
- Optional: crc8 override, logger
3. Init and run:
- `shd_core_init(&core, &deps);`
- `shd_core_set_timeout_ms(&core, timeout_ms);`
- call `shd_core_tick(&core);` in your main loop

### Minimal C Example
```c
#include "serial_hid_core.h"

static shd_core_t g_core;

void app_init(const shd_core_deps_t* deps) {
    shd_core_init(&g_core, deps);
    shd_core_set_timeout_ms(&g_core, 5u);
}

void app_loop(void) {
    (void)shd_core_tick(&g_core);
}
```

## C++ Integration
Include `serial_hid_cpp.hpp`, implement adapter classes, then construct `shd::cpp::Core`.

Constructor:
```cpp
shd::cpp::Core core(serial, keyboard, rel_mouse, abs_mouse, host, reset, clock,
                    /*crc8*/ nullptr,
                    /*logger*/ nullptr);
```

If you have hardware CRC-8, pass a `shd::cpp::Crc8` implementation.
Implement `Keyboard::get_lock_state()` and `Host::get_status_flags()` for query support.
Implement `Reset::reboot(uint8_t)` for reset-frame support.

## Reset Payload
- Request type: `SHD_FRAME_RESET` (`0xF0`)
- Payload length: exactly 1 byte
- Payload values:
  - `SHD_RESET_NORMAL` (`0x00`): normal reboot
  - `SHD_RESET_BOOTLOADER` (`0x01`): bootloader requested (any nonzero is treated as requested)

For Arduino integration in this repository, bootloader intent is currently ignored and a normal
reboot is performed.
