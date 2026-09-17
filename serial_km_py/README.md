# serial-km

Synchronous Python client for SerialKeyboardMouseController hardware using the
current `serial_hid_core` typed-reply protocol.

The library is intentionally blocking and explicit: opening or closing a
controller does not release keys, release mouse buttons, or reset the device.
Use `release_all()` when you want that cleanup behavior.

## Setup

```powershell
cd serial_km_py
.\.venv\Scripts\python -m pip install -e ".[dev]"
```

## Basic Usage

```python
from serial_km import Key, MouseButton, SerialKeyboardMouse

with SerialKeyboardMouse.open(port="COM3", baudrate=6000000, rtscts=True) as km:
    print(km.host_status())
    print(km.keyboard_lock_state())

    km.set_mouse_resolution(1920, 1080)
    km.move_to(960, 540)
    km.click(MouseButton.Left)
    km.type_text("hello\n")
    km.hotkey(Key.LeftControl, Key.A)
```

Coordinates are 1-based because the firmware validates absolute mouse `x` and
`y` in `[1, current_resolution]`.

## API Shape

- `SerialKeyboardMouse.open(...)` opens a pyserial transport. Defaults are
  `COM3`, `6000000` baud, `rtscts=True`, `timeout=0.4`, `retries=3`.
- Mouse: `set_mouse_resolution`, `move_to`, `move_relative`, `scroll`,
  `mouse_press`, `mouse_release`, `click`, `release_all_mouse`.
- Keyboard: `key_press`, `key_release`, `tap_key`, `hotkey`, `type_text`,
  `release_all_keys`.
- Queries/control: `keyboard_lock_state`, `host_status`, `reset_device`,
  `release_all`.

`key_press` and `mouse_press` intentionally hold the key/button until the caller
releases it. Composite helpers release what they press.

## Hardware Smoke Test

```powershell
cd serial_km_py
.\.venv\Scripts\python scripts\hardware_smoke.py --port COM3 --baudrate 6000000 --rtscts --width 1920 --height 1080 --capture "HDPro 2"
```

The default smoke test queries status, sets mouse resolution, moves the pointer
to safe coordinates, and tries to read a frame from the capture card. Typing and
clicking are opt-in flags.

## Tests

```powershell
cd serial_km_py
.\.venv\Scripts\python -m pytest
```
