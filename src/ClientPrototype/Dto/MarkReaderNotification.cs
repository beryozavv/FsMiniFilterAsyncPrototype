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
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct FilterMessageHeader
{
    [MarshalAs(UnmanagedType.U4)]
    public uint ReplyLength;
    [MarshalAs(UnmanagedType.U8)]
    public ulong MessageId;
}

/*[StructLayout(LayoutKind.Sequential)]
public struct Overlapped
{
    public IntPtr Internal;
    public IntPtr InternalHigh;
    public ulong Offset;
    public ulong OffsetHigh;
    public IntPtr hEvent;
}*/