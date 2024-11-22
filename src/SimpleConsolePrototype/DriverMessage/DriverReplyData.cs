using System.Runtime.InteropServices;
using SimpleConsolePrototype.Enums;

namespace SimpleConsolePrototype.DriverMessage;

/// <summary>
/// Данные ответа на сообщение драйвера.
/// Строковые данные идут после структуры.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct DriverReplyData
{
    /// <summary>
    /// Тип ответа для драйвера. Указание что делать с файлом.
    /// </summary>
    [MarshalAs(UnmanagedType.I4)]
    public eReplyActionType ActionType;

    /// <summary>
    /// Длина части Dos имени диска.
    /// </summary>
    [MarshalAs(UnmanagedType.U2)]
    public ushort VolumePartLength;

    /// <summary>
    /// Длина части пути до файла.
    /// </summary>
    [MarshalAs(UnmanagedType.U2)]
    public ushort PathPartLength;

    /// <summary>
    /// Длина части имени файла.
    /// </summary>
    [MarshalAs(UnmanagedType.U2)]
    public ushort FilePartLength;

    /// <summary>
    /// Не добавлять в кеш.
    /// </summary>
    [MarshalAs(UnmanagedType.U1)]
    public byte NotAddToCache;
}
