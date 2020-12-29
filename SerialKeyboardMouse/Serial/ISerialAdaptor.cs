using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SerialKeyboardMouse.Serial
{
    public interface ISerialAdaptor
    {
        public delegate void SerialDataAvailable(ISerialAdaptor sender);

        public event SerialDataAvailable SerialDataAvailableEvent;

        /// <summary>
        /// Synchronously read a byte from serial
        /// </summary>
        /// <returns>A byte from serial port</returns>
        public byte ReadByte();

        /// <summary>
        /// Number of available bytes not read
        /// </summary>
        public int AvailableBytes { get; }

        /// <summary>
        /// Asynchronously read bytes from serial
        /// </summary>
        /// <param name="length">Desired length</param>
        /// <param name="memory">Memory storing reading result</param>
        /// <param name="token">Cancellation Token</param>
        /// <exception cref="OperationCanceledException">When the token is cancelled</exception>
        /// <returns>Actual bytes read</returns>
        public Task<int> AsyncRead(int length, byte[] memory, CancellationToken token = default);

        /// <summary>
        /// Asynchronously write bytes to serial
        /// </summary>
        /// <param name="memory">Memory containing bytes to write</param>
        /// <param name="token">Cancellation Token</param>
        /// <exception cref="OperationCanceledException">When the token is cancelled</exception>
        /// <returns>Async task</returns>
        public Task AsyncWrite(byte[] memory, CancellationToken token = default);

        /// <summary>
        /// Asynchronously read a byte
        /// </summary>
        /// <param name="token">Cancellation Token</param>
        /// <exception cref="OperationCanceledException">When the token is cancelled</exception>
        /// <returns>Byte read from task</returns>
        public Task<byte> AsyncReadByte(CancellationToken token = default);

        /// <summary>
        /// Asynchronously write a byte
        /// </summary>
        /// <param name="b">byte to write</param>
        /// <param name="token">Cancellation Token</param>
        /// <exception cref="OperationCanceledException">When the token is cancelled</exception>
        /// <returns>Async Task</returns>
        public Task AsyncWriteByte(byte b, CancellationToken token = default);

    }
}
