using System.Runtime.InteropServices;
using System.Text;
using ClientPrototype.Constants;
using ClientPrototype.Dto;
using ClientPrototype.NativeMethods;
using Microsoft.Win32.SafeHandles;

class Program
{
    private const int ERROR_IO_PENDING = 997;
    private const uint INFINITE = 0xFFFFFFFF;

    const string portName = "\\MarkReaderPort"; // "\\DssFsMiniFilterPort"
    static SafeFileHandle portHandle;

    static int msgSize = Marshal.SizeOf<MarkReaderMessage>();
    static int ovlpOffset = Marshal.OffsetOf<MarkReaderMessage>("Ovlp").ToInt32();
    private static int replySize = Marshal.SizeOf<MarkReaderReplyMessage>();

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr CreateEvent(IntPtr lpEventAttributes, bool bManualReset, bool bInitialState,
        string lpName);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetEvent(IntPtr hEvent);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

    static async Task Main(string[] args)
    {
        // Подключение к порту фильтра
        uint result =
            WindowsNativeMethods.FilterConnectCommunicationPort(portName, 0, IntPtr.Zero, 0, IntPtr.Zero,
                out portHandle);
        if (result != 0)
        {
            Console.WriteLine($"Не удалось подключиться к порту. Код ошибки: 0x{result:X}");
            return;
        }

        // Начинаем асинхронное чтение сообщений
        ReadMessages(portHandle);
    }

    private static void ReadMessages(SafeFileHandle portHandle)
    {
        while (true)
        {
            IntPtr hEvent = CreateEvent(IntPtr.Zero, true, false, null);
            if (hEvent == IntPtr.Zero)
            {
                throw new Exception("Не удалось создать событие.");
            }

            var ovlp = new NativeOverlapped()
            {
                EventHandle = hEvent
            };

            var msgPtr = Marshal.AllocHGlobal(msgSize);
            IntPtr ovlpPtr = IntPtr.Add(msgPtr, ovlpOffset);
            Marshal.StructureToPtr(ovlp, ovlpPtr, false);

            uint ioResult = WindowsNativeMethods.FilterGetMessage(portHandle, msgPtr, (uint)msgSize, ovlpPtr);

            // Рабочий код:
            // var msgPtr = Marshal.AllocHGlobal(msgSize);
            //
            // IntPtr hEvent = CreateEvent(IntPtr.Zero, true, false, null);
            //
            // if (hEvent == IntPtr.Zero)
            // {
            //     throw new Exception("Не удалось создать событие.");
            // }
            //
            // var overlapped = new NativeOverlapped
            // {
            //     EventHandle = hEvent
            // };
            //
            // IntPtr overlappedPtr = IntPtr.Add(msgPtr, ovlpOffset);
            // Marshal.StructureToPtr(overlapped, overlappedPtr, false);

            //var result = WindowsNativeMethods.FilterGetMessage(portHandle, msgPtr, (uint)msgSize, overlappedPtr);

            if (ioResult != DriverConstants.ErrorIoPending)
            {
                var lastError = Marshal.GetLastWin32Error();
                Marshal.FreeHGlobal(msgPtr);
                throw new($"FilterGetMessage failed. Error code: 0x{lastError:X}");
            }
            else
            {
                // todo здесь подозрительно много считываний в цикле?
                Console.WriteLine($"Async Get Result = {ioResult.ToString("X")} ThreadId = {Thread.CurrentThread.ManagedThreadId}");
                RegisterCallback(ovlp.EventHandle, msgPtr);
            }
        }
    }

    private static void SendReplyToMessage(MarkReaderMessage result)
    {
        var rights = result.Notification.Contents[0] == 78 && result.Notification.Contents[1] == 79 ? 0 : 1;

        var reply = new MarkReaderReplyMessage
        {
            ReplyHeader = new FilterReplyHeader { MessageId = result.MessageHeader.MessageId, Status = 0 },
            Reply = new MarkReaderReply { Rights = (byte)rights }
        };

        var replyBuffer = Marshal.AllocHGlobal(replySize);
        Marshal.StructureToPtr(reply, replyBuffer, true);
        var hr = WindowsNativeMethods.FilterReplyMessage(
            portHandle,
            replyBuffer,
            (uint)replySize);
        Marshal.FreeHGlobal(replyBuffer);

        Console.WriteLine(
            $"Message {reply.ReplyHeader.MessageId} Reply status = {hr.ToString("X")}; Rights={rights}; c[0]={result.Notification.Contents[0]}; c[1]={result.Notification.Contents[1]}");
    }

    private static void RegisterCallback(IntPtr hEvent, IntPtr state)
    {
        RegisteredWaitHandle regWaitHandle = null;
        regWaitHandle = ThreadPool.RegisterWaitForSingleObject(
            new WaitHandleSafe(hEvent),
            /*static*/ (state, timedOut) =>
            {
                var callbackState = (CallbackState)state;
                try
                {
                    var message = Marshal.PtrToStructure<MarkReaderMessage>(callbackState.MsgPtr);
                    var contentString = Encoding.UTF8.GetString(message.Notification.Contents);
                    Console.WriteLine($"{message.MessageHeader.MessageId}; ThreadId = {Thread.CurrentThread.ManagedThreadId} \n Content: {contentString}");

                    // todo call dataflow
                    SendReplyToMessage(message);
                }
                finally
                {
                    regWaitHandle.Unregister(null); // todo это без замыкания не работает или не инициализируется или уничтожается..
                    //callbackState.waitHandle.Unregister(null);
                }
            },
            new CallbackState(state, regWaitHandle)
            ,
            -1,
            true
        );
    }

    record CallbackState(IntPtr MsgPtr, RegisteredWaitHandle waitHandle);
}

public class WaitHandleSafe : WaitHandle
{
    public WaitHandleSafe(IntPtr handle)
    {
        SafeWaitHandle = new Microsoft.Win32.SafeHandles.SafeWaitHandle(handle, false);
    }
}