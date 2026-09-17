"""pyserial-backed transport adapter."""

from __future__ import annotations

from .errors import SerialConnectionError


class SerialPortTransport:
    """Small blocking transport wrapper around pyserial."""

    def __init__(
        self,
        port: str,
        *,
        baudrate: int = 6000000,
        timeout: float = 0.4,
        write_timeout: float | None = None,
        rtscts: bool = True,
    ):
        try:
            import serial
        except ImportError as exc:
            raise SerialConnectionError("pyserial is required; install serial-km with its runtime dependencies") from exc

        try:
            self._serial = serial.Serial(
                port=port,
                baudrate=baudrate,
                timeout=timeout,
                write_timeout=timeout if write_timeout is None else write_timeout,
                rtscts=rtscts,
            )
        except Exception as exc:  # pyserial raises several OS-specific exception types.
            raise SerialConnectionError(f"Failed to open serial port {port!r}: {exc}") from exc

    def write(self, data: bytes) -> None:
        try:
            written = self._serial.write(data)
            self._serial.flush()
        except Exception as exc:
            raise SerialConnectionError(f"Serial write failed: {exc}") from exc
        if written != len(data):
            raise SerialConnectionError(f"Serial write incomplete: wrote {written} of {len(data)} bytes")

    def read(self, size: int) -> bytes:
        try:
            return bytes(self._serial.read(size))
        except Exception as exc:
            raise SerialConnectionError(f"Serial read failed: {exc}") from exc

    def reset_input_buffer(self) -> None:
        try:
            self._serial.reset_input_buffer()
        except Exception as exc:
            raise SerialConnectionError(f"Failed to reset serial input buffer: {exc}") from exc

    def close(self) -> None:
        self._serial.close()

    def __enter__(self) -> SerialPortTransport:
        return self

    def __exit__(self, exc_type, exc, tb) -> None:
        self.close()
