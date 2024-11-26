using System.Runtime.InteropServices;
using ClientPrototype.Constants;

namespace ClientPrototype.Dto;

[StructLayout(LayoutKind.Sequential)]
public struct MarkReaderMessage
{
    public FilterMessageHeader MessageHeader;
    public MarkReaderNotification Notification; // Уведомление от драйвера
    public NativeOverlapped Ovlp;               // OVERLAPPED структура
}

[StructLayout(LayoutKind.Sequential)]
public struct MarkReaderNotification
{
    [MarshalAs(UnmanagedType.U4)]
    public uint Size;
    [MarshalAs(UnmanagedType.U4)]
    public uint Reserved;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = DriverConstants.MarkReaderReadBufferSize)]
    public byte[] Contents;
    // [MarshalAs(UnmanagedType.LPUTF8Str, SizeConst = DriverConstants.MarkReaderReadBufferSize)]
    // public string Contents;
}

[StructLayout(LayoutKind.Sequential)]
public struct FilterMessageHeader
{
    /// <summary>
    /// Максимальная длина ответа на сообщение.
    /// </summary>
    [MarshalAs(UnmanagedType.U4)]
    public uint ReplyLength;

    /// <summary>
    /// Номер сообщения.
    /// Высталвяется драйвером. В ответе должен быть тот же номер.
    /// </summary>
    [MarshalAs(UnmanagedType.U8)]
    public ulong MessageId;
}