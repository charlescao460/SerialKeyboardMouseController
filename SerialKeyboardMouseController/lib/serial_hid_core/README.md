# serial_hid_core

`serial_hid_core` is a C-first embedded library for parsing fixed serial HID request frames and producing typed reply frames.

## Features
- Fixed request/reply protocol with 16-bit Frame ID
- CRC-8 integrity check (default pure C implementation, no lookup table)
- Optional hardware CRC-8 override callback
- No dynamic allocation in core
- C ABI for C-only toolchains
- Optional C++ wrapper layer

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

Current reply behavior:
- Successful operation -> `SHD_REPLY_OP_OK` with same `FrameId`
- Query request enums are declared for future expansion but not implemented yet

## C Integration
1. Include `serial_hid_core.h`
2. Provide `shd_core_deps_t` callbacks:
- Required: serial, keyboard, rel_mouse, abs_mouse, clock
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
shd::cpp::Core core(serial, keyboard, rel_mouse, abs_mouse, clock,
                    /*crc8*/ nullptr,
                    /*logger*/ nullptr);
```

If you have hardware CRC-8, pass a `shd::cpp::Crc8` implementation.

