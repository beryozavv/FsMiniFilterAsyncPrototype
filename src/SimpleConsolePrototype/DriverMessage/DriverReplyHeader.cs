using System.Runtime.InteropServices;

namespace SimpleConsolePrototype.DriverMessage;

/// <summary>
/// Заголовок ответа на сообщение драйвера.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct DriverReplyHeader
{
    /// <summary>
    /// Статус обработки сообщения драйвера.
    /// </summary>
    [MarshalAs(UnmanagedType.U4)]
    public uint Status;

    /// <summary>
    /// Номер сообщения.
    /// Высталвяется драйвером. Должен иметь тот же номер, что и в сообщении от драйвера.
    /// </summary>
    [MarshalAs(UnmanagedType.U8)]
    public long MessageId;
}