from __future__ import annotations

import argparse
import time
from pathlib import Path

from serial_km import MouseButton, SerialKeyboardMouse


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Hardware smoke test for serial-km.")
    parser.add_argument("--port", default="COM3")
    parser.add_argument("--baudrate", type=int, default=6000000)
    parser.add_argument("--rtscts", action=argparse.BooleanOptionalAction, default=True)
    parser.add_argument("--timeout", type=float, default=0.4)
    parser.add_argument("--retries", type=int, default=3)
    parser.add_argument("--width", type=int, default=1920)
    parser.add_argument("--height", type=int, default=1080)
    parser.add_argument("--capture", default="HDPro 2", help="OpenCV capture source name/index, or empty to skip.")
    parser.add_argument("--with-click", action="store_true", help="Also perform a left click after movement.")
    parser.add_argument("--type-text", default="", help="Optional text to type on the target.")
    parser.add_argument("--save-frame", type=Path, default=None, help="Optional path to save one captured frame.")
    parser.add_argument("--capture-scan-limit", type=int, default=8, help="Numeric OpenCV indexes to try if name open fails.")
    return parser.parse_args()


def capture_frame(source: str, save_path: Path | None, scan_limit: int) -> None:
    if not source:
        return

    try:
        import cv2
    except ImportError:
        print("OpenCV is not installed; skipping capture check.")
        return

    capture_source: int | str
    try:
        capture_source = int(source)
    except ValueError:
        capture_source = source

    cap = cv2.VideoCapture(capture_source, cv2.CAP_DSHOW)
    if not cap.isOpened() and isinstance(capture_source, str):
        cap.release()
        cap = None
        for index in range(max(0, scan_limit)):
            candidate = cv2.VideoCapture(index, cv2.CAP_DSHOW)
            if candidate.isOpened():
                cap = candidate
                print(f"Capture source {source!r} did not open by name; using OpenCV index {index}.")
                break
            candidate.release()
        if cap is None:
            print(f"Capture source {source!r} did not open by name and no numeric fallback opened.")
            return
    elif not cap.isOpened():
        print(f"Capture source {source!r} did not open.")
        return

    try:
        cap.set(cv2.CAP_PROP_FRAME_WIDTH, 1920)
        cap.set(cv2.CAP_PROP_FRAME_HEIGHT, 1080)
        cap.set(cv2.CAP_PROP_FPS, 60)
        ok, frame = cap.read()
        if not ok or frame is None:
            print(f"Capture source {source!r} opened but did not return a frame.")
            return
        print(f"Captured frame: {frame.shape[1]}x{frame.shape[0]}")
        if save_path is not None:
            cv2.imwrite(str(save_path), frame)
            print(f"Saved captured frame to {save_path}")
    finally:
        cap.release()


def safe_points(width: int, height: int) -> list[tuple[int, int]]:
    return [
        (max(1, width // 2), max(1, height // 2)),
        (max(1, width // 3), max(1, height // 3)),
        (max(1, (width * 2) // 3), max(1, (height * 2) // 3)),
    ]


def main() -> None:
    args = parse_args()
    print(f"Opening {args.port} at {args.baudrate} baud, rtscts={args.rtscts}")

    with SerialKeyboardMouse.open(
        port=args.port,
        baudrate=args.baudrate,
        rtscts=args.rtscts,
        timeout=args.timeout,
        retries=args.retries,
    ) as km:
        host_status = km.host_status()
        keyboard_locks = km.keyboard_lock_state()
        print(f"Host status: {host_status}")
        print(f"Keyboard locks: {keyboard_locks}")

        km.set_mouse_resolution(args.width, args.height)
        print(f"Mouse resolution set to {args.width}x{args.height}")

        for x, y in safe_points(args.width, args.height):
            km.move_to(x, y)
            print(f"Moved mouse to {x},{y}")
            time.sleep(0.05)

        if args.with_click:
            km.click(MouseButton.Left)
            print("Clicked left mouse button")

        if args.type_text:
            km.type_text(args.type_text, inter_key_delay=0.02)
            print(f"Typed {len(args.type_text)} characters")

    capture_frame(args.capture, args.save_frame, args.capture_scan_limit)


if __name__ == "__main__":
    main()
