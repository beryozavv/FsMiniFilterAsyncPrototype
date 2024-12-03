using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;
using ClientPrototype.Constants;
using ClientPrototype.Dto;
using ClientPrototype.NativeMethods;
using Microsoft.Win32.SafeHandles;

namespace SimpleCallbackPrototype;

class Program
{
    const string ErrorPortName = "\\MarkReaderPort"; // "\\DssFsMiniFilterPort"
    static SafeFileHandle _portHandle=null!;

    static int _msgSize = Marshal.SizeOf<MarkReaderMessage>();
    static int _ovlpOffset = Marshal.OffsetOf<MarkReaderMessage>("Overlapped").ToInt32();
    private static int _replySize = Marshal.SizeOf<MarkReaderReplyMessage>();

    private static readonly CancellationTokenSource Cts = new();
    
    static Task Main()
    {
        // Подключение к порту фильтра
        uint result =
            WindowsNativeMethods.FilterConnectCommunicationPort(ErrorPortName, 0, IntPtr.Zero, 0, IntPtr.Zero,
                out _portHandle);
        if (result != 0)
        {
            Console.WriteLine($"Не удалось подключиться к порту. Код ошибки: 0x{result:X}");
            return Task.CompletedTask;
        }

        // Начинаем асинхронное чтение сообщений
        ReadMessages();
        return Task.CompletedTask;
    }

    private static void ReadMessages()
    {
        for (int i = 0; i < 10; i++)
        {
            ReadMessage();
        }

        CancellationToken cancellationToken = Cts.Token;
        while (cancellationToken.IsCancellationRequested == false)
        {
        }

        Console.WriteLine("Cancellation requested");
    }

    [SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
    private static void ReadMessage()
    {
        IntPtr hEvent = WindowsNativeMethods.CreateEvent(IntPtr.Zero, true, false, null);
        if (hEvent == IntPtr.Zero)
        {
            throw new Exception("Не удалось создать событие.");
        }

        var ovlp = new NativeOverlapped
        {
            EventHandle = hEvent
        };

        var msgPtr = Marshal.AllocHGlobal(_msgSize);
        IntPtr ovlpPtr = IntPtr.Add(msgPtr, _ovlpOffset);
        Marshal.StructureToPtr(ovlp, ovlpPtr, false);

        uint ioResult = WindowsNativeMethods.FilterGetMessage(_portHandle, msgPtr, (uint)_msgSize, ovlpPtr);

        // Рабочий код:
        /*var msgPtr = Marshal.AllocHGlobal(msgSize);

        IntPtr hEvent = CreateEvent(IntPtr.Zero, true, false, null);

        if (hEvent == IntPtr.Zero)
        {
            throw new Exception("Не удалось создать событие.");
        }

        var ovlp = new NativeOverlapped
        {
            EventHandle = hEvent
        };

        IntPtr ovlpPtr = IntPtr.Add(msgPtr, ovlpOffset);
        Marshal.StructureToPtr(ovlp, ovlpPtr, false);

        var ioResult = WindowsNativeMethods.FilterGetMessage(portHandle, msgPtr, (uint)msgSize, ovlpPtr);*/

        if (ioResult != DriverConstants.ErrorIoPending)
        {
            var lastError = Marshal.GetLastWin32Error();
            Marshal.FreeHGlobal(msgPtr);
            Cts.Cancel();
            throw new($"FilterGetMessage failed. Error code: 0x{lastError:X}");
        }
        else
        {
            // todo здесь подозрительно много считываний в цикле?
            Console.WriteLine($"Async Get Result = {ioResult:X} ThreadId = {Thread.CurrentThread.ManagedThreadId}");
            Console.WriteLine($"Registrating callback for event {ovlp.EventHandle.ToInt32():X}");
            RegisterCallback(ovlp.EventHandle, msgPtr);
        }
    }

    private static void SendReplyToMessage(MarkReaderMessage result)
    {
        var rights = result.Notification.Contents[0] == 78 && result.Notification.Contents[1] == 79 ? 0 : 1;

        var reply = new MarkReaderReplyMessage
        {
            ReplyHeader = new FilterReplyHeader
            {
                MessageId = result.Header.MessageId,
                Status = 0
            },
            Reply = new MarkReaderReply
            {
                Rights = (byte)rights
            }
        };

        var replyBuffer = Marshal.AllocHGlobal(_replySize);
        Marshal.StructureToPtr(reply, replyBuffer, true);
        var hr = WindowsNativeMethods.FilterReplyMessage(
            _portHandle,
            replyBuffer,
            (uint)_replySize);
        Marshal.FreeHGlobal(replyBuffer);

        Console.WriteLine(
            $"Message {reply.ReplyHeader.MessageId} Reply status = {hr:X}; Rights={rights}; c[0]={(char)result.Notification.Contents[0]}; c[1]={(char)result.Notification.Contents[1]}");
        if (hr != DriverConstants.Ok)
        {
            Cts.Cancel();
        }
    }

    private static void RegisterCallback(IntPtr hEvent, IntPtr state)
    {
        var callbackState = new CallbackState
        {
            MsgPtr = state
        };

        callbackState.WaitHandle = ThreadPool.RegisterWaitForSingleObject(
            new WaitHandleSafe(hEvent),
            /*/*static#1# (state, timedOut) =>
            {
                var callbackState = (CallbackState)state;
                try
                {
                    var message = Marshal.PtrToStructure<MarkReaderMessage>(callbackState.MsgPtr);
                    var contentString = Encoding.UTF8.GetString(message.Notification.Contents);
                    Console.WriteLine($"{message.Header.MessageId}; ThreadId = {Thread.CurrentThread.ManagedThreadId} \n Content: {contentString}");

                    // todo call dataflow
                    SendReplyToMessage(message);
                }
                finally
                {
                    regWaitHandle.Unregister(null); // todo это без замыкания не работает или не инициализируется или уничтожается..
                    //callbackState.waitHandle.Unregister(null);
                }
            }*/
            WaitProc,
            callbackState,
            -1,
            true
        );
        Console.WriteLine($"Callback registered for event {hEvent.ToInt32():X}");
    }

    private static void WaitProc(object? state, bool timedOut)
    {
        var callbackState = (CallbackState?)state;
        if (callbackState == null)
        {
            return;
        }

        if (callbackState.WaitHandle == null)
        {
            Console.WriteLine("waitHandle is null");
            Cts.Cancel();
            return;
        }

        try
        {
            var message = Marshal.PtrToStructure<MarkReaderMessage>(callbackState.MsgPtr);
            var contentString = Encoding.UTF8.GetString(message.Notification.Contents);
            Console.WriteLine(
                $"Handle MessageId={message.Header.MessageId}; ThreadId = {Thread.CurrentThread.ManagedThreadId} \n Content: {contentString.Substring(0, 50)}");

            // todo call dataflow
            SendReplyToMessage(message);
            ReadMessage();
        }
        finally
        {
            callbackState.WaitHandle.Unregister(null);
        }
    }

    class CallbackState
    {
        public IntPtr MsgPtr { get; init; }
        public RegisteredWaitHandle? WaitHandle { get; set; }
    }
}

internal class WaitHandleSafe : WaitHandle
{
    public WaitHandleSafe(IntPtr handle)
    {
        SafeWaitHandle = new SafeWaitHandle(handle, false);
    }
}