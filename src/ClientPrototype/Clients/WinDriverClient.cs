using System.Diagnostics;
using System.Runtime.InteropServices;
using ClientPrototype.Abstractions;
using ClientPrototype.Constants;
using ClientPrototype.Dto;
using ClientPrototype.NativeMethods;
using ClientPrototype.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Win32.SafeHandles;
using ProcessPrivileges;

namespace ClientPrototype.Clients;

internal class WinDriverClient : IDriverClient
{
    private readonly DriverSettings _driverSettings;

    private readonly ILogger<WinDriverClient> _logger;
    private SafeFileHandle _portHandle;
    private IntPtr _completionPort;

    private IntPtr _msgPtr;
    private NativeOverlapped _overlapped;

    public WinDriverClient(IOptions<DriverSettings> driverSettings, ILogger<WinDriverClient> logger)
    {
        _logger = logger;
        _driverSettings = driverSettings.Value;
        UnloadFilter();
        LoadFilter();
        OpenConnection(_driverSettings.ConnectionName);
    }

    public void ReadNotification()
    {
        // Предварительная инициализация FilterGetMessage
        var msgSize = Marshal.SizeOf<MarkReaderMessage>();
        var msgPtr = Marshal.AllocHGlobal(msgSize);

        var overlapped = new NativeOverlapped();
        var offset = Marshal.OffsetOf<MarkReaderMessage>("Ovlp").ToInt32();
        
        var result = WindowsNativeMethods.FilterGetMessage(_portHandle, msgPtr, (uint)msgSize, IntPtr.Add(msgPtr, offset));
        
        if (result != DriverConstants.ErrorIoPending)
        {
            var lastError = Marshal.GetLastWin32Error();
            Marshal.FreeHGlobal(msgPtr);
            throw new($"FilterGetMessage failed. Error code: 0x{lastError:X}");
        }

        // MarkReaderMessage message = Marshal.PtrToStructure<MarkReaderMessage>(msgPtr);
        // var notification = new RequestNotification(message.MessageHeader.MessageId, message.Notification.Contents);
        //return notification;
    }

    public RequestNotification ReadAsyncNotification()
    {
        IntPtr pOvlp;

        if (!WindowsNativeMethods.GetQueuedCompletionStatus(_completionPort,
            out var bytesTransferred,
            out var completionKey,
            out pOvlp,
            uint.MaxValue))
        {

            int errorCode = Marshal.GetLastWin32Error();
            throw new($"GetQueuedCompletionStatus failed. Error: 0x{errorCode:X}");
        }

        int offset = Marshal.OffsetOf<MarkReaderMessage>("Ovlp").ToInt32();
        IntPtr structPtr = IntPtr.Subtract(pOvlp, offset);
        //MarkReaderMessage message = Marshal.PtrToStructure<MarkReaderMessage>(structPtr);
        FilterMessageHeader header =
            (FilterMessageHeader)Marshal.PtrToStructure(structPtr, typeof(FilterMessageHeader));
        structPtr += Marshal.SizeOf(typeof(FilterMessageHeader));
        //--------------------------------------------------------------------------------
    
        MarkReaderNotification notification =
            (MarkReaderNotification)Marshal.PtrToStructure(structPtr, typeof(MarkReaderNotification));

        IntPtr.Subtract(structPtr, Marshal.SizeOf(typeof(FilterMessageHeader)));

        // byte[] rawData = new byte[Marshal.SizeOf<MarkReaderMessage>()];
        // Marshal.Copy(structPtr, rawData, 0, rawData.Length);
        // Console.WriteLine(BitConverter.ToString(rawData));
        //
        // FilterMessageHeader headerM = Marshal.PtrToStructure<FilterMessageHeader>(structPtr);
        // MarkReaderNotification notificationM = Marshal.PtrToStructure<MarkReaderNotification>(
        //     IntPtr.Add(structPtr, Marshal.OffsetOf<MarkReaderMessage>("Notification").ToInt32()));

        var notificationRes = new RequestNotification(header.MessageId, notification.Contents);
        return notificationRes;
    }

