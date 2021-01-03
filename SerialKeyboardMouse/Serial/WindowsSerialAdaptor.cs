using System;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;

namespace SerialKeyboardMouse.Serial
{
    public class WindowsSerialAdaptor : ISerialAdaptor
    {
        private const int ReadTimeout = 10;
        private const int WriteTimeout = 10000;
        private readonly SerialPort _serialPort;

        public WindowsSerialAdaptor(string portName)
        {
            _serialPort = new SerialPort(portName, SerialSymbols.BaudRate, Parity.None);
            _serialPort.Open();
            _serialPort.BaseStream.ReadTimeout = ReadTimeout;
            _serialPort.ReadTimeout = ReadTimeout;
            _serialPort.BaseStream.WriteTimeout = WriteTimeout;
            _serialPort.WriteTimeout = WriteTimeout;
            _serialPort.DataReceived += (o, e) => { SerialDataAvailableEvent?.Invoke(this); };
        }

        public event ISerialAdaptor.SerialDataAvailable SerialDataAvailableEvent;

        public byte ReadByte(out bool timeout)
        {
            byte ret = 0;
            try
            {
                ret = (byte)_serialPort.ReadByte();
            }
            catch (TimeoutException)
            {
                timeout = true;
                return ret;
            }
            timeout = false;
            return ret;
        }

        public void WriteByte(byte b)
        {
            Span<byte> toSend = stackalloc byte[1];
            _serialPort.BaseStream.Write(toSend);
        }

        public int Read(Memory<byte> memory)
        {
            return _serialPort.BaseStream.Read(memory.Span);
        }

        public void Write(Memory<byte> memory)
        {
            _serialPort.BaseStream.Write(memory.Span);
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