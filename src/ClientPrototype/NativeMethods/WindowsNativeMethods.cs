using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace ClientPrototype.NativeMethods;

internal class WindowsNativeMethods
{
    [DllImport("fltlib.dll", SetLastError = true)]
    public static extern uint FilterConnectCommunicationPort(
        /* [in]  */ [MarshalAs(UnmanagedType.LPWStr)] string portName,
        /* [in]  */ uint options,
        /* [in]  */ IntPtr context,
        /* [in]  */ short sizeOfContext,
        /* [in]  */ IntPtr securityAttributes,
        /* [out] */ out SafeFileHandle portHandle);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr CreateIoCompletionPort(
        SafeFileHandle fileHandle,
        IntPtr existingCompletionPort,
        UIntPtr completionKey,
        uint numberOfConcurrentThreads
    );

    [DllImport("fltlib.dll", SetLastError = true)]
    public static extern uint FilterGetMessage(
        SafeFileHandle hPort,
        IntPtr lpMessageBuffer,
        uint dwMessageBufferSize,
        ref NativeOverlapped lpOverlapped);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool GetQueuedCompletionStatus(
        IntPtr completionPort,
        out uint lpNumberOfBytesTransferred,
        out UIntPtr lpCompletionKey,
        out IntPtr lpOverlapped,
        uint dwMilliseconds
    );

    [DllImport("fltlib.dll", SetLastError = true)]
    public static extern uint FilterReplyMessage(
        SafeFileHandle port,
        IntPtr replyBuffer,
        uint replyBufferSize
    );

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool CloseHandle(IntPtr handle);

    [DllImport("fltlib.dll")]
    public static extern uint FilterLoad(
        /* [in] */ [MarshalAs(UnmanagedType.LPWStr)] string filterName);

    [DllImport("fltlib.dll")]
    public static extern uint FilterUnload(
        /* [in] */ [MarshalAs(UnmanagedType.LPWStr)] string filterName);
}
