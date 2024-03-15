using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace SerialKeyboardMouse
{
    /// <summary>
    /// Exception when the serial device not responding or not work as expected.
    /// </summary>
    public class SerialDeviceException : Exception
    {
        public SerialDeviceException()
        {
        }

        public SerialDeviceException(string message) : base(message)
        {
        }

        public SerialDeviceException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
