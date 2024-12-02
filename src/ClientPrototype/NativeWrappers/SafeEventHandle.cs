using System.Runtime.InteropServices;
using ClientPrototype.NativeMethods;

namespace ClientPrototype.NativeWrappers;

public sealed class SafeEventHandle() : SafeHandle(IntPtr.Zero, true)
{
    // Конструктор: базовый IntPtr.Zero — невалидный дескриптор

    // Переопределение свойства для проверки валидности
    public override bool IsInvalid => handle == IntPtr.Zero || handle == new IntPtr(-1);

    public new void SetHandle(IntPtr eventHandle)
    {
        base.SetHandle(eventHandle);
    }

    // Реализация освобождения ресурса
    protected override bool ReleaseHandle()
    {
        return WindowsNativeMethods.CloseHandle(handle);
    }
}