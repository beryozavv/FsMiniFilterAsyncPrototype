using System.Runtime.InteropServices;
using System.Text;
using ClientPrototype.Dto;
using Microsoft.Win32.SafeHandles;
using SimpleConsolePrototype;
using SimpleConsolePrototype.DriverMessage;


const string portName = "\\MarkReaderPort"; // "\\DssFsMiniFilterPort"
int bufferLength = Environment.SystemPageSize;
SafeFileHandle portHandle;

// Подключение к порту фильтра
uint result =
    WindowsNativeMethods.FilterConnectCommunicationPort(portName, 0, IntPtr.Zero, 0, IntPtr.Zero, out portHandle);
if (result != 0)
{
    Console.WriteLine($"Не удалось подключиться к порту. Код ошибки: 0x{result:X}");
    return;
}

IntPtr completionPort = WindowsNativeMethods.CreateIoCompletionPort(
    portHandle,
    IntPtr.Zero,
    UIntPtr.Zero,
    (uint)Environment.ProcessorCount);

if (completionPort == IntPtr.Zero)
{
    Console.WriteLine($"Не удалось создать I/O Completion Port. Код ошибки: 0x{Marshal.GetLastWin32Error():X}");
    if (!portHandle.IsInvalid)
    {
        portHandle.Close();
    }

    return;
}

try
{
    // Обработка сообщений
    ProcessMessagesAsync(portHandle, completionPort);
}
catch (Exception e)
{
    Console.WriteLine(e);
}
finally
{
    WindowsNativeMethods.CloseHandle(completionPort);
    if (!portHandle.IsInvalid)
    {
        portHandle.Close();
    }
}

void ProcessMessagesAsync(SafeFileHandle portHandle, IntPtr completionPort)
{
    while (true)
    {
        try
        {
            ReadMessage();
            // process message
            //
            // var reply = new MarkReaderReplyMessage()
            // {
            //     
            // };
            // SendReply(portHandle, reply);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            break;
        }
    }
}

void ReadMessage()
{
    Console.ReadLine();
    //IntPtr msgPtr = IntPtr.Zero;
    var msgPtr = Marshal.AllocHGlobal(bufferLength);
    uint hr = WindowsNativeMethods.FilterGetMessage(portHandle, msgPtr, bufferLength,
        IntPtr.Zero);
    if (hr != DriverConstants.Ok)
    {
        // Проверить был ли вызван метод Disconnect(), т.е. штатное завершение программы посредством отключения от порта драйвера.
        bool normalCloseProcess = hr == DriverConstants.ErrorInvalidHandle;
        if (normalCloseProcess)
        {
            Console.WriteLine("Normal close process");
            return;
        }

        throw new Exception($"Error FilterGetMessage. Code: 0x{hr:X8}");
    }

    // MarkReaderMessage message = (MarkReaderMessage)Marshal.PtrToStructure(msgPtr, typeof(MarkReaderMessage));
    // var markReaderNotification = message.Notification;
    // var notificationHeader = message.MessageHeader;
    DriverNotificationHeader notificationHeader =
        (DriverNotificationHeader)Marshal.PtrToStructure(msgPtr, typeof(DriverNotificationHeader));
    msgPtr += Marshal.SizeOf(typeof(DriverNotificationHeader));
    //--------------------------------------------------------------------------------
    
    MarkReaderNotification markReaderNotification =
        (MarkReaderNotification)Marshal.PtrToStructure(msgPtr, typeof(MarkReaderNotification));

    IntPtr.Subtract(msgPtr, Marshal.SizeOf(typeof(DriverNotificationHeader)));
    //Marshal.FreeHGlobal(msgPtr);

    var codeArr = string.Join('-', markReaderNotification.Contents);
    var bitContent = BitConverter.ToString(markReaderNotification.Contents);
    var content = Encoding.ASCII.GetString(markReaderNotification.Contents);
    Console.WriteLine(
        $"MessageId={notificationHeader.MessageId} Encoded content. Size = {markReaderNotification.Size}, \n Codes ={codeArr}; \n BitContent = {bitContent} \n Content = {content}");

    SendReply(portHandle, new MarkReaderReplyMessage
    {
        ReplyHeader = new FilterReplyHeader { MessageId = notificationHeader.MessageId, Status = 0 },
        Reply = new MarkReaderReply { Rights = 1 }
    });

    //--------------------------------------------------------------------------------
    // // Data
    // DriverNotificationData notificationData =
    //     (DriverNotificationData)Marshal.PtrToStructure(msgPtr, typeof(DriverNotificationData));
    // msgPtr += Marshal.SizeOf(typeof(DriverNotificationData));
    // Console.WriteLine("SizeOf - DriverNotificationData: {Size}", Marshal.SizeOf(typeof(DriverNotificationData)));

    // const int sizeOfUnicodeInBytes = 2;
    // //--------------------------------------------------------------------------------
    // // Volume
    // string volumePart =
    //     Marshal.PtrToStringUni(msgPtr, notificationData.VolumePartLength / sizeOfUnicodeInBytes);
    // msgPtr += notificationData.VolumePartLength;
    // //--------------------------------------------------------------------------------
    //
    // //--------------------------------------------------------------------------------
    // // Path
    // string pathPart = Marshal.PtrToStringUni(msgPtr, notificationData.PathPartLength / sizeOfUnicodeInBytes);
    // msgPtr += notificationData.PathPartLength;
    // //--------------------------------------------------------------------------------
    //
    // //--------------------------------------------------------------------------------
    // // File
    // string filePart = Marshal.PtrToStringUni(msgPtr, notificationData.FilePartLength / sizeOfUnicodeInBytes);
}

