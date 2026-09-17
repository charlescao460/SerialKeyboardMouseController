"""High-level synchronous keyboard and mouse controller."""

from __future__ import annotations

import threading
import time
from typing import Protocol

from .enums import HostStatus, Key, KeyboardLock, MouseButton, ResetMode
from .errors import (
    DeviceOperationError,
    SerialConnectionError,
    SerialKMError,
    SerialProtocolError,
    SerialTimeoutError,
)
from .protocol import (
    MAX_RESOLUTION,
    MIN_DATA_LENGTH,
    MAX_DATA_LENGTH,
    RELEASE_ALL,
    START,
    FrameType,
    Reply,
    ReplyType,
    pack_request,
    parse_reply,
    status_name,
)
from .transport import SerialPortTransport


class _Transport(Protocol):
    def write(self, data: bytes) -> None:
        ...

    def read(self, size: int) -> bytes:
        ...

    def reset_input_buffer(self) -> None:
        ...

    def close(self) -> None:
        ...


_ASCII_TO_KEY: dict[str, tuple[bool, Key]] = {
    " ": (False, Key.Space),
    "\t": (False, Key.Tab),
    "\r": (False, Key.Enter),
    "\n": (False, Key.Enter),
    "0": (False, Key.Num0),
    "1": (False, Key.Num1),
    "2": (False, Key.Num2),
    "3": (False, Key.Num3),
    "4": (False, Key.Num4),
    "5": (False, Key.Num5),
    "6": (False, Key.Num6),
    "7": (False, Key.Num7),
    "8": (False, Key.Num8),
    "9": (False, Key.Num9),
    "!": (True, Key.Num1),
    "@": (True, Key.Num2),
    "#": (True, Key.Num3),
    "$": (True, Key.Num4),
    "%": (True, Key.Num5),
    "^": (True, Key.Num6),
    "&": (True, Key.Num7),
    "*": (True, Key.Num8),
    "(": (True, Key.Num9),
    ")": (True, Key.Num0),
    "-": (False, Key.Minus),
    "_": (True, Key.Minus),
    "=": (False, Key.Equals),
    "+": (True, Key.Equals),
    "[": (False, Key.LeftBracket),
    "{": (True, Key.LeftBracket),
    "]": (False, Key.RightBracket),
    "}": (True, Key.RightBracket),
    "\\": (False, Key.Backslash),
    "|": (True, Key.Backslash),
    ";": (False, Key.Semicolon),
    ":": (True, Key.Semicolon),
    "'": (False, Key.Quote),
    '"': (True, Key.Quote),
    "`": (False, Key.GraveAccent),
    "~": (True, Key.GraveAccent),
    ",": (False, Key.Comma),
    "<": (True, Key.Comma),
    ".": (False, Key.Period),
    ">": (True, Key.Period),
    "/": (False, Key.Slash),
    "?": (True, Key.Slash),
}


