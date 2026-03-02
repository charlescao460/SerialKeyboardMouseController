# AGENTS.md - serial_hid_core

## Scope
These instructions apply to contributions for the `serial_hid_core` library.

## Purpose
`serial_hid_core` is a C-first embedded protocol core that parses fixed serial HID request frames and emits typed reply frames via injected interfaces.

## Design Rules
- Keep protocol behavior fixed; do not introduce runtime protocol reconfiguration.
- Keep the primary ABI in C and usable from C-only toolchains.
- Keep C++ support as thin wrappers over the C API; wrappers must remain optional.
- Avoid dynamic allocation, exceptions, RTTI-dependent behavior, and heavy STL usage.
- Prefer deterministic execution, bounded buffers, and small stack usage.

## Protocol Invariants
- Request frame format: `0xAB <Length> <FrameId:2> <ReqType> <Payload...> <CRC8>`.
- Reply frame format: `0xAB <Length> <FrameId:2> <ReplyType> <Payload...> <CRC8>`.
- `Length` includes `FrameId + Type + Payload + CRC8`.
- CRC is CRC-8 over `FrameId + Type + Payload` only.
- Existing keyboard/mouse operations return `SHD_REPLY_OP_OK` on success.
- `SHD_RELEASE_ALL_KEYS` behavior must stay intact for key and mouse release paths.
- Absolute move coordinates must remain non-zero and within current resolution bounds.

## Interface Contracts
- `shd_core_deps_t` is the only integration boundary.
- Required dependencies must be non-null.
- `logger` is optional.
- `crc8.compute` is optional; if absent, core must use built-in software CRC-8.
- Timeout policy belongs to core via `shd_core_set_timeout_ms`.
- `serial.read_bytes` may return partial reads; core is responsible for accumulation and timeout handling.

## Logging
- Use logger only for concise diagnostics on invalid or failed frame processing.
- Keep messages short and static-friendly.
- Avoid logging in hot paths unless signaling an error.

## C++ Wrapper Guidance
- Wrapper classes should only adapt C++ implementations to C callbacks.
- Do not add wrapper logic that changes protocol semantics or core decision flow.

## Style
- Use ASCII.
- Keep public API documentation in Doxygen style.
- Keep internal helpers `static` in implementation files.
- Prefer explicit fixed-width integer types for protocol-related values.

## Change Checklist
- Preserve all protocol invariants listed above.
- Update docs when wire format or public API changes.
- Ensure the library still builds in a standard embedded C/C++ toolchain.

## Non-Goals
- No host-side protocol redesign in this library.
- No board-specific logic in core.
- No hidden dependency on Arduino runtime in the C core.
