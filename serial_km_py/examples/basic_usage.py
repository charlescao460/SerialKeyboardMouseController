from serial_km import Key, MouseButton, SerialKeyboardMouse


def main() -> None:
    with SerialKeyboardMouse.open(port="COM3", baudrate=6000000, rtscts=True) as km:
        print(f"host status: {km.host_status()}")
        print(f"keyboard locks: {km.keyboard_lock_state()}")

        km.set_mouse_resolution(1920, 1080)
        km.move_to(960, 540)
        km.click(MouseButton.Left)
        km.type_text("hello from serial-km\n")
        km.hotkey(Key.LeftControl, Key.A)


if __name__ == "__main__":
    main()
