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
    public class ReliableFrameSender
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
        private const int CommandTimeout = 10;

        /// <summary>
        /// Maximum number of frame pending for send.
        /// </summary>
        private const int MaxNumQueuedTask = 20;

        private readonly ISerialAdaptor _serial;

        /// <summary>
        /// All serial I/O happens in this thread
        /// </summary>
        private readonly Thread _thread;

        private readonly EventWaitHandle _threadTrigger;

        private volatile bool _shouldExit;

        private readonly ConcurrentQueue<SenderTask> _senderTasks;

        private readonly Random _random;


        public ReliableFrameSender(ISerialAdaptor serial)
        {
            _serial = serial ?? throw new ArgumentNullException(nameof(serial));
            _shouldExit = false;
            _threadTrigger = new EventWaitHandle(false, EventResetMode.AutoReset);
            _senderTasks = new ConcurrentQueue<SenderTask>();
            _random = new Random();
            _thread = new Thread(new ThreadStart(ThreadLoop));
            _thread.Start();
        }

        /// <summary>
        /// Send frame bytes to serial, and wait respond. 
        /// </summary>
        /// <param name="bytes">Bytes to sent</param>
        /// <exception cref="SerialDeviceException"> If timeout or exceed maximum number of retries.</exception>
        public async Task SendFrame(Memory<byte> bytes)
        {
            if (!ValidFrame(bytes))
            {
                throw new ArgumentException("Invalid frame bytes!");
            }

            if (_senderTasks.Count > MaxNumQueuedTask)
            {
                throw new SerialDeviceException("Too many frame queued!");
            }

            SenderTask task = new SenderTask(bytes);
            _senderTasks.Enqueue(task);
            _threadTrigger.Set();

            try
            {
                await task.AwaitSource.Task.ConfigureAwait(false);
            }
            catch (SerialDeviceException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new SerialDeviceException("Exception in serial sender sender thread", e);
            }
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
                        _serial.AsyncWrite(toSend.BytesToSend).GetAwaiter().GetResult();

                        // Start timer
                        stopwatch.Restart();

                        // Wait loop back
                        int index = 0;
                        for (byte c = _serial.AsyncReadByte().GetAwaiter().GetResult();
                            stopwatch.ElapsedMilliseconds <= CommandTimeout;
                            c = _serial.AsyncReadByte().GetAwaiter().GetResult())
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
                        Thread.Sleep(RetryInterval + _random.Next(-20, 20));
                    }
                    toSend.AwaitSource.SetException(new SerialDeviceException($"Command failed or timeout after {NumMaxRetries} retries."));
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

        private static bool ValidFrame(Memory<byte> memory)
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

            SerialSymbols.FrameType type = (SerialSymbols.FrameType)span[1];
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

        private class SenderTask
        {
            public TaskCompletionSource AwaitSource { get; }

            public Memory<byte> BytesToSend { get; }

            public SenderTask(Memory<byte> bytes)
            {
                AwaitSource = new TaskCompletionSource();
                BytesToSend = bytes;
            }
        }

    }
}
