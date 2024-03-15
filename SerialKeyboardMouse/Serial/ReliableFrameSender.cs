using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SerialKeyboardMouse.Serial
{
    /// <summary>
    /// A reliable serial communication utility class to make sure all
    /// commands are sent correctly and in-order. Otherwise, throw exception
    /// <see cref="SerialDeviceException"/>. If a frame was received by the device,
    /// device will loop it back.
    /// </summary>
    internal class ReliableFrameSender : IDisposable
    {
        /// <summary>
        /// Max number of retries when timeout or unsuccessful
        /// </summary>
        private const int NumMaxRetries = 3;

        /// <summary>
        /// Time interval in ms between each retries
        /// </summary>
        private const int RetryInterval = 120;

        /// <summary>
        /// Timeout when waiting for command loop back. Unit in ms.
        /// </summary>
        private const int CommandTimeout = 20;

        /// <summary>
        /// Maximum number of frame pending for send.
        /// </summary>
        private const int MaxNumQueuedTask = 50;

        private readonly ISerialAdaptor _serial;

        /// <summary>
        /// All serial I/O happens in this thread
        /// </summary>
        private readonly Thread _thread;

        private readonly EventWaitHandle _threadTrigger;

        private volatile bool _shouldExit;
        private bool _disposedValue;
        private readonly ConcurrentQueue<SenderTask> _senderTasks;

        private readonly Random _random;

        /// <summary>
        /// Enable the delay between retries of all key/button operations. Default is true.
        /// Set this can make sure our HID report interval is big enough.
        /// </summary>
        public bool EnableKeyRetryDelay { get; set; } = true;

        /// <summary>
        /// Enable the delay between retries of mouse move operations. Default is false
        /// </summary>
        public bool EnableMouseMoveRetryDelay { get; set; } = false;

        public ReliableFrameSender(ISerialAdaptor serial)
        {
            _serial = serial ?? throw new ArgumentNullException(nameof(serial));
            _shouldExit = false;
            _threadTrigger = new EventWaitHandle(false, EventResetMode.AutoReset);
            _senderTasks = new ConcurrentQueue<SenderTask>();
            _random = new Random();
            _thread = new Thread(ThreadLoop)
            {
                Priority = ThreadPriority.Highest
            };
            _thread.Start();
        }

        /// <summary>
        /// Send frame to serial, and wait respond. 
        /// </summary>
        /// <param name="frame">Frame to be sent</param>
        /// <exception cref="SerialDeviceException"> If timeout or exceed maximum number of retries.</exception>
        public Task SendFrame(SerialCommandFrame frame)
        {
            if (_senderTasks.Count > MaxNumQueuedTask)
            {
                throw new SerialDeviceException($"Too many frames ({_senderTasks.Count}) queued!");
            }

            SenderTask task = new SenderTask(frame);

            if (!ValidFrameBytes(task.BytesToSend))
            {
                throw new ArgumentException("Invalid frame bytes!");
            }

            _senderTasks.Enqueue(task);
            _threadTrigger.Set();

            return task.AwaitSource.Task;
        }

        private void ThreadLoop()
        {
            Stopwatch stopwatch = new Stopwatch();
            byte[] desiredLoopback = new byte[SerialSymbols.MaxFrameLength];

            while (true)
            {
                // Get next task
                if (!_senderTasks.TryDequeue(out SenderTask toSend))
                {
                    _threadTrigger.WaitOne();
                }

                if (_shouldExit)
                {
                    return; // Terminate thread
                }

                if (toSend == null)
                {
                    continue;
                }

                try
                {
                    // Load bytes to thread local array
                    toSend.BytesToSend.CopyTo(new Memory<byte>(desiredLoopback));
                    int length = toSend.BytesToSend.Length;

                    // Loop for retry
                    for (int i = 0; i < NumMaxRetries; ++i)
                    {
                        // Send command
                        _serial.Write(toSend.BytesToSend.Span);

                        // Start timer
                        stopwatch.Restart();

                        // Wait loop back
                        int index = 0;
                        for (byte
                            c = _serial.ReadByte(out bool _); // We don't care timeout here, and it shouldn't happen
                            stopwatch.ElapsedMilliseconds <= CommandTimeout;
                            c = _serial.ReadByte(out _))
                        {
                            if (c == desiredLoopback[index])
                            {
                                if (++index == length)
                                {
                                    goto onSuccessful;
                                }
                            }
                            else
                            {
                                index = 0;
                            }
                        }
                        // Retry delay if needed
                        if ((toSend.Original.Type == SerialSymbols.FrameType.MouseMove && EnableMouseMoveRetryDelay)
                            || (toSend.Original.Type != SerialSymbols.FrameType.MouseMove && EnableKeyRetryDelay))
                        {
                            Thread.Sleep(RetryInterval + _random.Next(-20, 20));
                        }
                        // Clean serial buffer
                        _serial.DiscardReadBuffer();
                    }
                    toSend.AwaitSource.SetException(
                        new SerialDeviceException($"Command failed or timeout after {NumMaxRetries} retries."));
                    continue;
                onSuccessful:
                    toSend.AwaitSource.SetResult();
                    continue;
                }
                catch (Exception e)
                {
                    toSend.AwaitSource.SetException(e);
                }
            }
        }

        private static bool ValidFrameBytes(Memory<byte> memory)
        {
            Span<byte> span = memory.Span;
            int length = span.Length;
            if (length > SerialSymbols.MaxFrameLength || length < SerialSymbols.MinFrameLength)
            {
                return false;
            }

            if (span[0] != SerialSymbols.FrameStart)
            {
                return false;
            }

            SerialSymbols.FrameType type = (SerialSymbols.FrameType)span[2];
            if (!SerialSymbols.ValidFrameTypes.Contains(type))
            {
                return false;
            }

            byte checksum = span[length - 1];
            if (!SerialSymbols.XorChecker(memory.Slice(2, length - 3), checksum))
            {
                return false;
            }

            return true;
        }

        ~ReliableFrameSender()
        {
            _shouldExit = true;
            _threadTrigger.Set();
            if (!_thread.Join(1000))
            {
                throw new Exception("Failed to terminate serial sender thread.");
            }
        }

        private class SenderTask(SerialCommandFrame frame)
        {
            public TaskCompletionSource AwaitSource { get; } = new();

            public Memory<byte> BytesToSend { get; } = frame.Bytes;

            public SerialCommandFrame Original { get; } = frame;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _serial.Dispose();
                    _threadTrigger.Dispose();
                    _shouldExit = true;
                    _threadTrigger.Set();
                    if (!_thread.Join(1000))
                    {
                        throw new Exception("Failed to terminate serial sender thread.");
                    }
                }
                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
