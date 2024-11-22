using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

class MarkReader
{
    const int MARK_READER_READ_BUFFER_SIZE = 1024;
    const int MARK_READER_MAX_THREAD_COUNT = 64;

    [StructLayout(LayoutKind.Sequential)]
    public struct MARK_READER_NOTIFICATION
    {
        public uint Size;
        public uint Reserved;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = MARK_READER_READ_BUFFER_SIZE)]
        public byte[] Contents;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MARK_READER_REPLY
    {
        public byte Rights;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct FILTER_MESSAGE_HEADER
    {
        public uint MessageId;
        public uint Reserved;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct OVERLAPPED
    {
        public IntPtr Internal;
        public IntPtr InternalHigh;
        public uint Offset;
        public uint OffsetHigh;
        public IntPtr hEvent;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MARK_READER_MESSAGE
    {
        public FILTER_MESSAGE_HEADER MessageHeader;
        public MARK_READER_NOTIFICATION Notification;
        public OVERLAPPED Ovlp;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct FILTER_REPLY_HEADER
    {
        public uint Status;
        public uint MessageId;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MARK_READER_REPLY_MESSAGE
    {
        public FILTER_REPLY_HEADER ReplyHeader;
        public MARK_READER_REPLY Reply;
    }

    [DllImport("fltLib.dll", SetLastError = true)]
    public static extern int FilterConnectCommunicationPort(
        string portName,
        uint options,
        IntPtr context,
        uint size,
        IntPtr securityAttributes,
        out IntPtr portHandle);

    [DllImport("fltLib.dll", SetLastError = true)]
    public static extern int FilterGetMessage(
        IntPtr portHandle,
        IntPtr messageBuffer,
        uint bufferSize,
        ref OVERLAPPED overlapped);

    [DllImport("fltLib.dll", SetLastError = true)]
    public static extern int FilterReplyMessage(
        IntPtr portHandle,
        IntPtr replyBuffer,
        uint replyBufferSize);

    public class WorkerContext
    {
        public IntPtr Port { get; set; }
        public IntPtr CompletionPort { get; set; }
    }

    public static void WorkerThread(object obj)
    {
        var context = (WorkerContext)obj;

        while (true)
        {
            var message = new MARK_READER_MESSAGE();
            var messageBuffer = Marshal.AllocHGlobal(Marshal.SizeOf(message));
            try
            {
                // Получаем сообщение
                OVERLAPPED overlapped = new OVERLAPPED();
                int result = FilterGetMessage(context.Port, messageBuffer, (uint)Marshal.SizeOf(message), ref overlapped);

                if (result != 0)
                {
                    Console.WriteLine($"Error in FilterGetMessage: {Marshal.GetLastWin32Error()}");
                    break;
                }

                // Обработка сообщения
                message = Marshal.PtrToStructure<MARK_READER_MESSAGE>(messageBuffer);

                Console.WriteLine($"Received message: {message.Notification.Size}");

                // Формируем ответ
                var reply = new MARK_READER_REPLY_MESSAGE
                {
                    ReplyHeader = new FILTER_REPLY_HEADER
                    {
                        Status = 0,
                        MessageId = message.MessageHeader.MessageId
                    },
                    Reply = new MARK_READER_REPLY
                    {
                        Rights = (message.Notification.Contents[0] == 'N' && message.Notification.Contents[1] == 'O') ? (byte)0 : (byte)1
                    }
                };

                var replyBuffer = Marshal.AllocHGlobal(Marshal.SizeOf(reply));
                Marshal.StructureToPtr(reply, replyBuffer, false);

                int replyResult = FilterReplyMessage(context.Port, replyBuffer, (uint)Marshal.SizeOf(reply));

                if (replyResult != 0)
                {
                    Console.WriteLine($"Error in FilterReplyMessage: {Marshal.GetLastWin32Error()}");
                    break;
                }

                Console.WriteLine($"Reply sent: {reply.Reply.Rights}");
            }
            finally
            {
                Marshal.FreeHGlobal(messageBuffer);
            }
        }
    }

    static void Main(string[] args)
    {
        uint requestCount = 5;
        uint threadCount = 2;

        if (args.Length > 0)
        {
            requestCount = uint.Parse(args[0]);
            if (args.Length > 1)
            {
                threadCount = uint.Parse(args[1]);
            }
        }

        IntPtr port;
        int result = FilterConnectCommunicationPort("MarkReaderPortName", 0, IntPtr.Zero, 0, IntPtr.Zero, out port);
        if (result != 0)
        {
            Console.WriteLine($"Error connecting to port: {Marshal.GetLastWin32Error()}");
            return;
        }

        var context = new WorkerContext { Port = port };

        Thread[] threads = new Thread[threadCount];
        for (int i = 0; i < threadCount; i++)
        {
            threads[i] = new Thread(WorkerThread);
            threads[i].Start(context);
        }

        foreach (var thread in threads)
        {
            thread.Join();
        }
    }
}
