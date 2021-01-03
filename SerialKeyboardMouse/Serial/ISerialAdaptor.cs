using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SerialKeyboardMouse.Serial
{
    public interface ISerialAdaptor : IDisposable
    {
        /// <summary>
        /// Handler of event when there's new data incoming in serial port
        /// </summary>
        /// <param name="sender">This adaptor</param>
        public delegate void SerialDataAvailable(ISerialAdaptor sender);

        /// <summary>
        /// Raised when there's new data coming
        /// </summary>
        public event SerialDataAvailable SerialDataAvailableEvent;

        /// <summary>
        /// Number of available bytes not read
        /// </summary>
        public int AvailableBytes { get; }

        /// <summary>
        /// Synchronously read a byte from serial
        /// </summary>
        /// <param name="timeout">If read timeout</param>
        /// <returns>A byte from serial port</returns>
        public byte ReadByte(out bool timeout);

        /// <summary>
        /// Synchronously write a byte to serial
        /// </summary>
        /// <param name="b">Byte to write.</param>
        public void WriteByte(byte b);

        /// <summary>
        /// Synchronously read bytes from serial
        /// </summary>
        /// <param name="memory">Memory storing reading result</param>
        /// <returns>Actual bytes read</returns>
        public int Read(Memory<byte> memory);

        /// <summary>
        /// Synchronously write bytes to serial
        /// </summary>
        /// <param name="memory">Memory containing bytes to write</param>
        public void Write(Memory<byte> memory);

        /// <summary>
        /// Asynchronously read bytes from serial
        /// </summary>
        /// <param name="memory">Memory storing reading result</param>
        /// <param name="token">Cancellation Token</param>
        /// <exception cref="OperationCanceledException">When the token is cancelled</exception>
        /// <returns>Actual bytes read</returns>
        public ValueTask<int> AsyncRead(Memory<byte> memory, CancellationToken token = default);

        /// <summary>
        /// Asynchronously write bytes to serial
        /// </summary>
        /// <param name="memory">Memory containing bytes to write</param>
        /// <param name="token">Cancellation Token</param>
        /// <exception cref="OperationCanceledException">When the token is cancelled</exception>
        /// <returns>Async task</returns>
        public ValueTask AsyncWrite(Memory<byte> memory, CancellationToken token = default);

        /// <summary>
        /// Asynchronously read a byte
        /// </summary>
        /// <param name="token">Cancellation Token</param>
        /// <exception cref="OperationCanceledException">When the token is cancelled</exception>
        /// <returns>Byte read from task</returns>
        public ValueTask<byte> AsyncReadByte(CancellationToken token = default);

        /// <summary>
        /// Asynchronously write a byte
        /// </summary>
        /// <param name="b">byte to write</param>
        /// <param name="token">Cancellation Token</param>
        /// <exception cref="OperationCanceledException">When the token is cancelled</exception>
        /// <returns>Async Task</returns>
        public ValueTask AsyncWriteByte(byte b, CancellationToken token = default);

    }
}
