# AGENTS.md - serial_hid_core

## Scope
These instructions apply to contributions for the `serial_hid_core` library.

## Purpose
`serial_hid_core` is a C-first embedded protocol core that parses fixed serial HID request frames and emits typed reply frames via injected interfaces.

## Design Rules
- Keep protocol behavior fixed; do not introduce runtime protocol reconfiguration.
- Treat [`PROTOCOL.md`](./PROTOCOL.md) as the protocol source of truth.
- Keep the primary ABI in C and usable from C-only toolchains.
- Keep C++ support as thin wrappers over the C API; wrappers must remain optional.
- Avoid dynamic allocation, exceptions, RTTI-dependent behavior, and heavy STL usage.
- Prefer deterministic execution, bounded buffers, and small stack usage.

## Interface Contracts
- `shd_core_deps_t` is the only integration boundary.
- Required dependencies must be non-null.
- `keyboard.get_lock_state` and `host.get_status_flags` are required.
- `reset.reboot` is required.
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
- Preserve all protocol rules defined in [`PROTOCOL.md`](./PROTOCOL.md).
- Update docs when wire format or public API changes.
- Ensure the library still builds in a standard embedded C/C++ toolchain.

## Non-Goals
- No host-side protocol redesign in this library.
- No board-specific logic in core.
- No hidden dependency on Arduino runtime in the C core.
