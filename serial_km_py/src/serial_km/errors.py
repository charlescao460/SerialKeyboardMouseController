"""Exception types exposed by serial_km."""


class SerialKMError(Exception):
    """Base class for serial keyboard/mouse errors."""


class SerialConnectionError(SerialKMError):
    """The serial transport failed to open, read, or write."""


class SerialTimeoutError(SerialKMError):
    """The device did not provide a complete reply before the timeout."""


class SerialProtocolError(SerialKMError):
    """The device reply did not match the serial HID protocol."""


class DeviceOperationError(SerialKMError):
    """The firmware accepted a frame but rejected the requested operation."""

    def __init__(self, status: int, status_name: str | None = None):
        self.status = int(status)
        self.status_name = status_name or f"UNKNOWN_STATUS_{self.status}"
        super().__init__(f"Device operation failed: {self.status_name} ({self.status})")
