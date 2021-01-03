using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using SerialKeyboardMouse.Serial;

namespace SerialKeyboardMouse
{
    public class KeyboardMouse
    {
        private readonly ReliableFrameSender _sender;

        public int MouseResolutionWidth { get; private set; }

        public int MouseResolutionHeight { get; private set; }

        public KeyboardMouse(ISerialAdaptor serial)
        {
            _sender = new ReliableFrameSender(serial);
        }

        public Task SetMouseResolution(int width, int height)
        {
            MouseResolutionWidth = width;
            MouseResolutionHeight = height;
            SerialCommandFrame frame
                = SerialCommandFrame.OfCoordinateType(SerialSymbols.FrameType.MouseResolution,
                    new Tuple<ushort, ushort>((ushort)width, (ushort)height));
            return _sender.SendFrame(frame.Bytes);
        }
    }
}