class SerialKeyboardMouse:
    """Blocking controller for SerialKeyboardMouseController hardware."""

    def __init__(
        self,
        transport: _Transport,
        *,
        timeout: float = 0.4,
        retries: int = 3,
        retry_delay: float = 0.12,
        close_transport: bool = True,
    ):
        if timeout <= 0:
            raise ValueError("timeout must be positive")
        if retries < 1:
            raise ValueError("retries must be at least 1")

        self._transport = transport
        self._timeout = float(timeout)
        self._retries = int(retries)
        self._retry_delay = float(retry_delay)
        self._close_transport = close_transport
        self._lock = threading.Lock()
        self._next_id = 0
        self._closed = False
        self._mouse_resolution = (MAX_RESOLUTION, MAX_RESOLUTION)
        self._last_mouse_position: tuple[int, int] | None = None
        self._pressed_keys: set[int] = set()
        self._pressed_mouse_buttons = MouseButton(0)

    @classmethod
    def open(
        cls,
        port: str = "COM3",
        *,
        baudrate: int = 6000000,
        rtscts: bool = True,
        timeout: float = 0.4,
        retries: int = 3,
        write_timeout: float | None = None,
        retry_delay: float = 0.12,
    ) -> SerialKeyboardMouse:
        transport = SerialPortTransport(
            port,
            baudrate=baudrate,
            timeout=timeout,
            write_timeout=write_timeout,
            rtscts=rtscts,
        )
        return cls(transport, timeout=timeout, retries=retries, retry_delay=retry_delay, close_transport=True)

    @property
    def mouse_resolution(self) -> tuple[int, int]:
        return self._mouse_resolution

    @property
    def last_mouse_position(self) -> tuple[int, int] | None:
        return self._last_mouse_position

    @property
    def pressed_keys(self) -> frozenset[int]:
        return frozenset(self._pressed_keys)

    @property
    def pressed_mouse_buttons(self) -> MouseButton:
        return self._pressed_mouse_buttons

    def set_mouse_resolution(self, width: int, height: int) -> None:
        self._validate_resolution(width, height)
        payload = int(width).to_bytes(2, "little") + int(height).to_bytes(2, "little")
        self._send_expect(FrameType.MouseResolution, payload)
        self._mouse_resolution = (int(width), int(height))

    def move_to(self, x: int, y: int) -> None:
        width, height = self._mouse_resolution
        if x <= 0 or y <= 0 or x > width or y > height:
            raise ValueError(f"mouse coordinate ({x}, {y}) is outside the current resolution ({width}, {height})")
        payload = int(x).to_bytes(2, "little") + int(y).to_bytes(2, "little")
        self._send_expect(FrameType.MouseMoveAbs, payload)
        self._last_mouse_position = (int(x), int(y))

    def move_relative(self, dx: int, dy: int) -> None:
        self._validate_int8(dx, "dx")
        self._validate_int8(dy, "dy")
        payload = int(dx).to_bytes(2, "little", signed=True) + int(dy).to_bytes(2, "little", signed=True)
        self._send_expect(FrameType.RelMouseMove, payload)
        if self._last_mouse_position is not None:
            x, y = self._last_mouse_position
            self._last_mouse_position = (x + int(dx), y + int(dy))

    def scroll(self, steps: int) -> None:
        self._validate_int8(steps, "steps")
        self._send_expect(FrameType.MouseScroll, int(steps).to_bytes(1, "little", signed=True))

    def mouse_press(self, button: MouseButton | int) -> None:
        mask = self._mouse_button_mask(button)
        if mask == 0:
            raise ValueError("button must not be zero; use release_all_mouse() to release every button")
        self._send_expect(FrameType.MousePress, bytes([mask]))
        self._pressed_mouse_buttons |= MouseButton(mask)

    def mouse_release(self, button: MouseButton | int) -> None:
        mask = self._mouse_button_mask(button)
        if mask == 0:
            raise ValueError("button must not be zero; use release_all_mouse() to release every button")
        self._send_expect(FrameType.MouseRelease, bytes([mask]))
        self._pressed_mouse_buttons &= ~MouseButton(mask)

    def click(self, button: MouseButton | int = MouseButton.Left, *, hold_seconds: float = 0.0) -> None:
        self.mouse_press(button)
        try:
            if hold_seconds > 0:
                time.sleep(hold_seconds)
        finally:
            self.mouse_release(button)

    def release_all_mouse(self) -> None:
        self._send_expect(FrameType.MouseRelease, bytes([RELEASE_ALL]))
        self._pressed_mouse_buttons = MouseButton(0)

    def key_press(self, key: Key | int) -> None:
        code = self._key_code(key)
        self._send_expect(FrameType.KeyPress, bytes([code]))
        self._pressed_keys.add(code)

    def key_release(self, key: Key | int) -> None:
        code = self._key_code(key)
        if code == RELEASE_ALL:
            raise ValueError("key must not be zero; use release_all_keys() to release every key")
        self._send_expect(FrameType.KeyRelease, bytes([code]))
        self._pressed_keys.discard(code)

    def tap_key(self, key: Key | int, *, hold_seconds: float = 0.0) -> None:
        self.key_press(key)
        try:
            if hold_seconds > 0:
                time.sleep(hold_seconds)
        finally:
            self.key_release(key)

    def hotkey(self, *keys: Key | int, hold_seconds: float = 0.0) -> None:
        if not keys:
            raise ValueError("at least one key is required")

        pressed: list[Key | int] = []
        try:
            for key in keys:
                self.key_press(key)
                pressed.append(key)
            if hold_seconds > 0:
                time.sleep(hold_seconds)
        finally:
            first_error: Exception | None = None
            for key in reversed(pressed):
                try:
                    self.key_release(key)
                except Exception as exc:
                    if first_error is None:
                        first_error = exc
            if first_error is not None:
                raise first_error

    def type_text(self, text: str, *, inter_key_delay: float = 0.0) -> None:
        for ch in text:
            shift, key = self._usage_for_ascii(ch)
            if shift:
                self.hotkey(Key.LeftShift, key)
            else:
                self.tap_key(key)
            if inter_key_delay > 0:
                time.sleep(inter_key_delay)

    def release_all_keys(self) -> None:
        self._send_expect(FrameType.KeyRelease, bytes([RELEASE_ALL]))
        self._pressed_keys.clear()

    def keyboard_lock_state(self) -> KeyboardLock:
        payload = self._send_expect(FrameType.QueryKeyboardLock, b"", expected=ReplyType.KeyboardLock, payload_len=1)
        return KeyboardLock(payload[0])

    def host_status(self) -> HostStatus:
        payload = self._send_expect(FrameType.QueryHostStatus, b"", expected=ReplyType.HostStatus, payload_len=1)
        return HostStatus(payload[0])

    def reset_device(self, mode: ResetMode | int = ResetMode.Normal) -> None:
        mode_value = int(mode)
        if not 0 <= mode_value <= 0xFF:
            raise ValueError("reset mode must be in range 0..255")
        self._send_expect(FrameType.Reset, bytes([mode_value]))

    def release_all(self) -> None:
        self.release_all_keys()
        self.release_all_mouse()

    def close(self) -> None:
        if self._closed:
            return
        if self._close_transport:
            self._transport.close()
        self._closed = True

    def __enter__(self) -> SerialKeyboardMouse:
        return self

    def __exit__(self, exc_type, exc, tb) -> None:
        self.close()

    def _send_expect(
        self,
        frame_type: FrameType,
        payload: bytes,
        *,
        expected: ReplyType = ReplyType.OpOk,
        payload_len: int | None = 0,
    ) -> bytes:
        reply = self._transact(frame_type, payload)
        if reply.type == ReplyType.OpError:
            if len(reply.payload) != 1:
                raise SerialProtocolError("Operation error reply must contain exactly one status byte")
            raise DeviceOperationError(reply.payload[0], status_name(reply.payload[0]))
        if reply.type != expected:
            raise SerialProtocolError(f"Unexpected reply type: expected {expected.name}, got {reply.type.name}")
        if payload_len is not None and len(reply.payload) != payload_len:
            raise SerialProtocolError(
                f"Unexpected reply payload length for {reply.type.name}: expected {payload_len}, got {len(reply.payload)}"
            )
        return reply.payload

    def _transact(self, frame_type: FrameType, payload: bytes) -> Reply:
        if self._closed:
            raise SerialConnectionError("controller is closed")

        with self._lock:
            frame_id = self._allocate_frame_id()
            frame = pack_request(frame_id, frame_type, payload)
            last_error: SerialKMError | None = None

            for attempt in range(self._retries):
                try:
                    self._transport.write(frame)
                    reply = parse_reply(self._read_reply_frame(), expected_frame_id=frame_id)
                    if reply.type in (ReplyType.Invalid, ReplyType.Timeout):
                        raise SerialProtocolError(f"Device rejected frame with {reply.type.name} reply")
                    return reply
                except DeviceOperationError:
                    raise
                except SerialKMError as exc:
                    last_error = exc
                    self._discard_input()
                    if attempt + 1 < self._retries and self._retry_delay > 0:
                        time.sleep(self._retry_delay)

            assert last_error is not None
            raise last_error

    def _read_reply_frame(self) -> bytes:
        deadline = time.monotonic() + self._timeout
        self._read_until_start(deadline)
        length_bytes = self._read_exact(1, deadline)
        length = length_bytes[0]
        if length < MIN_DATA_LENGTH or length > MAX_DATA_LENGTH:
            raise SerialProtocolError(f"Invalid reply length: {length}")
        body = self._read_exact(length, deadline)
        return bytes([START, length]) + body

    def _read_until_start(self, deadline: float) -> None:
        while True:
            if time.monotonic() >= deadline:
                raise SerialTimeoutError("Timed out waiting for reply start byte")
            data = self._transport.read(1)
            if not data:
                time.sleep(0.001)
                continue
            if data[0] == START:
                return

    def _read_exact(self, size: int, deadline: float) -> bytes:
        data = bytearray()
        while len(data) < size:
            if time.monotonic() >= deadline:
                raise SerialTimeoutError(f"Timed out reading {size} reply bytes")
            chunk = self._transport.read(size - len(data))
            if not chunk:
                time.sleep(0.001)
                continue
            data.extend(chunk)
        return bytes(data)

    def _discard_input(self) -> None:
        try:
            self._transport.reset_input_buffer()
        except SerialKMError:
            pass

    def _allocate_frame_id(self) -> int:
        frame_id = self._next_id
        self._next_id = (self._next_id + 1) & 0xFFFF
        return frame_id

    @staticmethod
    def _validate_resolution(width: int, height: int) -> None:
        if width <= 0 or height <= 0 or width > MAX_RESOLUTION or height > MAX_RESOLUTION:
            raise ValueError(f"resolution must be in range 1..{MAX_RESOLUTION} on both axes")

    @staticmethod
    def _validate_int8(value: int, name: str) -> None:
        if value < -128 or value > 127:
            raise ValueError(f"{name} must be in signed int8 range -128..127")

    @staticmethod
    def _mouse_button_mask(button: MouseButton | int) -> int:
        mask = int(button)
        allowed = int(MouseButton.Left | MouseButton.Right | MouseButton.Middle)
        if mask < 0 or mask > allowed or (mask & ~allowed):
            raise ValueError(f"unsupported mouse button mask: 0x{mask:02X}")
        return mask

    @staticmethod
    def _key_code(key: Key | int) -> int:
        code = int(key)
        if not 0 <= code <= 0xFF:
            raise ValueError("key code must be in range 0..255")
        return code

    @staticmethod
    def _usage_for_ascii(ch: str) -> tuple[bool, Key]:
        if len(ch) != 1:
            raise ValueError("expected exactly one character")
        if "a" <= ch <= "z":
            return False, Key(int(Key.A) + (ord(ch) - ord("a")))
        if "A" <= ch <= "Z":
            return True, Key(int(Key.A) + (ord(ch) - ord("A")))
        try:
            return _ASCII_TO_KEY[ch]
        except KeyError as exc:
            raise ValueError(f"unsupported character for US HID typing: {ch!r}") from exc
