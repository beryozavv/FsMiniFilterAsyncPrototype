using System.Runtime.InteropServices;

namespace SimpleConsolePrototype.DriverMessage;

/// <summary>
/// Данные для сообщения, получаемые из драйвера.
/// Строковые данные идут после структуры.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct DriverNotificationData
{
    /// <summary>
    /// Длина части Dos имени диска.
    /// </summary>
    [MarshalAs(UnmanagedType.U4)]
    public int VolumePartLength;

    /// <summary>
    /// Длина части пути до файла.
    /// </summary>
    [MarshalAs(UnmanagedType.U4)]
    public int PathPartLength;

    /// <summary>
    /// Длина части имени файла.
    /// </summary>
    [MarshalAs(UnmanagedType.U4)]
    public int FilePartLength;

    /// <summary>
    /// Pid процесса, открывающего файл.
    /// </summary>
    [MarshalAs(UnmanagedType.U8)]
    public long Pid;

    /// <summary>
    /// Тип обращения.
    /// </summary>
    [MarshalAs(UnmanagedType.U4)]
    public int AccessType;

    /// <summary>
    /// Флаги.
    /// </summary>
    [MarshalAs(UnmanagedType.U8)]
    public long Flags;

    /// <summary>
    /// Тип события.
    /// </summary>
    [MarshalAs(UnmanagedType.U4)]
    public UInt32 EventType;

    /// <summary>
    /// Флаг shareMode.
    /// </summary>
    [MarshalAs(UnmanagedType.U2)]
    public ushort shareAccess;

    /// <summary>
    /// Идентификатор сессии
    /// </summary>
    [MarshalAs(UnmanagedType.U4)]
    public uint sessionId;
}
