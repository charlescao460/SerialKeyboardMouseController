# serial_hid_core

`serial_hid_core` is a C-first embedded library for parsing fixed serial HID request frames and producing typed reply frames.

## Features
- Fixed request/reply parser core with strict validation
- CRC-8 integrity check (default pure C implementation, no lookup table)
- Optional hardware CRC-8 override callback
- No dynamic allocation in core
- C ABI for C-only toolchains
- Optional C++ wrapper layer
- Reset request support with post-reply reboot callback

## Protocol Specification
The complete wire protocol specification is maintained in:
- [`PROTOCOL.md`](./PROTOCOL.md)

Use that file for:
- exact frame layouts
- per-request payload formats and constraints
- full reply semantics and error behavior

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
