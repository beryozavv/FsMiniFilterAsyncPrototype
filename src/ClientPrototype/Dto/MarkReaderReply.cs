using System.Runtime.InteropServices;

namespace ClientPrototype.Dto;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct MarkReaderReplyMessage
{
    public FilterReplyHeader ReplyHeader;
    public MarkReaderReply Reply;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct MarkReaderReply
{
    /// <summary>
    /// Есть ли право на просмотр документа
    /// </summary>
    /// <remarks>true если доступ есть, false - если доступа нет</remarks>
    public byte Rights;

    public MarkReaderReply(byte rights)
    {
        Rights = rights;
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 8)]
public struct FilterReplyHeader
{
    [MarshalAs(UnmanagedType.U4)]
    public uint Status;
    [MarshalAs(UnmanagedType.U8)]
    public ulong MessageId;
}