    public uint Reply(ReplyNotification reply)
    {
        var replyMessage = new MarkReaderReplyMessage
        {
            ReplyHeader = new()
            {
                MessageId = reply.MessageId,
                Status = reply.Status
            },
            Reply = new(reply.Rights)
        };

        var replySize = Marshal.SizeOf(replyMessage);
        IntPtr replyBuffer = Marshal.AllocHGlobal(replySize);
        Marshal.StructureToPtr(replyMessage, replyBuffer, false);
        var hr = WindowsNativeMethods.FilterReplyMessage(
            _portHandle,
            replyBuffer,
            (uint)Marshal.SizeOf<MarkReaderReply>());
        Marshal.FreeHGlobal(replyBuffer);

        if (hr != 0)
        {
            throw new($"ERROR: Failed to send reply. HRESULT: 0x{hr:X}");
        }

        return hr;
    }

    public void Disconnect(CancellationToken token)
    {
        // Close port handle, it will cause return from FilterGetMessage
        bool portIsValid = _portHandle is { IsClosed: false, IsInvalid: false };
        if (portIsValid)
        {
            _portHandle.Dispose();
        }

        if (_completionPort != IntPtr.Zero)
        {
            WindowsNativeMethods.CloseHandle(_completionPort);
        }
        UnloadFilter();
    }


    private void OpenConnection(string connectionName)
    {
        _logger.LogDebug("Try to open communication port");
        uint hr = WindowsNativeMethods.FilterConnectCommunicationPort(
            connectionName,
            0,
            IntPtr.Zero,
            0,
            IntPtr.Zero,
            out _portHandle
        );

        if (hr != DriverConstants.Ok)
        {
            throw new($"Error connect to driver. ErrorCode: 0x{hr:X8}");
        }

        _completionPort = WindowsNativeMethods.CreateIoCompletionPort(
            _portHandle,
            IntPtr.Zero,
            UIntPtr.Zero,
            24
        );

        if (_completionPort == IntPtr.Zero)
        {
            throw new("ERROR: Failed to create completion port.");
        }
    }

    private void LoadFilter()
    {
        string filterName = _driverSettings.DriverName;

        _logger.LogDebug("Try to load driver {name}", filterName);
        if (string.IsNullOrEmpty(filterName))
        {
            throw new ArgumentNullException(nameof(filterName));
        }

        _logger.LogDebug("Try to enable privileges");
        // ProcessPrivileges https://archive.codeplex.com/?p=processprivileges
        using Process process = Process.GetCurrentProcess();
        using PrivilegeEnabler privilegeEnabler = new PrivilegeEnabler(process, Privilege.LoadDriver);

        uint hr = WindowsNativeMethods.FilterLoad(filterName);
        if (hr == DriverConstants.Ok)
        {
            return;
        }

        string msg = $"Unable to load filter driver. FilterName: {filterName}, ErrorCode: {hr:X8}";
        throw new(msg);
    }

    private void UnloadFilter()
    {
        string filterName = _driverSettings.DriverName;

        if (string.IsNullOrEmpty(filterName))
        {
            throw new ArgumentNullException(nameof(filterName));
        }

        // ProcessPrivileges https://archive.codeplex.com/?p=processprivileges
        using Process process = Process.GetCurrentProcess();
        using PrivilegeEnabler privilegeEnabler = new PrivilegeEnabler(process, Privilege.LoadDriver);

        uint hr = WindowsNativeMethods.FilterUnload(filterName);
        if (hr is DriverConstants.Ok or DriverConstants.ErrorFltFilterNotFound)
        {
            return;
        }

        string msg = $"Unable to unload filter driver. FilterName: {filterName}, ErrorCode: {hr:X8}";
        throw new(msg);
    }
}
