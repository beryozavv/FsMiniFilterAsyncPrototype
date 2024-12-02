using System.Runtime.InteropServices;

namespace ClientPrototype.NativeWrappers;

public sealed class SafeHGlobalHandle() : SafeHandle(IntPtr.Zero, true)
{
    // Конструктор

    // Свойство проверки валидности
    public override bool IsInvalid => handle == IntPtr.Zero;
    
    public new void SetHandle(IntPtr eventHandle)
    {
        base.SetHandle(eventHandle);
    }

    // Освобождение памяти
    protected override bool ReleaseHandle()
    {
        Marshal.FreeHGlobal(handle);
        return true;
    }
}