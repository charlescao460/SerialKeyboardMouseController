import pytest

from serial_km.errors import SerialProtocolError
from serial_km.protocol import FrameType, ReplyType, crc8, pack_reply, pack_request, parse_reply


def test_protocol_examples_match_core_docs() -> None:
    assert pack_request(0x1234, FrameType.MousePress, b"\x01") == bytes.fromhex("AB 05 34 12 AC 01 66")
    assert pack_reply(0x1234, ReplyType.OpOk) == bytes.fromhex("AB 04 34 12 01 30")

    assert pack_request(0x1234, FrameType.QueryKeyboardLock) == bytes.fromhex("AB 04 34 12 C0 79")
    assert pack_reply(0x1234, ReplyType.KeyboardLock, b"\x03") == bytes.fromhex("AB 05 34 12 20 03 22")

    assert pack_request(0x1235, FrameType.QueryHostStatus) == bytes.fromhex("AB 04 35 12 C1 15")
    assert pack_reply(0x1235, ReplyType.HostStatus, b"\x05") == bytes.fromhex("AB 05 35 12 21 05 33")

    assert pack_request(0x1236, FrameType.Reset, b"\x00") == bytes.fromhex("AB 05 36 12 F0 00 BD")
    assert pack_reply(0x1236, ReplyType.OpOk) == bytes.fromhex("AB 04 36 12 01 E6")


def test_crc8_known_body() -> None:
    body = bytes.fromhex("34 12 AC 01")
    assert crc8(body) == 0x66


def test_parse_reply_validates_frame_id_and_crc() -> None:
    reply = pack_reply(7, ReplyType.OpOk)
    assert parse_reply(reply, expected_frame_id=7).type == ReplyType.OpOk

    with pytest.raises(SerialProtocolError, match="frame id mismatch"):
        parse_reply(reply, expected_frame_id=8)

    corrupted = bytearray(reply)
    corrupted[-1] ^= 0x01
    with pytest.raises(SerialProtocolError, match="CRC mismatch"):
        parse_reply(corrupted, expected_frame_id=7)


def test_parse_reply_rejects_bad_start_and_length() -> None:
    reply = bytearray(pack_reply(1, ReplyType.OpOk))
    reply[0] = 0x00
    with pytest.raises(SerialProtocolError, match="start byte"):
        parse_reply(reply, expected_frame_id=1)

    reply = bytearray(pack_reply(1, ReplyType.OpOk))
    reply[1] = 0xFF
    with pytest.raises(SerialProtocolError, match="length"):
        parse_reply(reply, expected_frame_id=1)
