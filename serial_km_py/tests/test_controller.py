import pytest

from serial_km import DeviceOperationError, HostStatus, Key, MouseButton, SerialKeyboardMouse
from serial_km.errors import SerialProtocolError, SerialTimeoutError
from serial_km.protocol import FrameType, ReplyType, Status, pack_reply


class FakeTransport:
    def __init__(self, responder=None, *, max_chunk: int | None = None):
        self.responder = responder
        self.max_chunk = max_chunk
        self.incoming = bytearray()
        self.written: list[bytes] = []
        self.reset_count = 0
        self.closed = False

    def write(self, data: bytes) -> None:
        frame = bytes(data)
        self.written.append(frame)
        if self.responder is not None:
            response = self.responder(frame, len(self.written) - 1)
            if response:
                self.incoming.extend(response)

    def read(self, size: int) -> bytes:
        if not self.incoming:
            return b""
        count = min(size, len(self.incoming))
        if self.max_chunk is not None:
            count = min(count, self.max_chunk)
        data = bytes(self.incoming[:count])
        del self.incoming[:count]
        return data

    def reset_input_buffer(self) -> None:
        self.reset_count += 1
        self.incoming.clear()

    def close(self) -> None:
        self.closed = True


def reply_for(frame: bytes, reply_type: ReplyType = ReplyType.OpOk, payload: bytes = b"") -> bytes:
    frame_id = int.from_bytes(frame[2:4], "little")
    return pack_reply(frame_id, reply_type, payload)


def default_responder(frame: bytes, attempt: int) -> bytes:
    frame_type = frame[4]
    if frame_type == FrameType.QueryKeyboardLock:
        return reply_for(frame, ReplyType.KeyboardLock, b"\x03")
    if frame_type == FrameType.QueryHostStatus:
        return reply_for(frame, ReplyType.HostStatus, b"\x05")
    return reply_for(frame)


def new_controller(transport: FakeTransport) -> SerialKeyboardMouse:
    return SerialKeyboardMouse(transport, timeout=0.01, retries=1, retry_delay=0.0)


def test_controller_sends_all_command_types_and_updates_state() -> None:
    transport = FakeTransport(default_responder)
    km = new_controller(transport)

    km.set_mouse_resolution(1920, 1080)
    km.move_to(960, 540)
    km.move_relative(-1, 127)
    km.scroll(-3)
    km.mouse_press(MouseButton.Left | MouseButton.Right)
    km.mouse_release(MouseButton.Right)
    km.release_all_mouse()
    km.key_press(Key.A)
    km.key_release(Key.A)
    km.release_all_keys()
    assert km.keyboard_lock_state().value == 0x03
    assert km.host_status() == (HostStatus.Configured | HostStatus.SofActive)
    km.reset_device()

    assert [frame[4] for frame in transport.written] == [
        FrameType.MouseResolution,
        FrameType.MouseMoveAbs,
        FrameType.RelMouseMove,
        FrameType.MouseScroll,
        FrameType.MousePress,
        FrameType.MouseRelease,
        FrameType.MouseRelease,
        FrameType.KeyPress,
        FrameType.KeyRelease,
        FrameType.KeyRelease,
        FrameType.QueryKeyboardLock,
        FrameType.QueryHostStatus,
        FrameType.Reset,
    ]
    assert transport.written[0][5:9] == bytes([0x80, 0x07, 0x38, 0x04])
    assert transport.written[2][5:9] == bytes([0xFF, 0xFF, 0x7F, 0x00])
    assert transport.written[3][5] == 0xFD
    assert km.mouse_resolution == (1920, 1080)
    assert km.last_mouse_position == (959, 667)
    assert km.pressed_mouse_buttons == MouseButton(0)
    assert km.pressed_keys == frozenset()


def test_stale_bytes_and_partial_reads_are_tolerated() -> None:
    def responder(frame: bytes, attempt: int) -> bytes:
        return b"stale bytes" + reply_for(frame, ReplyType.HostStatus, b"\x01")

    transport = FakeTransport(responder, max_chunk=1)
    km = new_controller(transport)

    assert km.host_status() == HostStatus.Configured
    assert len(transport.written) == 1


def test_retry_after_bad_crc_then_success() -> None:
    def responder(frame: bytes, attempt: int) -> bytes:
        reply = bytearray(reply_for(frame))
        if attempt == 0:
            reply[-1] ^= 0xFF
        return bytes(reply)

    transport = FakeTransport(responder)
    km = SerialKeyboardMouse(transport, timeout=0.01, retries=2, retry_delay=0.0)

    km.key_press(Key.A)
    assert len(transport.written) == 2
    assert transport.reset_count == 1


def test_device_operation_error_is_not_retried() -> None:
    def responder(frame: bytes, attempt: int) -> bytes:
        return reply_for(frame, ReplyType.OpError, bytes([Status.OutOfRange]))

    transport = FakeTransport(responder)
    km = SerialKeyboardMouse(transport, timeout=0.01, retries=3, retry_delay=0.0)

    with pytest.raises(DeviceOperationError) as exc_info:
        km.move_relative(1, 1)
    assert exc_info.value.status == Status.OutOfRange
    assert len(transport.written) == 1


def test_wrong_frame_id_and_timeout_raise() -> None:
    def wrong_id(frame: bytes, attempt: int) -> bytes:
        frame_id = (int.from_bytes(frame[2:4], "little") + 1) & 0xFFFF
        return pack_reply(frame_id, ReplyType.OpOk)

    with pytest.raises(SerialProtocolError, match="frame id mismatch"):
        new_controller(FakeTransport(wrong_id)).key_press(Key.A)

    with pytest.raises(SerialTimeoutError):
        new_controller(FakeTransport(lambda frame, attempt: b"")).key_press(Key.A)


def test_local_validation_prevents_serial_writes() -> None:
    transport = FakeTransport(default_responder)
    km = new_controller(transport)

    invalid_calls = [
        lambda: km.set_mouse_resolution(0, 1080),
        lambda: km.move_to(0, 1),
        lambda: km.move_relative(128, 0),
        lambda: km.scroll(-129),
        lambda: km.mouse_press(0),
        lambda: km.mouse_press(8),
        lambda: km.key_press(256),
    ]

    for call in invalid_calls:
        with pytest.raises(ValueError):
            call()
    assert transport.written == []


def test_composite_helpers_release_what_they_press() -> None:
    transport = FakeTransport(default_responder)
    km = new_controller(transport)

    km.click(MouseButton.Left)
    km.tap_key(Key.A)
    km.hotkey(Key.LeftControl, Key.C)
    km.type_text("Aa!")

    assert km.pressed_mouse_buttons == MouseButton(0)
    assert km.pressed_keys == frozenset()
    assert transport.written[0][4] == FrameType.MousePress
    assert transport.written[1][4] == FrameType.MouseRelease


def test_close_has_no_hid_side_effect() -> None:
    transport = FakeTransport(default_responder)
    km = new_controller(transport)
    km.close()

    assert transport.closed
    assert transport.written == []
