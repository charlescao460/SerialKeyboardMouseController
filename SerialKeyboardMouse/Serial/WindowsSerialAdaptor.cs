using System;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;

namespace SerialKeyboardMouse.Serial
{
    public class WindowsSerialAdaptor : ISerialAdaptor
    {
        private const int ReadTimeout = 10;
        private readonly SerialPort _serialPort;

        public WindowsSerialAdaptor(string portName)
        {
            _serialPort = new SerialPort(portName, SerialSymbols.BaudRate, Parity.None);
            _serialPort.Open();
            _serialPort.BaseStream.ReadTimeout = ReadTimeout;
            _serialPort.ReadTimeout = ReadTimeout;
            _serialPort.DataReceived += (o, e) => { SerialDataAvailableEvent?.Invoke(this); };
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

        public ValueTask<int> AsyncRead(Memory<byte> memory, CancellationToken token = default)
        {
            return _serialPort.BaseStream.ReadAsync(memory, token);
        }

        public ValueTask AsyncWrite(Memory<byte> memory, CancellationToken token = default)
        {
            return _serialPort.BaseStream.WriteAsync(memory, token);
        }

        public async ValueTask<byte> AsyncReadByte(CancellationToken token = default)
        {
            byte[] arr = new byte[1];
            await AsyncRead(arr, token).ConfigureAwait(false);
            return arr[0];
        }

        public async ValueTask AsyncWriteByte(byte b, CancellationToken token = default)
        {
            byte[] arr = new byte[1];
            arr[0] = b;
            await AsyncWrite(arr, token).ConfigureAwait(false);
        }

        public void Dispose()
        {
            _serialPort?.Dispose();
        }
    }
}