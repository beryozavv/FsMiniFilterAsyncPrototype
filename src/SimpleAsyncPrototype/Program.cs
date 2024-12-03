using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;
using ClientPrototype.Constants;
using ClientPrototype.Dto;
using ClientPrototype.NativeMethods;
using ClientPrototype.NativeWrappers;
using Microsoft.Win32.SafeHandles;

namespace SimpleAsyncPrototype;

internal class Program
{
    const string ErrorPortName = "\\MarkReaderPort";
    private static SafeFileHandle _portHandle = null!;

    private static readonly int MsgSize = Marshal.SizeOf<MarkReaderMessage>();

    private static readonly int OverlappedOffset =
        Marshal.OffsetOf<MarkReaderMessage>(nameof(MarkReaderMessage.Overlapped)).ToInt32();

    private static readonly int ReplySize = Marshal.SizeOf<MarkReaderReplyMessage>();

    static async Task Main()
    {
        // Подключение к порту фильтра
        var result =
            WindowsNativeMethods.FilterConnectCommunicationPort(ErrorPortName, 0, IntPtr.Zero, 0, IntPtr.Zero,
                out _portHandle);
        if (result != 0)
        {
            Console.WriteLine($"Не удалось подключиться к порту. Код ошибки: 0x{result:X}");
            return;
        }

        // Начинаем асинхронное чтение сообщений
        await ReadMessagesAsync();
    }

    [SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
    private static async Task ReadMessagesAsync()
    {
        while (true)
        {
            var safeEventHandle = new SafeEventHandle();
            safeEventHandle.SetHandle(WindowsNativeMethods.CreateEvent(IntPtr.Zero, true, false, null));
            using (safeEventHandle)
            {
                if (safeEventHandle.IsInvalid)
                {
                    throw new Exception("Не удалось создать событие.");
                }

                var overlapped = new NativeOverlapped
                {
                    EventHandle = safeEventHandle.DangerousGetHandle()
                };
                var safeMessageHandle = new SafeHGlobalHandle();
                safeMessageHandle.SetHandle(Marshal.AllocHGlobal(MsgSize));
                MarkReaderMessage message;
                using (safeMessageHandle)
                {
                    var messagePtr = safeMessageHandle.DangerousGetHandle();
                    var overlappedPtr = IntPtr.Add(messagePtr, OverlappedOffset);
                    Marshal.StructureToPtr(overlapped, overlappedPtr, false);
                    var ioResult =
                        WindowsNativeMethods.FilterGetMessage(_portHandle, messagePtr,
                            (uint)MsgSize, overlappedPtr);

                    if (ioResult != DriverConstants.ErrorIoPending)
                    {
                        var lastError = Marshal.GetLastWin32Error();
                        throw new Exception($"FilterGetMessage failed. Error code: 0x{lastError:X}");
                    }

                    
                    message = await WaitForEventAsync(overlapped.EventHandle, messagePtr);
                    var contentString = Encoding.UTF8.GetString(message.Notification.Contents);
                    Console.WriteLine($"{message.Header.MessageId}; Content: {contentString.Substring(0, 50)}");
                    
                }
                SendReplyToMessage(message);
            }
        }
    }

    private static async Task<MarkReaderMessage> WaitForEventAsync(IntPtr hEvent, IntPtr state)
    {
        var tcs = new TaskCompletionSource<MarkReaderMessage>();

        var regWaitHandle = ThreadPool.RegisterWaitForSingleObject(
            new WaitHandleSafe(hEvent),
            (cbState, _) =>
            {
                var message = Marshal.PtrToStructure<MarkReaderMessage>((IntPtr)cbState!);
                tcs.SetResult(message);
            },
            state,
            -1,
            true
        );

        var result = await tcs.Task;
        regWaitHandle.Unregister(null);
        return result;
    }

    private static void SendReplyToMessage(MarkReaderMessage result)
    {
        var rights = result.Notification.Contents[0] == 78 && result.Notification.Contents[1] == 79 ? 0 : 1;

        var reply = new MarkReaderReplyMessage
        {
            ReplyHeader = new FilterReplyHeader { MessageId = result.Header.MessageId, Status = 0 },
            Reply = new MarkReaderReply { Rights = (byte)rights }
        };

        var safeReplyHandle = new SafeHGlobalHandle();
        safeReplyHandle.SetHandle(Marshal.AllocHGlobal(ReplySize));
        using (safeReplyHandle)
        {
            var replyPtr = safeReplyHandle.DangerousGetHandle();
            Marshal.StructureToPtr(reply, replyPtr, true);
            var hr = WindowsNativeMethods.FilterReplyMessage(
                _portHandle,
                replyPtr,
                (uint)ReplySize);

            Console.WriteLine(
                $"Message {reply.ReplyHeader.MessageId} Reply status = {hr.ToString("X")}; Rights={rights}; c[0]={(char)result.Notification.Contents[0]}; c[1]={(char)result.Notification.Contents[1]}");
        }
    }
}