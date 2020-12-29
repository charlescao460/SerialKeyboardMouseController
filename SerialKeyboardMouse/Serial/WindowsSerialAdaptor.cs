using System;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;

namespace SerialKeyboardMouse.Serial
{
    public class WindowsSerialAdaptor : ISerialAdaptor
    {
        private const int ReadTimeout = 10;
        private SerialPort _serialPort;

        public WindowsSerialAdaptor(string portName)
        {
            _serialPort = new SerialPort(portName, SerialSymbols.BaudRate, Parity.None);
            _serialPort.Open();
            _serialPort.DataReceived += (o, e) =>
            {
                SerialDataAvailableEvent?.Invoke(this);
            };
        }

        public event ISerialAdaptor.SerialDataAvailable SerialDataAvailableEvent;

        public byte ReadByte()
        {
            int oldTimeout = _serialPort.ReadTimeout;
            _serialPort.ReadTimeout = SerialPort.InfiniteTimeout;
            byte ret = (byte)_serialPort.ReadByte();
            _serialPort.ReadTimeout = oldTimeout;
            return ret;
        }

        public int AvailableBytes => (int)_serialPort.BaseStream.Length;

        public async Task<int> AsyncRead(int length, byte[] memory, CancellationToken token = default)
        {
            return await _serialPort.BaseStream.ReadAsync(memory, 0, length, token);
        }

        public async Task AsyncWrite(byte[] memory, CancellationToken token = default)
        {
            await _serialPort.BaseStream.WriteAsync(memory, 0, memory.Length, token);
        }

        public async Task<byte> AsyncReadByte(CancellationToken token = default)
        {
            byte[] arr = new byte[1];
            await AsyncRead(1, arr, token);
            return arr[0];
        }

        public async Task AsyncWriteByte(byte b, CancellationToken token = default)
        {
            byte[] arr = new byte[1];
            arr[0] = b;
            await AsyncWrite(arr, token);
        }
    }
}
