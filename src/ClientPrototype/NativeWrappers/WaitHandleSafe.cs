using Microsoft.Win32.SafeHandles;

namespace ClientPrototype.NativeWrappers;

public class WaitHandleSafe : WaitHandle
{
    public WaitHandleSafe(IntPtr handle)
    {
        SafeWaitHandle = new SafeWaitHandle(handle, false);
    }
}