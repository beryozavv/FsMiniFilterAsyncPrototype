using System.Diagnostics;
using System.Runtime.InteropServices;
using ClientPrototype.Abstractions;
using ClientPrototype.Constants;
using ClientPrototype.Dto;
using ClientPrototype.NativeMethods;
using ClientPrototype.Settings;
using Microsoft.Extensions.Options;
using Microsoft.Win32.SafeHandles;

namespace ClientPrototype.Managers;

internal class WinDriverClient : IDriverClient
{
    private readonly DriverSettings _driverSettings;
    private SafeFileHandle _fileHandle;

    public WinDriverClient(IOptions<DriverSettings> driverSettings)
    {
        _driverSettings = driverSettings.Value;
        LoadFilter();
        _fileHandle = OpenConnection(_driverSettings.ConnectionName);
    }

    public MarkReaderNotification ReadNotification()
    {
        MarkReaderNotification notification = new MarkReaderNotification
        {
            Contents = new byte[DriverConstants.MarkReaderReadBufferSize]
        };

        int notificationSize = Marshal.SizeOf(notification);
        IntPtr notificationPtr = Marshal.AllocHGlobal(notificationSize);
        uint result = WindowsNativeMethods.FilterGetMessage(
            _fileHandle,
            notificationPtr,
            notificationSize,
            IntPtr.Zero
        );

        if (result == 0) // Успешно получили сообщение
        {
            notification = Marshal.PtrToStructure<MarkReaderNotification>(notificationPtr);
            return notification;
        }

        throw new Exception("Unable to get mark read notification");
    }

    public uint Reply(MarkReaderReply reply)
    {

        int replySize = Marshal.SizeOf(reply);
        IntPtr replyPtr = Marshal.AllocHGlobal(replySize);
        uint replyResult = WindowsNativeMethods.FilterReplyMessage(
            _fileHandle,
            replyPtr,
            (uint)replySize
        );

        return replyResult;
    }

    public void Disconnect(CancellationToken token)
    {
        // Close port handle, it will cause return from FilterGetMessage
        bool portIsValid = _fileHandle is { IsClosed: false, IsInvalid: false };
        if (!portIsValid)
        {
            return;
        }

        _fileHandle.Dispose();
        _fileHandle = null;
        FilterUnload();
    }


    private SafeFileHandle OpenConnection(string connectionName)
    {
        uint hr = WindowsNativeMethods.FilterConnectCommunicationPort(connectionName, 0, IntPtr.Zero, 0, IntPtr.Zero,
            out _fileHandle);

        if (hr != NativeResponse.Ok)
        {
            throw new Exception($"Error connect to driver. ErrorCode: 0x{hr:X8}");
        }

        bool portIsValid = _fileHandle is { IsClosed: false, IsInvalid: false };
        if (!portIsValid)
        {
            throw new Exception("Error - port is invalid.");
        }
        return _fileHandle;
    }

    private void LoadFilter()
    {
        string filterName = _driverSettings.ConnectionName;

        if (string.IsNullOrEmpty(filterName))
        {
            throw new ArgumentNullException(nameof(filterName));
        }

        // ProcessPrivileges https://archive.codeplex.com/?p=processprivileges
        using Process process = Process.GetCurrentProcess();
        // using PrivilegeEnabler privilegeEnabler = new PrivilegeEnabler(process, Privilege.LoadDriver);

        uint hr = WindowsNativeMethods.FilterLoad(filterName);
        if (hr == NativeResponse.Ok)
        {
            return;
        }

        string msg = $"Unable to load filter driver. FilterName: {filterName}, ErrorCode: {hr:X8}";
        throw new Exception(msg);
    }

    private void FilterUnload()
    {
        string filterName = _driverSettings.ConnectionName;

        if (string.IsNullOrEmpty(filterName))
        {
            throw new ArgumentNullException(nameof(filterName));
        }

        // ProcessPrivileges https://archive.codeplex.com/?p=processprivileges
        using Process process = Process.GetCurrentProcess();
        // using PrivilegeEnabler privilegeEnabler = new PrivilegeEnabler(process, Privilege.LoadDriver);

        uint hr = WindowsNativeMethods.FilterUnload(filterName);
        if (hr is NativeResponse.Ok or NativeResponse.ErrorFltFilterNotFound)
        {
            return;
        }

        string msg = $"Unable to unload filter driver. FilterName: {filterName}, ErrorCode: {hr:X8}";
        throw new Exception(msg);
    }
}
