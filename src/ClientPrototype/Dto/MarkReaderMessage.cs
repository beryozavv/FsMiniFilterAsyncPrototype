﻿using System.Runtime.InteropServices;
using ClientPrototype.Constants;

namespace ClientPrototype.Dto;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct MarkReaderMessage
{
    public Header Header;
    public Notification Notification; // Уведомление от драйвера
    public NativeOverlapped Overlapped;               // OVERLAPPED структура
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Notification
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

[StructLayout(LayoutKind.Sequential, Pack = 8)]
public struct Header
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