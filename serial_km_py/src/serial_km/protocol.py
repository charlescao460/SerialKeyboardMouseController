"""Low-level serial HID protocol helpers."""

from __future__ import annotations

from dataclasses import dataclass
from enum import IntEnum

from .errors import SerialProtocolError

START = 0xAB
MIN_DATA_LENGTH = 4
MAX_DATA_LENGTH = 16
MAX_FRAME_LENGTH = MAX_DATA_LENGTH + 2
RELEASE_ALL = 0x00
MAX_RESOLUTION = 32767


class FrameType(IntEnum):
    RelMouseMove = 0xA0
    MouseMoveAbs = 0xAA
    MouseScroll = 0xAB
    MousePress = 0xAC
    MouseRelease = 0xAD
    MouseResolution = 0xAE
    KeyPress = 0xBB
    KeyRelease = 0xBC
    QueryKeyboardLock = 0xC0
    QueryHostStatus = 0xC1
    Reset = 0xF0


class ReplyType(IntEnum):
    OpOk = 0x01
    OpError = 0x02
    Invalid = 0x03
    Timeout = 0x04
    KeyboardLock = 0x20
    HostStatus = 0x21


class Status(IntEnum):
    Ok = 0
    NoData = 1
    InvalidStart = 2
    InvalidLength = 3
    Timeout = 4
    CrcMismatch = 5
    UnsupportedFrame = 6
    InvalidPayload = 7
    OutOfRange = 8
    IoError = 9
    BadDependency = 10


@dataclass(frozen=True)
class Reply:
    frame_id: int
    type: ReplyType
    payload: bytes


def crc8(data: bytes | bytearray | memoryview) -> int:
    """Compute protocol CRC-8: poly 0x07, init 0x00, no reflection."""

    crc = 0
    for value in bytes(data):
        crc ^= value
        for _ in range(8):
            if crc & 0x80:
                crc = ((crc << 1) ^ 0x07) & 0xFF
            else:
                crc = (crc << 1) & 0xFF
    return crc


def pack_request(frame_id: int, frame_type: FrameType | int, payload: bytes = b"") -> bytes:
    return _pack_frame(frame_id, int(frame_type), payload)


def pack_reply(frame_id: int, reply_type: ReplyType | int, payload: bytes = b"") -> bytes:
    return _pack_frame(frame_id, int(reply_type), payload)


def parse_reply(frame: bytes | bytearray | memoryview, expected_frame_id: int | None = None) -> Reply:
    raw = bytes(frame)
    if len(raw) < MIN_DATA_LENGTH + 2:
        raise SerialProtocolError(f"Reply too short: {len(raw)} bytes")
    if raw[0] != START:
        raise SerialProtocolError(f"Invalid reply start byte: 0x{raw[0]:02X}")

    length = raw[1]
    if length < MIN_DATA_LENGTH or length > MAX_DATA_LENGTH:
        raise SerialProtocolError(f"Invalid reply length: {length}")
    if len(raw) != length + 2:
        raise SerialProtocolError(f"Reply length mismatch: header={length}, actual={len(raw) - 2}")

    body = raw[2:]
    expected_crc = body[-1]
    actual_crc = crc8(body[:-1])
    if actual_crc != expected_crc:
        raise SerialProtocolError(f"Reply CRC mismatch: expected 0x{expected_crc:02X}, got 0x{actual_crc:02X}")

    frame_id = int.from_bytes(body[0:2], "little")
    if expected_frame_id is not None and frame_id != (expected_frame_id & 0xFFFF):
        raise SerialProtocolError(f"Reply frame id mismatch: expected {expected_frame_id & 0xFFFF}, got {frame_id}")

    try:
        reply_type = ReplyType(body[2])
    except ValueError as exc:
        raise SerialProtocolError(f"Unknown reply type: 0x{body[2]:02X}") from exc

    return Reply(frame_id=frame_id, type=reply_type, payload=body[3:-1])


def status_name(status: int) -> str:
    try:
        return Status(status).name
    except ValueError:
        return f"UNKNOWN_STATUS_{int(status)}"


def _pack_frame(frame_id: int, frame_type: int, payload: bytes) -> bytes:
    if not 0 <= int(frame_id) <= 0xFFFF:
        raise ValueError("frame_id must be in range 0..65535")
    if not 0 <= int(frame_type) <= 0xFF:
        raise ValueError("frame_type must be in range 0..255")

    payload = bytes(payload)
    body_without_crc = frame_id.to_bytes(2, "little") + bytes([frame_type]) + payload
    length = len(body_without_crc) + 1
    if length < MIN_DATA_LENGTH or length > MAX_DATA_LENGTH:
        raise ValueError(f"Invalid protocol payload length: {len(payload)}")

    return bytes([START, length]) + body_without_crc + bytes([crc8(body_without_crc)])
