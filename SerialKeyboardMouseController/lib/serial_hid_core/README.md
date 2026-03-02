# serial_hid_core

`serial_hid_core` is a C-first embedded library for parsing a fixed serial HID protocol and dispatching actions through injected interfaces.

## Features
- Fixed protocol parser and dispatcher (`0xAB <Length> <Data...> <Checksum>`)
- No dynamic allocation in core
- C ABI for broad MCU/toolchain compatibility
- Optional C++ wrapper layer
- Pluggable checksum backend (software or hardware CRC/XOR)
- Always sends ACK (loopback frame) after successful command execution

## Folder Layout
- `include/serial_hid_types.h`: protocol constants, enums, status codes
- `include/serial_hid_interfaces.h`: callback interfaces (serial, HID, checksum, clock, logger)
- `include/serial_hid_core.h`: C core API + protocol docs
- `include/serial_hid_cpp.hpp`: optional C++ wrapper API
- `src/serial_hid_core.c`: core implementation
- `src/serial_hid_cpp.cpp`: C++ adapters/wrapper implementation

## Protocol Summary
Frame format:
- `0xAB <Length> <Data...> <Checksum>`

Rules:
- `Length` includes `<Data...>` and `<Checksum>` bytes
- `SHD_MAX_DATA_LENGTH` limits `<Length>`
- Checksum format is provided by your `shd_checksum_t`
- Current project default checksum is 1-byte XOR

Supported command payloads:
- Relative move: `<Type> <dx:2 LE> <dy:2 LE>`
- Absolute move: `<Type> <x:2 LE> <y:2 LE>`
- Scroll: `<Type> <step>`
- Mouse press/release: `<Type> <buttons>`
- Resolution: `<Type> <width:2 LE> <height:2 LE>`
- Key press/release: `<Type> <scan_code>`

## Integration (C API)
1. Include headers:
- `#include "serial_hid_core.h"`

2. Implement callbacks in `shd_core_deps_t`:
- Required: `serial`, `keyboard`, `rel_mouse`, `abs_mouse`, `checksum`, `clock`
- Optional: `logger`

3. Initialize and run:
- `shd_core_init(&core, &deps);`
- `shd_core_set_timeout_ms(&core, timeout_ms);`
- call `shd_core_tick(&core);` in main loop

### Minimal C Example
```c
#include "serial_hid_core.h"

static shd_core_t g_core;

int main(void) {
    shd_core_deps_t deps;
    /* fill deps callbacks + ctx pointers */

    shd_core_init(&g_core, &deps);
    shd_core_set_timeout_ms(&g_core, 5u);

    for (;;) {
        (void)shd_core_tick(&g_core);
    }
}
```

## Integration (C++ Wrapper)
1. Include:
- `#include "serial_hid_cpp.hpp"`

2. Implement C++ adapter classes:
- `shd::cpp::SerialIo`, `Keyboard`, `RelMouse`, `AbsMouse`, `Checksum`, `Clock`
- optional: `Logger`

3. Construct wrapper core and tick in loop.

### Minimal C++ Example
```cpp
#include "serial_hid_cpp.hpp"

class MySerial : public shd::cpp::SerialIo { /* ... */ };
class MyKeyboard : public shd::cpp::Keyboard { /* ... */ };
class MyRelMouse : public shd::cpp::RelMouse { /* ... */ };
class MyAbsMouse : public shd::cpp::AbsMouse { /* ... */ };
class MyChecksum : public shd::cpp::Checksum { /* ... */ };
class MyClock : public shd::cpp::Clock { /* ... */ };

MySerial serial;
MyKeyboard keyboard;
MyRelMouse rel_mouse;
MyAbsMouse abs_mouse;
MyChecksum checksum;
MyClock clock;

shd::cpp::Core core(serial, keyboard, rel_mouse, abs_mouse, checksum, clock);

void loop() {
    (void)core.tick();
}
```

## Callback Contract Notes
- `serial.read_bytes` may return fewer bytes than requested; core handles accumulation.
- Core timeout is enforced using `clock.now_ms` + `shd_core_set_timeout_ms`.
- `checksum.checksum_ok` is optional. If null, core falls back to `checksum.compute` comparison.
- If a frame executes successfully, ACK is always written using `serial.write_bytes`.

## Status Handling
`shd_core_tick` returns `shd_status_t`, including:
- `SHD_STATUS_OK`
- `SHD_STATUS_NO_DATA`
- `SHD_STATUS_TIMEOUT`
- `SHD_STATUS_CHECKSUM_MISMATCH`
- `SHD_STATUS_UNSUPPORTED_FRAME`
- `SHD_STATUS_BAD_DEPENDENCY`