void SendReply(SafeFileHandle portHandle, MarkReaderReplyMessage replyMessage)
{
    var messageSize = Marshal.SizeOf(replyMessage);
    var replyHeaderSize = Marshal.SizeOf<FilterReplyHeader>();
    var replyNotificationSize = Marshal.SizeOf<MarkReaderReply>();
    var replyBufferPointer = Marshal.AllocHGlobal(messageSize);

    //Marshal.StructureToPtr(replyMessage, replyBufferPointer, true);
    // Marshal.Copy(new byte[messageSize], 0, replyBufferPointer, messageSize);
    //
    // Header
    Marshal.StructureToPtr(replyMessage.ReplyHeader, replyBufferPointer, true);
    replyBufferPointer += replyHeaderSize;
    
    // Data
    Marshal.StructureToPtr(replyMessage.Reply, replyBufferPointer, true);
    replyBufferPointer += replyNotificationSize;
    
    replyBufferPointer = IntPtr.Subtract(replyBufferPointer, messageSize);

    // // Volume
    // Marshal.Copy(replyMessage.VolumePartBytes, 0, replyBufferPointer, replyMessage.VolumePartBytes.Length);
    // replyBufferPointer += replyMessage.VolumePartBytes.Length;
    //
    // // Path
    // Marshal.Copy(replyMessage.PathPartBytes, 0, replyBufferPointer, replyMessage.PathPartBytes.Length);
    // replyBufferPointer += replyMessage.PathPartBytes.Length;
    //
    // // File
    // Marshal.Copy(replyMessage.FilePartBytes, 0, replyBufferPointer, replyMessage.FilePartBytes.Length);
    //
    // // ReSharper disable once RedundantAssignment
    // // прямая запись в память через указатель на начало буфера
    // replyBufferPointer += replyMessage.FilePartBytes.Length;

    var hr = WindowsNativeMethods.FilterReplyMessage(
        portHandle,
        replyBufferPointer,
        (uint)messageSize);
    
    Console.WriteLine($"reply result = {hr:X}");
    
    Marshal.FreeHGlobal(replyBufferPointer);
}