using System.Runtime.InteropServices;

namespace SimpleConsolePrototype.DriverMessage;

/// <summary>
/// Заголовок сообщения от драйвера.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct DriverNotificationHeader
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

