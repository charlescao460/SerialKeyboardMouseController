"""Synchronous Python client for SerialKeyboardMouseController hardware."""

from .controller import SerialKeyboardMouse
from .enums import HostStatus, Key, KeyboardLock, MouseButton, ResetMode
from .errors import (
    DeviceOperationError,
    SerialConnectionError,
    SerialKMError,
    SerialProtocolError,
    SerialTimeoutError,
)

__all__ = [
    "DeviceOperationError",
    "HostStatus",
    "Key",
    "KeyboardLock",
    "MouseButton",
    "ResetMode",
    "SerialConnectionError",
    "SerialKMError",
    "SerialKeyboardMouse",
    "SerialProtocolError",
    "SerialTimeoutError",
]
