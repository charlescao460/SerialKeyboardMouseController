Import("env")
board_config = env.BoardConfig()
board_config.update("build.hwids", [
  ["0x046D", "0xC500"]
])
board_config.update("build.usb_product", "USB Keyboard")
board_config.update("vendor", "Logitech")