Import("env")
board_config = env.BoardConfig()
board_config.update("build.hwids", [
  ["0x046D", "0xC52B"]
])
board_config.update("build.usb_product", "USB Receiver")
board_config.update("vendor", "Logitech")