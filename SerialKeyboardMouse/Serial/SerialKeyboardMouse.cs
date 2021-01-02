using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SerialKeyboardMouse.Serial
{
    public class SerialKeyboardMouse
    {
        private readonly ReliableFrameSender _sender;

        public SerialKeyboardMouse(ISerialAdaptor serial)
        {
            _sender = new ReliableFrameSender(serial);
        }
    }
}
