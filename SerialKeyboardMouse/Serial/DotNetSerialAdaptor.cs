using System;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;

namespace SerialKeyboardMouse.Serial
{
    public class DotNetSerialAdaptor : ISerialAdaptor
    {
        private const int ReadTimeout = 50;
        private const int WriteTimeout = 100;
        private readonly SerialPort _serialPort;

        /// <summary>
        /// Construct an opened serial port using .NET SerialPort.
        /// </summary>
        /// <param name="portName">Port number (e.g. "COM1")</param>
        public DotNetSerialAdaptor(string portName)
        {
            _serialPort = new SerialPort(portName, SerialSymbols.BaudRate, Parity.None);
            _serialPort.Open();
            InitializePort();
        }

        /// <summary>
        /// Construct an opened serial port using .NET SerialPort.
        /// Allowing specifying whether to enable hardware flow control (RTS/CTS)
        /// </summary>
        /// <param name="portName">Port number (e.g. "COM1")</param>
        /// <param name="hardwareFlowControl">True to enable RTS/CTS hardware flow control</param>
        public DotNetSerialAdaptor(string portName, bool hardwareFlowControl)
        {
            _serialPort = new SerialPort(portName, SerialSymbols.BaudRate, Parity.None);
            if (hardwareFlowControl)
            {
                _serialPort.Handshake = Handshake.RequestToSend;
            }
            _serialPort.Open();
            InitializePort();
        }

        /// <summary>
        /// Construct an opened serial port using .NET SerialPort.
        /// Allowing specifying baud rate and enable hardware flow control (RTS/CTS)
        /// </summary>
        /// <param name="portName">Port number (e.g. "COM1")</param>
        /// <param name="baudRate">Baud Rate</param>
        /// <param name="hardwareFlowControl">True to enable RTS/CTS hardware flow control</param>
        public DotNetSerialAdaptor(string portName, int baudRate, bool hardwareFlowControl)
        {
            _serialPort = new SerialPort(portName, baudRate, Parity.None);
            if (hardwareFlowControl)
            {
                _serialPort.Handshake = Handshake.RequestToSend;
            }
            _serialPort.Open();
            InitializePort();
        }

        private void InitializePort()
        {
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

        public int Read(Span<byte> memory)
        {
            return _serialPort.BaseStream.Read(memory);
        }

        public void Write(Span<byte> memory)
        {
            _serialPort.BaseStream.Write(memory);
            _serialPort.BaseStream.Flush();
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

        public void DiscardReadBuffer()
        {
            _serialPort.BaseStream.Flush();
            _serialPort.DiscardInBuffer();
        }

        public void Dispose()
        {
            _serialPort?.Dispose();
        }
    }
}