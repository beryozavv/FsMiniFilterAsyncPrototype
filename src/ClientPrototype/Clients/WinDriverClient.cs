using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using ClientPrototype.Abstractions;
using ClientPrototype.Constants;
using ClientPrototype.Dto;
using ClientPrototype.Exceptions;
using ClientPrototype.Helpers;
using ClientPrototype.NativeMethods;
using ClientPrototype.NativeWrappers;
using ClientPrototype.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Win32.SafeHandles;

namespace ClientPrototype.Clients;

internal class WinDriverClient : IDriverClient
{
    private readonly DriverSettings _driverSettings;

    private readonly ILogger<WinDriverClient> _logger;
    private SafeFileHandle _portHandle = null!;

    private static readonly int MsgSize = Marshal.SizeOf<MarkReaderMessage>();

    private static readonly int OverlappedOffset =
        Marshal.OffsetOf<MarkReaderMessage>(nameof(MarkReaderMessage.Overlapped)).ToInt32();

    private static readonly int ReplySize = Marshal.SizeOf<MarkReaderReplyMessage>();

    public WinDriverClient(IOptions<DriverSettings> driverSettings, ILogger<WinDriverClient> logger)
    {
        _logger = logger;
        _driverSettings = driverSettings.Value;
    }

    public void Connect()
    {
        UnloadFilter();
        LoadFilter();
        OpenConnection(_driverSettings.ConnectionName);
    }

    [SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
    public async Task<RequestNotification> ReadNotificationAsync(Guid commandId, CancellationToken cancellationToken)
    {
        try
        {
            var safeEventHandle = new SafeEventHandle();
            safeEventHandle.SetHandle(WindowsNativeMethods.CreateEvent(IntPtr.Zero, true, false, null));
            using (safeEventHandle)
            {
                if (safeEventHandle.IsInvalid)
                {
                    throw new DriverClientException("Не удалось создать событие.");
                }

                var overlapped = new NativeOverlapped
                {
                    EventHandle = safeEventHandle.DangerousGetHandle()
                };
                var safeMessageHandle = new SafeHGlobalHandle();
                safeMessageHandle.SetHandle(Marshal.AllocHGlobal(MsgSize));
                using (safeMessageHandle)
                {
                    var messagePtr = safeMessageHandle.DangerousGetHandle();
                    var overlappedPtr = IntPtr.Add(messagePtr, OverlappedOffset);
                    Marshal.StructureToPtr(overlapped, overlappedPtr, false);
                    var ioResult =
                        WindowsNativeMethods.FilterGetMessage(_portHandle, messagePtr,
                            (uint)MsgSize, overlappedPtr);

                    if (ioResult != DriverConstants.ErrorIoPending)
                    {
                        var lastError = Marshal.GetLastWin32Error();
                        throw new DriverClientException($"FilterGetMessage failed. Error code: 0x{lastError:X}");
                    }

                    var message = await overlapped.EventHandle.WaitForMessageAsync<MarkReaderMessage>(messagePtr);

                    var notificationRes =
                        new RequestNotification(commandId, message.Header.MessageId, message.Notification.Contents,
                            (int)message.Notification.Size);
                    return notificationRes;
                }
            }
        }
        catch (Exception e)
        {
            throw new DriverClientException("ReadNotification Error", e);
        }
    }

    public uint Reply(ReplyNotification replyDto)
    {
        try
        {
            var reply = new MarkReaderReplyMessage
            {
                ReplyHeader = new FilterReplyHeader { MessageId = replyDto.MessageId, Status = replyDto.Status },
                Reply = new MarkReaderReply { Rights = replyDto.Rights }
            };

            var safeReplyHandle = new SafeHGlobalHandle();
            safeReplyHandle.SetHandle(Marshal.AllocHGlobal(ReplySize));
            using (safeReplyHandle)
            {
                var replyPtr = safeReplyHandle.DangerousGetHandle();
                Marshal.StructureToPtr(reply, replyPtr, true);
                var replyResult = WindowsNativeMethods.FilterReplyMessage(
                    _portHandle,
                    replyPtr,
                    (uint)ReplySize);

                if (replyResult != DriverConstants.Ok)
                {
                    throw new DriverClientException($"Reply failed. Error code: 0x{replyResult:X}");
                }

                return replyResult;
            }
        }
        catch (Exception e)
        {
            throw new DriverClientException("Reply Error", e);
        }
    }

    public void Disconnect(CancellationToken token)
    {
        // Close port handle, it will cause return from FilterGetMessage
        var portIsValid = _portHandle is { IsClosed: false, IsInvalid: false };
        if (portIsValid)
        {
            _portHandle.Dispose();
        }

        UnloadFilter();
    }


    private void OpenConnection(string connectionName)
    {
        _logger.LogDebug("Try to open communication port");
        var hr = WindowsNativeMethods.FilterConnectCommunicationPort(
            connectionName,
            0,
            IntPtr.Zero,
            0,
            IntPtr.Zero,
            out _portHandle
        );

        if (hr != DriverConstants.Ok)
        {
            throw new DriverClientException($"Error connect to driver. ErrorCode: 0x{hr:X8}");
        }
    }

    private void LoadFilter()
    {
        var filterName = _driverSettings.DriverName;

        _logger.LogDebug("Try to load driver {Name}", filterName);
        if (string.IsNullOrEmpty(filterName))
        {
            throw new DriverClientException($"Invalid setting {nameof(filterName)}");
        }

        _logger.LogDebug("Try to enable privileges");
        PrivilegeManager.EnableCurrentProcessPrivilege(PrivilegeManager.SeLoadDriverPrivilege);

        var hr = WindowsNativeMethods.FilterLoad(filterName);
        if (hr == DriverConstants.Ok)
        {
            return;
        }

        var msg = $"Unable to load filter driver. FilterName: {filterName}, ErrorCode: {hr:X8}";
        throw new(msg);
    }

    private void UnloadFilter()
    {
        var filterName = _driverSettings.DriverName;

        if (string.IsNullOrEmpty(filterName))
        {
            throw new DriverClientException($"Invalid setting {nameof(filterName)}");
        }

        PrivilegeManager.EnableCurrentProcessPrivilege(PrivilegeManager.SeLoadDriverPrivilege);

        var hr = WindowsNativeMethods.FilterUnload(filterName);
        if (hr is DriverConstants.Ok or DriverConstants.ErrorFltFilterNotFound)
        {
            return;
        }

        var msg = $"Unable to unload filter driver. FilterName: {filterName}, ErrorCode: {hr:X8}";
        throw new(msg);
    }
}