using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace ClientPrototype.NativeMethods;

internal class WindowsNativeMethods
{
    [DllImport("fltlib.dll")]
    public static extern uint FilterConnectCommunicationPort(
        /* [in]  */ [MarshalAs(UnmanagedType.LPWStr)] string portName,
        /* [in]  */ uint options,
        /* [in]  */ IntPtr context,
        /* [in]  */ short sizeOfContext,
        /* [in]  */ IntPtr securityAttributes,
        /* [out] */ out SafeFileHandle portHandle);

    [DllImport("fltlib.dll")]
    public static extern uint FilterGetMessage(
        /* [in]  */ SafeFileHandle portHandle,
        /* [out] */ IntPtr messageBuffer,
        /* [in]  */ int messageBufferSize,
        /* [out] */ IntPtr overlapped);

    [DllImport("fltlib.dll")]
    public static extern uint FilterReplyMessage(
        /* [in] */ SafeFileHandle portHandle,
        /* [in] */ IntPtr replyBuffer,
        /* [in] */ uint replyBufferSize);

    [DllImport("fltlib.dll")]
    public static extern uint FilterLoad(
        /* [in] */ [MarshalAs(UnmanagedType.LPWStr)] string filterName);

    [DllImport("fltlib.dll")]
    public static extern uint FilterUnload(
        /* [in] */ [MarshalAs(UnmanagedType.LPWStr)] string filterName);
}
