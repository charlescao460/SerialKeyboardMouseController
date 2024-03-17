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
        private SpinWait _spinWait;
        private readonly Stopwatch _loopBackStopwatch;

        /// <summary>
        /// Enable the delay between retries of all key/button operations. Default is true.
        /// Set this can make sure our HID report interval is big enough.
        /// </summary>
        public bool EnableKeyRetryDelay { get; set; } = true;

        /// <summary>
        /// Enable the delay between retries of mouse move operations. Default is false
        /// </summary>
        public bool EnableMouseMoveRetryDelay { get; set; } = false;

        /// <summary>
        /// Event will be raised when we about to send a new HID report through hardware.
        /// </summary>
        public event KeyboardMouseEvent OnSendingReport;

        public ReliableFrameSender(ISerialAdaptor serial)
        {
            _serial = serial ?? throw new ArgumentNullException(nameof(serial));
            _shouldExit = false;
            _threadTrigger = new EventWaitHandle(false, EventResetMode.AutoReset);
            _senderTasks = new ConcurrentQueue<SenderTask>();
            _random = new Random();
            _spinWait = new SpinWait();
            _loopBackStopwatch = new Stopwatch();
            _thread = new Thread(ThreadLoop)
            {
                Priority = ThreadPriority.Highest
            };
            _thread.Start();
        }

        /// <summary>
        /// Send frame to serial, and wait respond. This method will block until we received the response. 
        /// The frame order called with this Async method is guaranteed.
        /// Due to the nature of async/sync, we do not guarantee the order when mixing both async and blocking APIs.
        /// </summary>
        /// <param name="frame">Frame to be sent</param>
        /// <param name="onSucceed">A callback which will be executed after succeed in the critical section. Useful for synchronize states. DO NOT BLOCK!</param>
        /// <exception cref="SerialDeviceException"> If timeout or exceed maximum number of retries.</exception>
        /// <returns></returns>
        public void SendFrame(SerialCommandFrame frame, Action<DateTime> onSucceed = null)
        {
            SendAndWaitForLoopback(frame, onSucceed);
        }

        /// <summary>
        /// Send frame to serial, and wait respond in an async way.
        /// The frame order called with this Async method is guaranteed.
        /// Due to the nature of async/sync, we do not guarantee the order when mixing both async and blocking APIs.
        /// </summary>
        /// <param name="frame">Frame to be sent</param>
        /// <param name="onSucceed">A callback which will be executed after succeed in the critical section. Useful for synchronize states. DO NOT BLOCK!</param>
        /// <exception cref="SerialDeviceException"> If timeout or exceed maximum number of retries.</exception>
        /// <returns></returns>
        public Task SendFrameAsync(SerialCommandFrame frame, Action<DateTime> onSucceed = null)
        {
            if (_senderTasks.Count > MaxNumQueuedTask)
            {
                throw new SerialDeviceException($"Too many frames ({_senderTasks.Count}) queued!");
            }

            SenderTask task = new SenderTask(frame, onSucceed);

            if (!ValidFrameBytes(task.Frame.Bytes.Span))
            {
                throw new ArgumentException("Invalid frame bytes!");
            }

            _senderTasks.Enqueue(task);
            _threadTrigger.Set();

            return task.AwaitSource.Task;
        }

        private void ThreadLoop()
        {
            while (true)
            {
                if (_shouldExit)
                {
                    return; // Terminate thread
                }

                // Get next task
                if (!_senderTasks.TryDequeue(out SenderTask toSend))
                {
                    // Two-phase wait for reducing latency
                    if (!_spinWait.NextSpinWillYield)
                    {
                        _spinWait.SpinOnce();
                    }
                    else
                    {
                        _threadTrigger.WaitOne();
                    }
                    continue;
                }

                try
                {
                    SendAndWaitForLoopback(toSend.Frame, toSend.OnSucceedCallback);
                    toSend.AwaitSource.SetResult();
                }
                catch (Exception e)
                {
                    toSend.AwaitSource.SetException(e);
                }
            }
        }

        /// <summary>
        /// Send and read the loopback of the frame bytes
        /// </summary>
        /// <param name="toSend">Task to send</param>
        /// <param name="onSucceed">A callback which will be executed after succeed in the critical section. Useful for synchronize states. DO NOT BLOCK!</param>
        /// <returns>The time immediately before sending the frame.</returns>
        private void SendAndWaitForLoopback(SerialCommandFrame toSend, Action<DateTime> onSucceed)
        {
            int length = toSend.Length;
            Span<byte> bytes = toSend.Bytes.Span;
            DateTime sendTime = DateTime.MinValue; // This value should not be used anyway
            bool succeed = false;

            // Loop for retry
            for (int i = 0; i < NumMaxRetries; ++i)
            {
                lock (_serial)
                {
                    try
                    {
                        // Get the time immediately before sending
                        sendTime = DateTime.Now;

                        // Send command
                        _serial.Write(bytes);

                        // Start timer
                        _loopBackStopwatch.Restart();

                        // Wait loop back
                        int index = 0;
                        for (byte
                             c = _serial.ReadByte(out bool _); // We don't care timeout here, and it shouldn't happen
                             _loopBackStopwatch.ElapsedMilliseconds <= CommandTimeout;
                             c = _serial.ReadByte(out _))
                        {
                            if (c == bytes[index])
                            {
                                if (++index == length)
                                {
                                    succeed = true;
                                    onSucceed?.Invoke(sendTime);
                                    goto endRetryLoop; // If succeeded, break here.
                                }
                            }
                            else
                            {
                                index = 0;
                            }
                        }

                        // Clean serial buffer
                        _serial.DiscardReadBuffer();
                    }
                    catch (Exception e)
                    {
                        throw new SerialDeviceException("Serial Errors!", e);
                    }

                    // Retry delay if needed
                    if ((toSend.Type == SerialSymbols.FrameType.MouseMove && EnableMouseMoveRetryDelay)
                        || (toSend.Type != SerialSymbols.FrameType.MouseMove && EnableKeyRetryDelay))
                    {
                        Thread.Sleep(RetryInterval + _random.Next(-20, 20));
                    }
                }
            }
        endRetryLoop:
            if (OnSendingReport != null)
            {
                // If succeeded, invoke event using the first sending time. If failed or retried, using the last sending time.
                Task.Run(() => OnSendingReport.Invoke(new KeyboardMouseEventArgs(sendTime, toSend.Info)));
            }
            if (!succeed)
            {
                throw new SerialDeviceException($"Command failed or timeout after {NumMaxRetries} retries.");
            }
        }

        private static bool ValidFrameBytes(Span<byte> span)
        {
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
            if (!SerialSymbols.XorChecker(span.Slice(2, length - 3), checksum))
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
                throw new TimeoutException("Failed to terminate serial sender thread.");
            }
        }

        private class SenderTask(SerialCommandFrame frame, Action<DateTime> onSucceed)
        {
            public TaskCompletionSource AwaitSource { get; } = new();

            public SerialCommandFrame Frame => frame;

            public Action<DateTime> OnSucceedCallback => onSucceed;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _shouldExit = true;
                    _threadTrigger.Set();
                    if (!_thread.Join(1000))
                    {
                        throw new TimeoutException("Failed to terminate serial sender thread.");
                    }
                    _threadTrigger.Dispose();
                    _serial.Dispose();
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
