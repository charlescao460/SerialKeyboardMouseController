# AGENTS.md - serial_hid_core

## Scope
This file applies to everything under this directory.

## Purpose
`serial_hid_core` is a C-first embedded library that parses a fixed serial protocol and dispatches HID actions through injected interfaces.

## Design Rules
- Keep the protocol fixed. Do not add runtime protocol configuration.
- Core ABI is C (`.h/.c`) and must remain usable by C-only toolchains.
- C++ is wrapper-only (`serial_hid_cpp.hpp/.cpp`) and must not become required for core use.
- Avoid dynamic allocation, exceptions, RTTI-dependent patterns, and heavy STL usage.
- Prefer predictable stack usage and small static buffers.

## Protocol Invariants
- Frame format: `0xAB <Length> <Data...> <Checksum>`.
- `Length` includes data bytes and checksum bytes.
- Max data length is fixed by `SHD_MAX_DATA_LENGTH`.
- ACK is always sent for successfully executed frames.
- `SHD_RELEASE_ALL_KEYS` behavior must be preserved for key and mouse release commands.
- Absolute move coordinates must remain bounded by current resolution and non-zero.

## Interface Contracts
- `shd_core_deps_t` callbacks are the integration boundary.
- `logger` is optional; all other required callbacks must be non-null.
- Timeout policy belongs to core (`shd_core_set_timeout_ms`).
- `serial.read_bytes` may return partial reads; core handles accumulation and timeout.
- Checksum backend is pluggable via `compute`/`checksum_ok`.

## Logging
- Use logger for concise diagnostic messages from core error paths.
- Keep log strings short and static-friendly.
- Do not log in hot paths unless it signals an actual error or invalid frame.

## C++ Wrapper Guidance
- Wrapper types should be thin adapters over C interfaces.
- Keep names aligned with C concepts (`SerialIo`, `RelMouse`, `AbsMouse`, etc.).
- Do not add behavior in wrappers that changes core semantics.

## File Map
- `include/serial_hid_types.h`: protocol constants/enums/status values.
- `include/serial_hid_interfaces.h`: C callback interfaces and deps struct.
- `include/serial_hid_core.h`: C core public API + protocol documentation.
- `include/serial_hid_cpp.hpp`: optional C++ interfaces/adapters.
- `src/serial_hid_core.c`: parser, validation, dispatch, ACK.
- `src/serial_hid_cpp.cpp`: C++ to C adapter implementations.

## Style
- Use ASCII.
- Keep Doxygen comments for public APIs.
- Keep private helper functions `static` in `.c/.cpp` files.
- Prefer explicit fixed-width integer types.

## Compatibility Checklist (before finishing a change)
- Verify protocol invariants above remain true.
- If API signatures changed, update Doxygen comments and this file.

## Non-Goals
- No host-side protocol redesign here.
- No board-specific logic in core.
- No hidden dependency on Arduino runtime in C core.
