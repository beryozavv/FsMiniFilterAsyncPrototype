namespace ClientPrototype.NativeMethods;

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

public static class PrivilegeManager
{
    public const string SeLoadDriverPrivilege = "SeLoadDriverPrivilege";

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool OpenProcessToken(IntPtr processHandle, uint desiredAccess, out IntPtr tokenHandle);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool LookupPrivilegeValue(string? lpSystemName, string lpName, out Luid luid);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool AdjustTokenPrivileges(IntPtr tokenHandle, bool disableAllPrivileges, ref TokenPrivileges newState, uint bufferLength, IntPtr previousState, IntPtr returnLength);

    private const uint ErrorTokenAdjustPrivileges = 0x20;
    private const uint ErrorTokenQuery = 0x8;

    [StructLayout(LayoutKind.Sequential)]
    private struct Luid
    {
        public uint LowPart;
        public int HighPart;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct TokenPrivileges
    {
        public uint PrivilegeCount;
        public Luid Luid;
        public uint Attributes;
    }

    private const uint ErrorSePrivilegeEnabled = 0x2;

    public static void EnableCurrentProcessPrivilege(string privilegeName)
    {
        var handle = Process.GetCurrentProcess().Handle;
        EnablePrivilege(handle, privilegeName);
    }

    public static void EnablePrivilege(IntPtr processHandle, string privilege)
    {
        if (!OpenProcessToken(processHandle,ErrorTokenAdjustPrivileges | ErrorTokenQuery, out IntPtr tokenHandle))
        {
            throw new InvalidOperationException("Failed to open process token.");
        }

        try
        {
            if (!LookupPrivilegeValue(null, privilege, out Luid luid))
            {
                throw new InvalidOperationException($"Failed to lookup privilege value for {privilege}.");
            }

            var tokenPrivileges = new TokenPrivileges
            {
                PrivilegeCount = 1,
                Luid = luid,
                Attributes = ErrorSePrivilegeEnabled
            };

            if (!AdjustTokenPrivileges(tokenHandle, false, ref tokenPrivileges, 0, IntPtr.Zero, IntPtr.Zero))
            {
                throw new InvalidOperationException($"Failed to adjust token privileges for {privilege}.");
            }
        }
        finally
        {
            Marshal.FreeHGlobal(tokenHandle);
        }
    }
}
